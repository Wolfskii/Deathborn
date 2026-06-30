namespace Deathborn.Client.Rendering;

public enum FacingDirection
{
    Down = 0,
    Up = 1,
    Right = 2,
    Left = 3,
}

internal static class SwordsmanSpriteSheet
{
    public const int FrameStride = 64;
    public const int FrameStartX = 16;
    public const int FrameCount = 8;
    public const float FrameDuration = 0.15f;
    public const float AttackFrameDuration = FrameDuration * 0.5f;
    public const int BodyWidth = 32;
    public const int BodyHeight = 32;

    // Feet anchor shared across run/idle/attack so the character stays planted.
    public static readonly Microsoft.Xna.Framework.Vector2 BodyAnchor = new(BodyWidth / 2f, BodyHeight - 2f);

    public const int AttackFrameWidth = 48;
    public const int AttackFrameHeight = 48;
    /// <summary>Left-facing attack swings extend into the cell's left margin (before FrameStartX).</summary>
    public const int AttackLeftSourcePad = 8;

    // down, up, right, left
    public static readonly int[] DirectionRowTops = [16, 208, 144, 80];

    // Idle sheet: the up-facing row only has art in the last four 64px cells.
    public static readonly int[] IdleFrameOffsets = [0, 8, 0, 0];
    public static readonly int[] IdleFrameCounts = [12, 4, 12, 12];
}
