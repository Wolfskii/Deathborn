using Microsoft.Xna.Framework;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Farm RPG modular sheets — 32×32 cells, 4 directions in one horizontal row.</summary>
internal static class FarmRpgAnimationSpecs
{
    public const int FrameWidth = 32;
    public const int FrameHeight = 32;
    public const int Directions = 4;
    public const float IdleFrameDuration = 0.18f;
    public const float WalkFrameDuration = 0.12f;
    public const float RunFrameDuration = 0.09f;
    public const float AttackFrameDuration = 0.075f;
    public const float CastFrameDuration = 0.11f;
    public const float HurtFrameDuration = 0.09f;
    public const float DeathFrameDuration = 0.14f;

    private static readonly Vector2 FootOrigin = new(FrameWidth / 2f, FrameHeight);

    public static AnimationSpecification For(CharacterClip clip) => clip switch
    {
        CharacterClip.Idle => Idle,
        CharacterClip.Cast => Cast,
        CharacterClip.Walk => Walk,
        CharacterClip.Run or CharacterClip.Roll => Run,
        CharacterClip.Attack => Attack,
        CharacterClip.Hurt => Hurt,
        CharacterClip.Death => Death,
        _ => Idle,
    };

    public static AnimationSpecification Idle => Strip(4, IdleFrameDuration);
    public static AnimationSpecification Walk => Strip(6, WalkFrameDuration);
    public static AnimationSpecification Run => Strip(8, RunFrameDuration);
    public static AnimationSpecification Attack => Strip(10, AttackFrameDuration);
    public static AnimationSpecification Cast => Strip(6, CastFrameDuration);

    /// <summary>
    /// Up/down sword strips include a second reverse arc from frame 6 onward; horizontal strips read as one swing.
    /// </summary>
    public const int AttackFramesVertical = 6;
    public const int AttackFramesDefault = 10;

    public static int AttackFramesFor(FacingDirection facing) =>
        facing is FacingDirection.Up or FacingDirection.Down
            ? AttackFramesVertical
            : AttackFramesDefault;
    public static AnimationSpecification Hurt => Strip(4, HurtFrameDuration);
    public static AnimationSpecification Death => Strip(4, DeathFrameDuration);

    private static AnimationSpecification Strip(int framesPerDirection, float frameDuration) => new()
    {
        FrameWidth = FrameWidth,
        FrameHeight = FrameHeight,
        FramesPerDirection = framesPerDirection,
        Directions = Directions,
        Pivot = FootOrigin,
        Origin = FootOrigin,
        FrameDuration = frameDuration,
    };

    /// <summary>Down, up, right, left — matches Farm RPG strip layout.</summary>
    public static int DirectionIndex(FacingDirection facing) => (int)facing;
}
