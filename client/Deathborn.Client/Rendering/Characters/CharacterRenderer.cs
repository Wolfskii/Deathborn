using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

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
    if (clip is CharacterClip.Attack or CharacterClip.ShieldBash && frameIndex < 0) return;
    if (clip == CharacterClip.Hurt && frameIndex < 0) return;

    var (src, origin, effects) = GetFrameRect(spec, frameIndex, facing);
    foreach (var layer in layers)
      sb.Draw(layer.Texture, screenPos, src, Multiply(tint, layer.Tint), 0f, origin, scale, effects, 0f);
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
    if (clip is CharacterClip.Attack or CharacterClip.ShieldBash && frameIndex < 0) return;
    if (clip == CharacterClip.Hurt && frameIndex < 0) return;

    var (src, origin, effects) = GetFrameRect(spec, frameIndex, facing);
    foreach (var layer in layers)
      SpriteOutlineDraw.Draw(sb, layer.Texture, screenPos, src, origin, outline, scale, thickness);
  }

  public static (Rectangle Source, Vector2 Origin, SpriteEffects Effects) GetFrameRect(
    in AnimationSpecification spec,
    int frameIndex,
    FacingDirection facing)
  {
    var dir = FarmRpgAnimationSpecs.DirectionIndex(facing);
    var frame = Math.Clamp(frameIndex, 0, spec.FramesPerDirection - 1);
    var col = dir * spec.FramesPerDirection + frame;
    var src = new Rectangle(col * spec.FrameWidth, 0, spec.FrameWidth, spec.FrameHeight);
    return (src, spec.Origin, SpriteEffects.None);
  }

  private static Color Multiply(Color baseTint, Color layerTint) => new(
    baseTint.R * layerTint.R / 255,
    baseTint.G * layerTint.G / 255,
    baseTint.B * layerTint.B / 255,
    baseTint.A * layerTint.A / 255);
}
