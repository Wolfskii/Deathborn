using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Legacy swordsman clip specs derived from <see cref="SwordsmanSpriteSheet"/>.</summary>
internal static class SwordsmanAnimationSpecs
{
  private static readonly int[] CraftpixRowTops = SwordsmanSpriteSheet.DirectionRowTops;
  private static readonly int[] GridDirectionRows = [0, 3, 2, 1]; // down, up, right, left
  private static readonly Vector2 BodyOrigin = SwordsmanSpriteSheet.BodyAnchor;
  private static readonly Vector2 GridOrigin = new(32f, 58f);

  public static AnimationSpecification For(CharacterClip clip) => clip switch
  {
    CharacterClip.Idle => Idle,
    CharacterClip.Run => Run,
    CharacterClip.Attack => Attack,
    CharacterClip.Cast => Idle,
    CharacterClip.Hurt => Hurt,
    CharacterClip.Death => Death,
    CharacterClip.Roll => Run,
    _ => Run,
  };

  public static AnimationSpecification Idle => new()
  {
    FrameWidth = SwordsmanSpriteSheet.BodyWidth,
    FrameHeight = SwordsmanSpriteSheet.BodyHeight,
    FramesPerDirection = 12,
    Directions = 4,
    Pivot = BodyOrigin,
    Origin = BodyOrigin,
    FrameDuration = SwordsmanSpriteSheet.FrameDuration,
    Layout = AnimationSheetLayout.CraftpixBody,
    FrameStride = SwordsmanSpriteSheet.FrameStride,
    FrameStartX = SwordsmanSpriteSheet.FrameStartX,
    DirectionRowTops = CraftpixRowTops,
    PerDirectionFrameCounts = SwordsmanSpriteSheet.IdleFrameCounts,
    PerDirectionFrameOffsets = SwordsmanSpriteSheet.IdleFrameOffsets,
  };

  public static AnimationSpecification Run => new()
  {
    FrameWidth = SwordsmanSpriteSheet.BodyWidth,
    FrameHeight = SwordsmanSpriteSheet.BodyHeight,
    FramesPerDirection = SwordsmanSpriteSheet.FrameCount,
    Directions = 4,
    Pivot = BodyOrigin,
    Origin = BodyOrigin,
    FrameDuration = SwordsmanSpriteSheet.FrameDuration,
    Layout = AnimationSheetLayout.CraftpixBody,
    FrameStride = SwordsmanSpriteSheet.FrameStride,
    FrameStartX = SwordsmanSpriteSheet.FrameStartX,
    DirectionRowTops = CraftpixRowTops,
  };

  public static AnimationSpecification Attack => new()
  {
    FrameWidth = SwordsmanSpriteSheet.AttackFrameWidth,
    FrameHeight = SwordsmanSpriteSheet.AttackFrameHeight,
    FramesPerDirection = SwordsmanSpriteSheet.FrameCount,
    Directions = 4,
    Pivot = BodyOrigin,
    Origin = BodyOrigin,
    FrameDuration = SwordsmanSpriteSheet.AttackFrameDuration,
    Layout = AnimationSheetLayout.CraftpixBody,
    FrameStride = SwordsmanSpriteSheet.FrameStride,
    FrameStartX = SwordsmanSpriteSheet.FrameStartX,
    DirectionRowTops = CraftpixRowTops,
    AttackLeftSourcePad = SwordsmanSpriteSheet.AttackLeftSourcePad,
  };

  public static AnimationSpecification Hurt => new()
  {
    FrameWidth = 64,
    FrameHeight = 64,
    FramesPerDirection = FourDirectionHurtAnimation.FrameCount,
    Directions = 4,
    Pivot = GridOrigin,
    Origin = GridOrigin,
    FrameDuration = 0.07f,
    Layout = AnimationSheetLayout.Grid64,
    DirectionRows = GridDirectionRows,
  };

  public static AnimationSpecification Death => new()
  {
    FrameWidth = 64,
    FrameHeight = 64,
    FramesPerDirection = FourDirectionDeathAnimation.FrameCount,
    Directions = 4,
    Pivot = GridOrigin,
    Origin = GridOrigin,
    FrameDuration = 0.11f,
    Layout = AnimationSheetLayout.Grid64,
    DirectionRows = GridDirectionRows,
  };
}
