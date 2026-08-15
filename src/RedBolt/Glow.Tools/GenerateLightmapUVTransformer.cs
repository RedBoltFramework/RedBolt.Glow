// Copyright (c) 2026 RedBolt contributors.
// This work is licensed under the terms of the MIT license.

namespace RedBolt.Utility;

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Logging;

using RedBolt.Core;
using RedBolt.Glow;
using RedBolt.Graphics;
using RedBolt.IO;
using RedBolt.Logging;
using RedBolt.Resource;

/// <summary>Parameters used internally to generate a model's lightmap UVs.</summary>
internal sealed class GenerateLightmapUVParameters : IArchiveSerializable
{
    public bool Generate;
    public float TexelsPerUnit = 10.0f;
    public uint UVChannel = 1;

    public void SerializeInBlock(Archive archive)
    {
        ArchiveSerialization.SerializeOptionalValue(
            archive, "lightmapUVGenerate", ref Generate, false, ArchiveSerialization.SerializeValue);
        ArchiveSerialization.SerializeOptionalValue(
            archive, "lightmapUVTexelsPerUnit", ref TexelsPerUnit, 10.0f,
            ArchiveSerialization.SerializeValue);
        ArchiveSerialization.SerializeOptionalValue(
            archive, "lightmapUVChannel", ref UVChannel, 1u, ArchiveSerialization.SerializeValue);
    }
}

/// <summary>Generate lightmap UVs for models emitted by another asset transformer.</summary>
/// <remarks>Existing per-model settings in <c>ModelImporter.json</c> remain authoritative.</remarks>
[Reflected(ObjectCategory.Category_Transformer)]
public partial class GenerateLightmapUVTransformer : AssetTransformer
{
    public override bool IsApplicable(AssetTransformerInput input)
    {
        if (string.Equals(input.InputFileName, input.OriginalInputFileName, StringComparison.Ordinal)
            || !input.ResourceName.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return LoadParameters(input).Generate;
    }

    public override bool Execute(AssetTransformerInput input, AssetTransformerOutput output,
        IReadOnlyList<AssetTransformer> transformers)
    {
        var parameters = LoadParameters(input);
        AddParameterDependencies(input, output);

        if (parameters.UVChannel >= ModelVertex.MaxUVs)
        {
            Log.GetLogger().LogError(
                "Cannot generate lightmap UVs for '{ResourceName}': UV channel {UVChannel} is outside [0, {MaxUVs})",
                input.ResourceName, parameters.UVChannel, ModelVertex.MaxUVs);
            return false;
        }

        using var model = new Model();
        var fileName = FileIdentifier.FromUri(input.InputFileName);
        if (!model.LoadFile(fileName))
        {
            Log.GetLogger().LogError("Cannot load generated model '{ResourceName}'", input.ResourceName);
            return false;
        }

        using var modelView = new ModelView();
        if (!modelView.ImportModel(model))
        {
            Log.GetLogger().LogError("Cannot import generated model '{ResourceName}'", input.ResourceName);
            return false;
        }

        var settings = new LightmapUVGenerationSettings
        {
            TexelPerUnit = parameters.TexelsPerUnit,
            UvChannel = checked((int)parameters.UVChannel),
        };
        if (!LightmapUVGenerator.GenerateLightmapUV(modelView, settings))
        {
            Log.GetLogger().LogError("Cannot generate lightmap UVs for '{ResourceName}'", input.ResourceName);
            return false;
        }

        using var result = modelView.ExportModel(name: model.Name);
        if (!result.SaveFile(fileName))
        {
            Log.GetLogger().LogError("Cannot save generated model '{ResourceName}'", input.ResourceName);
            return false;
        }

        return true;
    }

    public override bool IsExecutedOnOutput() => true;

    private GenerateLightmapUVParameters LoadParameters(AssetTransformerInput input)
    {
        var parameters = new GenerateLightmapUVParameters
        {
            Generate = Generate,
            TexelsPerUnit = TexelsPerUnit,
            UVChannel = UVChannel,
        };

        using var mergedFile = new JSONFile();
        mergedFile.SaveObject("params", parameters);

        MergeParameters(mergedFile, GetParametersFileName(input));

        return mergedFile.LoadObject("params", parameters) ? parameters : new GenerateLightmapUVParameters
        {
            Generate = Generate,
            TexelsPerUnit = TexelsPerUnit,
            UVChannel = UVChannel,
        };
    }

    private static void MergeParameters(JSONFile mergedFile, string fileName)
    {
        using var overrideFile = new JSONFile();
        if (overrideFile.LoadFile(FileIdentifier.FromUri(fileName)))
            mergedFile.Root.ReplaceRecursively(overrideFile.Root);
    }

    private void AddParameterDependencies(AssetTransformerInput input, AssetTransformerOutput output)
    {
        var parametersFileName = GetParametersFileName(input);
        if (System.IO.File.Exists(parametersFileName))
            AddDependency(input, output, parametersFileName);
    }

    private static string GetParametersFileName(AssetTransformerInput input) =>
        input.OriginalInputFileName + ".d/ModelImporter.json";

    private static object GetGenerateDefault() => new GenerateLightmapUVParameters().Generate;

    [Serialized("LightMap UV: Generate", DefaultSource = nameof(GetGenerateDefault))]
    private bool Generate { get; set; }

    private static object GetTexelsPerUnitDefault() => new GenerateLightmapUVParameters().TexelsPerUnit;

    [Serialized("LightMap UV: Texels per Unit", DefaultSource = nameof(GetTexelsPerUnitDefault))]
    private float TexelsPerUnit { get; set; } = 10.0f;

    private static object GetUVChannelDefault() => new GenerateLightmapUVParameters().UVChannel;

    [Serialized("LightMap UV: Channel", DefaultSource = nameof(GetUVChannelDefault))]
    private uint UVChannel { get; set; } = 1;
}
