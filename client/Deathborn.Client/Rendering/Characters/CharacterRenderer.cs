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
    FacingDirection facing,
    ReadOnlySpan<SpriteLayer> layers)
  {
    if (layers.Length == 0) return;
    if (clip == CharacterClip.Attack && frameIndex < 0) return;
    if (clip == CharacterClip.Hurt && frameIndex < 0) return;

    var (src, origin) = GetFrameRect(spec, clip, frameIndex, facing);
    foreach (var layer in layers)
      sb.Draw(layer.Texture, screenPos, src, tint, 0f, origin, scale, SpriteEffects.None, 0f);
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
    FacingDirection facing,
    ReadOnlySpan<SpriteLayer> layers)
  {
    if (layers.Length == 0) return;
    if (clip == CharacterClip.Attack && frameIndex < 0) return;
    if (clip == CharacterClip.Hurt && frameIndex < 0) return;

    var (src, origin) = GetFrameRect(spec, clip, frameIndex, facing);
    foreach (var layer in layers)
      SpriteOutlineDraw.Draw(sb, layer.Texture, screenPos, src, origin, outline, scale, thickness);
  }

  public static (Rectangle Source, Vector2 Origin) GetFrameRect(
    in AnimationSpecification spec,
    CharacterClip clip,
    int frameIndex,
    FacingDirection facing)
  {
    var facingIndex = (int)facing;

    if (spec.Layout == AnimationSheetLayout.Grid64)
    {
      var rows = spec.DirectionRows ?? throw new InvalidOperationException("Grid layout requires DirectionRows.");
      var row = rows[facingIndex];
      var src = new Rectangle(frameIndex * spec.FrameWidth, row * spec.FrameHeight, spec.FrameWidth, spec.FrameHeight);
      return (src, spec.Origin);
    }

    var frameOffset = spec.PerDirectionFrameOffsets is { Length: > 0 } offsets
      ? offsets[facingIndex]
      : 0;
    var srcX = spec.FrameStartX + (frameOffset + frameIndex) * spec.FrameStride;
    var srcY = spec.DirectionRowTops[facingIndex];
    var width = spec.FrameWidth;
    var height = spec.FrameHeight;
    var origin = spec.Origin;

    if (clip == CharacterClip.Attack && facing == FacingDirection.Left && spec.AttackLeftSourcePad > 0)
    {
      srcX -= spec.AttackLeftSourcePad;
      width += spec.AttackLeftSourcePad;
      origin = new Vector2(origin.X + spec.AttackLeftSourcePad, origin.Y);
    }

    return (new Rectangle(srcX, srcY, width, height), origin);
  }
}
