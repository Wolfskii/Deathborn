using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Composes appearance, equipment, animation, and rendering for one character.</summary>
public sealed class CharacterVisual
{
  private readonly AnimationController _controller = new();
  private readonly SpriteAssembler _assembler = new();
  private CharacterAppearance _appearance = CharacterAppearance.DefaultSwordsman;
  private CharacterEquipment _equipment = CharacterEquipment.Empty;

  public AnimationController Controller => _controller;

  public CharacterAppearance Appearance
  {
    get => _appearance;
    set => _appearance = value;
  }

  public CharacterEquipment Equipment
  {
    get => _equipment;
    set => _equipment = value;
  }

  public static CharacterVisual CreateCorpse(Vector2 facingDir)
  {
    var visual = new CharacterVisual();
    visual._controller.HoldDeathPose(facingDir);
    return visual;
  }

  public void BeginDeath(Vector2 facingDir) => _controller.BeginDeath(facingDir);

  public void HoldDeathPose(Vector2 facingDir) => _controller.HoldDeathPose(facingDir);

  public void StartAttack(Vector2 facingDir) => _controller.StartAttack(facingDir);

  public void ApplyHit(Vector2 facingDir) => _controller.ApplyHit(facingDir);

  public void UpdateAnimation(float dt, in AnimationInput input) => _controller.Update(dt, input);

  public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale)
  {
    var draw = _controller.GetDrawState();
    if (draw.Clip == CharacterClip.Hurt && !_controller.IsHurtPlaying) return;
    if (draw.Clip == CharacterClip.Attack && !_controller.IsAttackPlaying) return;
    if (draw.Clip == CharacterClip.Death && !_controller.IsDead) return;

    var layers = _assembler.Assemble(_appearance, _equipment, draw.Clip);
    CharacterRenderer.Draw(sb, screenPos, tint, scale, draw.Spec, draw.Clip, draw.Frame, draw.Facing, layers);
  }

  public void DrawOutline(
    SpriteBatch sb, Vector2 screenPos, Color outline, float scale, float thickness)
  {
    var draw = _controller.GetDrawState();
    if (draw.Clip == CharacterClip.Hurt && !_controller.IsHurtPlaying) return;
    if (draw.Clip == CharacterClip.Attack && !_controller.IsAttackPlaying) return;

    var layers = _assembler.Assemble(_appearance, _equipment, draw.Clip);
    CharacterRenderer.DrawOutline(sb, screenPos, outline, scale, thickness,
      draw.Spec, draw.Clip, draw.Frame, draw.Facing, layers);
  }
}
