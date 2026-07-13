using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Swordsman V2 sprite sheets — 8 directions, uniform grid layout.</summary>
internal static class SwordsmanV2AnimationSpecs
{
    public const int Directions = 8;
    public const float IdleFrameDuration = 0.18f;
    public const float WalkFrameDuration = 0.12f;
    public const float AttackFrameDuration = 0.075f;

    // Walk sheet has 6 physical rows; map 8 facings onto them.
    // Rows: 0=down, 1=left, 2=up, 3=right, 4=down dup, 5=up dup
    private static readonly int[] WalkRowMap =
    [
        0, // Down
        1, // DownLeft -> left profile
        1, // Left
        2, // UpLeft -> up
        2, // Up
        2, // UpRight -> up
        3, // Right
        3, // DownRight -> right profile
    ];

    public static AnimationSpecification For(CharacterClip clip) => clip switch
    {
        CharacterClip.Idle or CharacterClip.Cast => Idle,
        CharacterClip.Run or CharacterClip.Roll => Walk,
        CharacterClip.Attack => Attack,
        CharacterClip.Hurt => SwordsmanAnimationSpecs.Hurt,
        CharacterClip.Death => SwordsmanAnimationSpecs.Death,
        _ => Idle,
    };

    public static AnimationSpecification Idle => new()
    {
        FrameWidth = 209,
        FrameHeight = 156,
        CellWidth = 209,
        CellHeight = 156,
        FramesPerDirection = 6,
        Directions = Directions,
        Pivot = new Vector2(104.5f, 155f),
        Origin = new Vector2(104.5f, 155f),
        FrameDuration = IdleFrameDuration,
        Layout = AnimationSheetLayout.UniformGrid,
    };

    public static AnimationSpecification Walk => new()
    {
        FrameWidth = 156,
        FrameHeight = 209,
        CellWidth = 156,
        CellHeight = 209,
        FramesPerDirection = 8,
        Directions = Directions,
        Pivot = new Vector2(78f, 208f),
        Origin = new Vector2(78f, 208f),
        FrameDuration = WalkFrameDuration,
        Layout = AnimationSheetLayout.UniformGrid,
        DirectionRowMap = WalkRowMap,
    };

    public static AnimationSpecification Attack => new()
    {
        FrameWidth = 192,
        FrameHeight = 128,
        CellWidth = 192,
        CellHeight = 128,
        FramesPerDirection = 8,
        Directions = Directions,
        Pivot = new Vector2(96f, 127f),
        Origin = new Vector2(96f, 127f),
        FrameDuration = AttackFrameDuration,
        Layout = AnimationSheetLayout.UniformGrid,
    };
}
