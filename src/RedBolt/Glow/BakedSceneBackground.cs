// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

namespace RedBolt.Glow;

using RedBolt.Math;
using RedBolt.Resource;

/// <summary>Scene background description.</summary>
public struct BakedSceneBackground
{
    public float Intensity;
    public Color Color;
    public ImageCube? Image;

    public BakedSceneBackground()
    {
        Color = new Color();
    }

    public readonly Vector3f SampleLinear(Vector3f direction)
    {
        return Intensity * SampleGammaInternal(direction).GammaToLinear().ToVector3();
    }

    private readonly Color SampleGammaInternal(Vector3f direction)
    {
        return Image is not null ? Image.SampleNearest(direction) : Color;
    }
}
