using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Composes appearance, equipment, animation, and rendering for one character.</summary>
public sealed class CharacterVisual
{
  private readonly AnimationController _controller = new();
  private readonly SpriteAssembler _assembler = new();
  private CharacterAppearance _appearance = CharacterAppearance.DefaultPlayer;
  private CharacterEquipment _equipment = CharacterEquipment.DefaultPlayer;

  public AnimationController Controller => _controller;

  public CharacterAppearance Appearance
  {
    get => _appearance;
    set
    {
      _appearance = value;
      _controller.BodyTypeId = value.BodyTypeId;
    }
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

  public void HoldIdlePose(Vector2 facingDir) => _controller.HoldIdlePose(facingDir);

  public void StartAttack(Vector2 facingDir, CharacterClip clip = CharacterClip.Attack) =>
    _controller.StartAttack(facingDir, clip);

  public void StartFishing(Vector2 facingDir, bool reeling) =>
    _controller.StartFishing(facingDir, reeling);

  public void StopFishing() => _controller.StopFishing();

  public void ApplyHit(Vector2 facingDir) => _controller.ApplyHit(facingDir);

  public void UpdateAnimation(float dt, in AnimationInput input) => _controller.Update(dt, input);

  public void DrawIdlePortrait(SpriteBatch sb, Vector2 screenPos, Color tint, float scale)
  {
    var spec = FarmRpgAnimationSpecs.Idle;
    var layers = _assembler.Assemble(_appearance, _equipment, CharacterClip.Idle);
    CharacterRenderer.Draw(sb, screenPos, tint, scale, spec, CharacterClip.Idle, 0, FacingDirection.Down, layers);
  }

  public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale)
  {
    var draw = _controller.GetDrawState();
    if (draw.Clip == CharacterClip.Hurt && !_controller.IsHurtPlaying) return;
    if (draw.Clip is CharacterClip.Attack or CharacterClip.ShieldBash && !_controller.IsAttackPlaying) return;
    if (draw.Clip == CharacterClip.Cast && !_controller.IsCastPlaying) return;
    if (draw.Clip is CharacterClip.FishWait or CharacterClip.FishReel && !_controller.IsFishingPlaying) return;
    if (draw.Clip == CharacterClip.Death && !_controller.IsDead) return;

    var layers = _assembler.Assemble(_appearance, _equipment, draw.Clip);
    CharacterRenderer.Draw(sb, screenPos, tint, scale, draw.Spec, draw.Clip, draw.Frame, draw.Facing, layers);
  }

  public void DrawOutline(
    SpriteBatch sb, Vector2 screenPos, Color outline, float scale, float thickness)
  {
    var draw = _controller.GetDrawState();
    if (draw.Clip == CharacterClip.Hurt && !_controller.IsHurtPlaying) return;
    if (draw.Clip is CharacterClip.Attack or CharacterClip.ShieldBash && !_controller.IsAttackPlaying) return;
    if (draw.Clip == CharacterClip.Cast && !_controller.IsCastPlaying) return;
    if (draw.Clip is CharacterClip.FishWait or CharacterClip.FishReel && !_controller.IsFishingPlaying) return;

    var layers = _assembler.Assemble(_appearance, _equipment, draw.Clip);
    CharacterRenderer.DrawOutline(sb, screenPos, outline, scale, thickness,
      draw.Spec, draw.Clip, draw.Frame, draw.Facing, layers);
  }
}
