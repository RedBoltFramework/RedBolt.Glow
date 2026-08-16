// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

namespace RedBolt.Glow;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using RedBolt.Core;
using RedBolt.Graphics;
using RedBolt.Logging;
using RedBolt.Math;
using RedBolt.RenderAPI;
using RedBolt.RenderPipeline;
using RedBolt.Resource;
using RedBolt.Scene;

public sealed class LightmapStitchingContext : IDisposable
{
    public required Context Context { get; init; }
    public int LightmapSize { get; init; }
    public int NumChannels { get; init; }
    public required Texture2D PingTexture { get; init; }
    public required Texture2D PongTexture { get; init; }

    public void Dispose()
    {
        PingTexture.Dispose();
        PongTexture.Dispose();
    }
}

public static class LightmapStitcher
{
    private static readonly Vector3f[] SeamMultitap =
    [
        new(-1.0f, -1.0f, 0.024879068361000005f), new(-1.0f, 1.0f, 0.024879068361000005f),
        new(1.0f, -1.0f, 0.024879068361000005f), new(1.0f, 1.0f, 0.024879068361000005f),
        new(-1.0f, 0.0f, 0.107972863278f), new(0.0f, 1.0f, 0.107972863278f),
        new(1.0f, 0.0f, 0.107972863278f), new(0.0f, -1.0f, 0.107972863278f),
        new(0.0f, 0.0f, 0.46859227344399995f)
    ];

    public static LightmapStitchingContext Initialize(Context context, int lightmapSize, int numChannels)
    {
        var format = numChannels switch
        {
            1 => TextureFormat.TexFormatR32Float,
            2 => TextureFormat.TexFormatRg32Float,
            4 => TextureFormat.TexFormatRgba32Float,
            _ => GetUnknownTextureFormat()
        };
        var ping = new Texture2D();
        var pong = new Texture2D();
        foreach (var texture in new[] { ping, pong })
        {
            texture.SetNumLevels(1);
            texture.SetSize(lightmapSize, lightmapSize, format, TextureFlag.BindRenderTarget);
        }
        return new LightmapStitchingContext
        {
            Context = context,
            LightmapSize = lightmapSize,
            NumChannels = numChannels,
            PingTexture = ping,
            PongTexture = pong
        };
    }

    public static Model CreateSeamsModel(Context context, IReadOnlyList<LightmapSeam> seams)
    {
        VertexElement[] elements =
        [
            new(VertexElementType.Vector3, VertexElementSemantic.Position),
            new(VertexElementType.Vector2, VertexElementSemantic.TexCoord)
        ];
        var vertexData = new Vector4f[seams.Count * 4];
        for (var seamIndex = 0; seamIndex < seams.Count; ++seamIndex)
        {
            var seam = seams[seamIndex];
            for (var i = 0; i < 2; ++i)
            {
                var vertexIndex = seamIndex * 2 + i;
                vertexData[vertexIndex * 2] = new Vector4f(
                    seam.Positions[i].X, 0.0f, 1.0f - seam.Positions[i].Y, 1.0f);
                vertexData[vertexIndex * 2 + 1] = seam.OtherPositions[i].ToVector4();
            }
        }

        var vertexBuffer = new VertexBuffer();
        vertexBuffer.SetShadowed(true);
        vertexBuffer.SetSize(checked((uint)(seams.Count * 2)), elements);
        vertexBuffer.SetUnpackedData(vertexData);

        var geometry = new Geometry();
        geometry.SetNumVertexBuffers(1);
        geometry.SetVertexBuffer(0, vertexBuffer);
        geometry.SetDrawRange(PrimitiveType.LineList, 0, 0, 0, checked((uint)(seams.Count * 2)), false);

        var model = new Model();
        model.SetBoundingBox(new BoundingBox(-Vector3f.One, Vector3f.One));
        model.SetNumGeometries(1);
        model.SetNumGeometryLodLevels(0, 1);
        model.SetVertexBuffers([vertexBuffer], [], []);
        model.SetGeometry(0, 0, geometry);
        return model;
    }

