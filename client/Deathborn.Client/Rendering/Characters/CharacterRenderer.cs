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

    if (spec.Layout == AnimationSheetLayout.UniformGrid)
    {
      var row = spec.DirectionRowMap is { Length: > 0 } map && facingIndex < map.Length
        ? map[facingIndex]
        : facingIndex;
      var srcX = frameIndex * spec.CellWidth;
      var srcY = row * spec.CellHeight;
      return (
        new Rectangle(srcX, srcY, spec.FrameWidth, spec.FrameHeight),
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
