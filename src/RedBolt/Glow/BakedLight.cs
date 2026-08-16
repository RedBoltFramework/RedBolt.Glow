// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

namespace RedBolt.Glow;

using RedBolt.Graphics;
using RedBolt.Math;

/// <summary>Baked light description.</summary>
public struct BakedLight
{
    /// <summary>Construct default.</summary>
    public BakedLight()
    {
    }

    /// <summary>Construct valid.</summary>
    public BakedLight(Light light)
    {
        LightType = light.GetLightType();
        LightMode = light.GetLightMode();
        LightMask = light.GetLightMask();
        Color = light.GetEffectiveColor().GammaToLinear();
        IndirectBrightness = light.GetIndirectBrightness();
        Fov = light.GetFov();
        Cutoff = MathDefs.Cos(Fov * 0.5f);
        Distance = light.GetRange();
        Radius = LightType != LightType.Directional ? light.GetRadius() : 0.0f;
        Angle = LightType == LightType.Directional ? light.GetRadius() : 0.0f;
        HalfAngleTan = MathDefs.Tan(Angle / 2.0f);
        Position = light.Node!.GetWorldPosition();
        Direction = light.Node!.GetWorldDirection();
        Rotation = light.Node!.GetWorldRotation();
    }

    /// <summary>Light type.</summary>
    public LightType LightType;
    /// <summary>Light mode.</summary>
    public LightMode LightMode;
    public uint LightMask;
    /// <summary>Light color.</summary>
    public Color Color;
    /// <summary>Indirect brightness.</summary>
    public float IndirectBrightness;
    /// <summary>FOV angle (for spot lights).</summary>
    public float Fov;
    /// <summary>Cutoff aka Cos(FOV * 0.5) (for spot lights).</summary>
    public float Cutoff;
    /// <summary>Light distance (for spot and point lights).</summary>
    public float Distance;
    /// <summary>Light radius (for spot and point lights).</summary>
    public float Radius;
    /// <summary>Light angle (for directional light).</summary>
    public float Angle;
    /// <summary>Tangent of half light angle.</summary>
    public float HalfAngleTan;
    /// <summary>Position.</summary>
    public Vector3f Position;
    /// <summary>Direction.</summary>
    public Vector3f Direction;
    /// <summary>Rotation.</summary>
    public Quaternion Rotation;
}
