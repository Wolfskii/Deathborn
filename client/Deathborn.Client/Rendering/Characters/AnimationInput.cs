using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Per-frame locomotion context supplied by gameplay code.</summary>
public readonly struct AnimationInput
{
    public bool IsDead { get; init; }
    public bool IsWhirlwinding { get; init; }
    public bool IsDashing { get; init; }
    public bool IsCasting { get; init; }
    public bool IsMoving { get; init; }
    public bool IsRunning { get; init; }
    public Vector2 FacingDir { get; init; }
    public float AnimSpeed { get; init; }
}