    public static void StitchLightmapSeams(LightmapStitchingContext context,
        IReadOnlyList<Vector3f> input, Vector4f[] output,
        LightmapStitchingSettings settings, Model seamsModel)
    {
        for (var i = 0; i < input.Count; ++i)
            output[i] = new Vector4f(input[i], 1.0f);

        var graphics = context.Context.GetSubsystem<Graphics>()!;
        var texelSize = 1.0f / context.LightmapSize;
        using var pingScene = CreateStitchingScene(context.Context, settings, context.PongTexture, seamsModel, texelSize);
        using var pongScene = CreateStitchingScene(context.Context, settings, context.PingTexture, seamsModel, texelSize);
        using var pingViewport = CreateViewport(pingScene, context.PingTexture, graphics, out var pingView);
        using var pongViewport = CreateViewport(pongScene, context.PongTexture, graphics, out var pongView);

        if (!graphics.BeginFrame())
        {
            Log.GetLogger().LogError("Failed to begin lightmap geometry buffer rendering");
            return;
        }
        try
        {
            var currentTexture = context.PongTexture;
            var swapTexture = context.PingTexture;
            var currentView = pingView;
            var swapView = pongView;
            var bytes = MemoryMarshal.AsBytes(output.AsSpan());
            currentTexture.SetData(0, 0, 0, context.LightmapSize, context.LightmapSize, bytes);
            for (var i = 0; i < settings.NumIterations; ++i)
            {
                currentView.Render(graphics, MaterialQuality.High);
                (currentTexture, swapTexture) = (swapTexture, currentTexture);
                (currentView, swapView) = (swapView, currentView);
            }
            currentTexture.GetData(0, bytes);
        }
        finally
        {
            graphics.EndFrame();
        }
    }

    private static Scene CreateStitchingScene(Context context, LightmapStitchingSettings settings,
        Texture2D inputTexture, Model seamsModel, float texelSize)
    {
        var cache = context.GetSubsystem<ResourceCache>()!;
        var scene = new Scene();
        scene.CreateComponent<Octree>();
        var cameraNode = scene.CreateChild("Camera");
        cameraNode.SetPosition(Vector3f.Up);
        cameraNode.SetDirection(Vector3f.Down);
        var camera = cameraNode.CreateComponent<Camera>();
        camera.SetOrthographic(true);
        camera.SetOrthoSize(1.0f);
        camera.SetNearClip(0.1f);
        camera.SetNearClip(10.0f);

        var backgroundNode = scene.CreateChild("Background");
        var backgroundMaterial = new Material();
        backgroundMaterial.SetTechnique(0, cache.Get<Technique>(settings.StitchBackgroundTechniqueName));
        backgroundMaterial.SetTexture(ShaderResources.Albedo, inputTexture);
        backgroundMaterial.SetRenderOrder(0);
        var backgroundModel = backgroundNode.CreateComponent<StaticModel>();
        backgroundModel.SetModel(cache.Get<Model>(settings.StitchBackgroundModelName));
        backgroundModel.SetMaterial(backgroundMaterial);

        for (var i = 0; i < SeamMultitap.Length; ++i)
        {
            var offsetAndWeight = SeamMultitap[i];
            var offset = new Vector3f(offsetAndWeight.X, 0.0f, offsetAndWeight.Y) * texelSize * 0.5f;
            var alpha = settings.BlendFactor * offsetAndWeight.Z;
            var seamsNode = scene.CreateChild("Seams");
            seamsNode.SetPosition(new Vector3f(-0.5f, 0.1f, -0.5f) + offset);
            var material = new Material();
            material.SetTechnique(0, cache.Get<Technique>(settings.StitchSeamsTechniqueName));
            material.SetTexture(ShaderResources.Albedo, inputTexture);
            material.SetShaderParameter("MatDiffColor", new Color(1.0f, 1.0f, 1.0f, alpha));
            material.SetRenderOrder(checked((byte)(1 + i)));
            var staticModel = seamsNode.CreateComponent<StaticModel>();
            staticModel.SetModel(seamsModel);
            staticModel.SetMaterial(material);
        }
        scene.GetComponent<Octree>()!.Update(new FrameInfo());
        return scene;
    }

    private static Viewport CreateViewport(Scene scene, Texture2D outputTexture, Graphics graphics,
        out RenderPipelineView view)
    {
        var viewport = new Viewport(scene, scene.FindComponent<Camera>()!);
        viewport.SetRect(IntRect.Zero);
        viewport.AllocateView();
        view = viewport.GetRenderPipelineView()!;
        view.Define(outputTexture.GetRenderSurface(), viewport, graphics);
        view.Update(graphics);
        return viewport;
    }

    private static TextureFormat GetUnknownTextureFormat()
    {
        Debug.Fail("Unsupported lightmap stitching channel count.");
        return TextureFormat.TexFormatUnknown;
    }
}
