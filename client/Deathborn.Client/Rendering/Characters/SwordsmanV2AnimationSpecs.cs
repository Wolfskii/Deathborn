using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Swordsman V2 sprite sheets — 8 directions, tight per-frame atlas.</summary>
internal static class SwordsmanV2AnimationSpecs
{
    public const int Directions = 8;
    public const float IdleFrameDuration = 0.18f;
    public const float WalkFrameDuration = 0.12f;
    public const float AttackFrameDuration = 0.075f;

    // Walk sheet rows (0-based): 0=SE, 1=E, 2=NE, 3=SW, 4=W, 5=NW (no pure N/S).
    private static readonly int[] WalkRowMap =
    [
        0, // Down — SE (closest to south)
        3, // DownLeft — SW
        4, // Left — W profile
        5, // UpLeft — NW
        2, // Up — NE back (closest to north)
        2, // UpRight — NE
        1, // Right — E profile
        0, // DownRight — SE
    ];

    // Idle row 7 in the source art is incomplete — reuse row 6 (east).
    private static readonly int[] IdleRowMap =
    [
        0, 1, 2, 3, 4, 5, 6, 6,
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
        FramesPerDirection = 6,
        Directions = Directions,
        FrameDuration = IdleFrameDuration,
        Layout = AnimationSheetLayout.TightFrames,
        DirectionRowMap = IdleRowMap,
    };

    public static AnimationSpecification Walk => new()
    {
        FramesPerDirection = 8,
        Directions = Directions,
        FrameDuration = WalkFrameDuration,
        Layout = AnimationSheetLayout.TightFrames,
        DirectionRowMap = WalkRowMap,
    };

    public static AnimationSpecification Attack => new()
    {
        FramesPerDirection = 8,
        Directions = Directions,
        FrameDuration = AttackFrameDuration,
        Layout = AnimationSheetLayout.TightFrames,
    };
}
