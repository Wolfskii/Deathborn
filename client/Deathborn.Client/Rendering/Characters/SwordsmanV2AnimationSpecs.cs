using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Swordsman V2 sprite sheets — 8 directions, tight per-frame atlas.</summary>
internal static class SwordsmanV2AnimationSpecs
{
    public const int Directions = 8;
    public const float IdleFrameDuration = 0.18f;
    public const float WalkFrameDuration = 0.12f;
    public const float AttackFrameDuration = 0.075f;

    // Sheet rows: 0=S, 1=SE, 2=E, 3=NE, 4=N, 5=NW, 6=W, 7=SW (see prompts/sprites/Player/manifest.json).
    private static readonly int[] WalkRowMap =
    [
        0, // Down — South
        7, // DownLeft — South-West
        6, // Left — West
        5, // UpLeft — North-West
        4, // Up — North
        3, // UpRight — North-East
        2, // Right — East
        1, // DownRight — South-East
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
