using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>
/// Immutable animation contract shared by every sprite layer for a given clip.
/// Farm RPG sheets: 32×32 cells, 4 directions × N frames in one horizontal row.
/// </summary>
public readonly struct AnimationSpecification
{
    public int FrameWidth { get; init; }
    public int FrameHeight { get; init; }
    public int FramesPerDirection { get; init; }
    public int Directions { get; init; }
    public Vector2 Pivot { get; init; }
    public Vector2 Origin { get; init; }
    public float FrameDuration { get; init; }
}
