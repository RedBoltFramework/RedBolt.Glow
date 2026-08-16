// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

namespace RedBolt.Glow;

using System;
using System.Collections.Generic;

using RedBolt.Math;

/// <summary>Baked lightmap data.</summary>
public struct BakedLightmap : ICloneable
{
    /// <summary>Construct default.</summary>
    public BakedLightmap()
    {
        Lightmap = [];
    }

    /// <summary>Construct.</summary>
    public BakedLightmap(int lightmapSize)
    {
        LightmapSize = lightmapSize;
        Lightmap = new List<Vector3f>(lightmapSize * lightmapSize);
        for (var i = 0; i < lightmapSize * lightmapSize; ++i)
            Lightmap.Add(Vector3f.Zero);
    }

    public readonly BakedLightmap Clone()
    {
        var clone = this;
        clone.Lightmap = new List<Vector3f>(Lightmap);
        return clone;
    }

    readonly object ICloneable.Clone() => Clone();

    /// <summary>Size of lightmap.</summary>
    public int LightmapSize;
    /// <summary>Lightmap data, in linear space.</summary>
    public List<Vector3f> Lightmap;
}

/// <summary>Lightmap cache interface.</summary>
public abstract class BakedLightCache : IDisposable
{
    public abstract void Dispose();
    /// <summary>Store baked scene chunk in the cache.</summary>
    public abstract void StoreBakedChunk(Vector3i chunk, BakedSceneChunk bakedChunk);
    /// <summary>Load baked scene chunk.</summary>
    public abstract BakedSceneChunk? LoadBakedChunk(Vector3i chunk);

    /// <summary>Store direct light for the lightmap chart.</summary>
    public abstract void StoreDirectLight(int lightmapIndex, LightmapChartBakedDirect bakedDirect);
    /// <summary>Load direct light for the lightmap chart.</summary>
    public abstract LightmapChartBakedDirect? LoadDirectLight(int lightmapIndex);

    /// <summary>Store baked lightmap.</summary>
    public abstract void StoreLightmap(int lightmapIndex, BakedLightmap bakedLightmap);
    /// <summary>Load baked lightmap.</summary>
    public abstract BakedLightmap? LoadLightmap(int lightmapIndex);
}

/// <summary>Memory lightmap cache.</summary>
public sealed class BakedLightMemoryCache : BakedLightCache
{
    private readonly Dictionary<Vector3i, BakedSceneChunk> _bakedChunks = [];
    private readonly Dictionary<int, LightmapChartBakedDirect> _directLight = [];
    private readonly Dictionary<int, BakedLightmap> _lightmaps = [];

    public override void Dispose()
    {
        foreach (var chunk in _bakedChunks.Values)
            chunk.Dispose();
        _bakedChunks.Clear();
        _directLight.Clear();
        _lightmaps.Clear();
    }

    public override void StoreBakedChunk(Vector3i chunk, BakedSceneChunk bakedChunk)
    {
        if (_bakedChunks.TryGetValue(chunk, out var previous))
            previous.Dispose();
        _bakedChunks[chunk] = bakedChunk;
    }

    public override BakedSceneChunk? LoadBakedChunk(Vector3i chunk) =>
        _bakedChunks.TryGetValue(chunk, out var value) ? value : null;

    public override void StoreDirectLight(int lightmapIndex, LightmapChartBakedDirect bakedDirect) =>
        _directLight[lightmapIndex] = bakedDirect;

    public override LightmapChartBakedDirect? LoadDirectLight(int lightmapIndex) =>
        _directLight.TryGetValue(lightmapIndex, out var value) ? value : null;

    public override void StoreLightmap(int lightmapIndex, BakedLightmap bakedLightmap) =>
        _lightmaps[lightmapIndex] = bakedLightmap;

    public override BakedLightmap? LoadLightmap(int lightmapIndex) =>
        _lightmaps.TryGetValue(lightmapIndex, out var value) ? value : null;
}
