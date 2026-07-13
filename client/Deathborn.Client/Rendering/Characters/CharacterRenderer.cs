using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>
/// Stateless layer compositor. Receives animation state and ordered layers only — no timers.
/// </summary>
public static class CharacterRenderer
{
  public static void Draw(
    SpriteBatch sb,
    Vector2 screenPos,
    Color tint,
    float scale,
    in AnimationSpecification spec,
    CharacterClip clip,
    int frameIndex,
    Facing8 facing,
    ReadOnlySpan<SpriteLayer> layers)
  {
    if (layers.Length == 0) return;
    if (clip == CharacterClip.Attack && frameIndex < 0) return;
    if (clip == CharacterClip.Hurt && frameIndex < 0) return;

    var (src, origin, effects) = GetFrameRect(spec, clip, frameIndex, facing);
    foreach (var layer in layers)
      sb.Draw(layer.Texture, screenPos, src, tint, 0f, origin, scale, effects, 0f);
  }

  public static void DrawOutline(
    SpriteBatch sb,
    Vector2 screenPos,
    Color outline,
    float scale,
    float thickness,
    in AnimationSpecification spec,
    CharacterClip clip,
    int frameIndex,
    Facing8 facing,
    ReadOnlySpan<SpriteLayer> layers)
  {
    if (layers.Length == 0) return;
    if (clip == CharacterClip.Attack && frameIndex < 0) return;
    if (clip == CharacterClip.Hurt && frameIndex < 0) return;

    var (src, origin, effects) = GetFrameRect(spec, clip, frameIndex, facing);
    foreach (var layer in layers)
      SpriteOutlineDraw.Draw(sb, layer.Texture, screenPos, src, origin, outline, scale, thickness);
  }

  public static (Rectangle Source, Vector2 Origin, SpriteEffects Effects) GetFrameRect(
    in AnimationSpecification spec,
    CharacterClip clip,
    int frameIndex,
    Facing8 facing)
  {
    var facingIndex = (int)facing;

    if (spec.Layout == AnimationSheetLayout.TightFrames)
    {
      var row = spec.DirectionRowMap is { Length: > 0 } map && facingIndex < map.Length
        ? map[facingIndex]
        : facingIndex;
      var frame = Math.Clamp(frameIndex, 0, spec.FramesPerDirection - 1);
      var tight = clip switch
      {
        CharacterClip.Idle or CharacterClip.Cast => SwordsmanV2FrameAtlas.GetIdle(row, frame),
        CharacterClip.Run or CharacterClip.Roll => SwordsmanV2FrameAtlas.GetWalk(row, frame),
        CharacterClip.Attack => SwordsmanV2FrameAtlas.GetAttack(row, frame),
        _ => SwordsmanV2FrameAtlas.GetIdle(row, frame),
      };
      return (tight.Source, tight.Origin, SpriteEffects.None);
    }

    if (spec.Layout == AnimationSheetLayout.UniformGrid)
    {
      var row = spec.DirectionRowMap is { Length: > 0 } map && facingIndex < map.Length
        ? map[facingIndex]
        : facingIndex;

      if (spec.ColumnStarts is { Length: >= 2 } cols && spec.RowStarts is { Length: >= 2 } rows)
      {
        var frame = Math.Clamp(frameIndex, 0, cols.Length - 2);
        var rowIndex = Math.Clamp(row, 0, rows.Length - 2);
        var srcX = cols[frame];
        var srcY = rows[rowIndex];
        var srcW = cols[frame + 1] - srcX;
        var srcH = rows[rowIndex + 1] - srcY;
        var origin = new Vector2(srcW / 2f, srcH - 1f);
        return (new Rectangle(srcX, srcY, srcW, srcH), origin, SpriteEffects.None);
      }

      var srcXLegacy = frameIndex * spec.FrameWidth;
      var srcYLegacy = row * spec.FrameHeight;
      return (
        new Rectangle(srcXLegacy, srcYLegacy, spec.FrameWidth, spec.FrameHeight),
        spec.Origin,
        SpriteEffects.None);
    }

    if (spec.Layout == AnimationSheetLayout.Grid64)
    {
      var cardinal = Facing8Resolver.ToCardinal4(facing);
      var rows = spec.DirectionRows ?? throw new InvalidOperationException("Grid layout requires DirectionRows.");
      var row = rows[(int)cardinal];
      var src = new Rectangle(frameIndex * spec.FrameWidth, row * spec.FrameHeight, spec.FrameWidth, spec.FrameHeight);
      return (src, spec.Origin, SpriteEffects.None);
    }

    var cardinalFacing = Facing8Resolver.ToCardinal4(facing);
    var cardinalIndex = (int)cardinalFacing;
    var frameOffset = spec.PerDirectionFrameOffsets is { Length: > 0 } offsets
      ? offsets[cardinalIndex]
      : 0;
    var craftpixX = spec.FrameStartX + (frameOffset + frameIndex) * spec.FrameStride;
    var craftpixY = spec.DirectionRowTops[cardinalIndex];
    var width = spec.FrameWidth;
    var height = spec.FrameHeight;
    var craftpixOrigin = spec.Origin;

    if (clip == CharacterClip.Attack && cardinalFacing == FacingDirection.Left && spec.AttackLeftSourcePad > 0)
    {
      craftpixX -= spec.AttackLeftSourcePad;
      width += spec.AttackLeftSourcePad;
      craftpixOrigin = new Vector2(craftpixOrigin.X + spec.AttackLeftSourcePad, craftpixOrigin.Y);
    }

    return (new Rectangle(craftpixX, craftpixY, width, height), craftpixOrigin, SpriteEffects.None);
  }
}
