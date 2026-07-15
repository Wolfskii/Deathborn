using Microsoft.Xna.Framework;
using Deathborn.Client;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>
/// Owns animation time, clip selection, frame index, and facing.
/// Sprite layers read this state — no layer maintains its own timing.
/// </summary>
public sealed class AnimationController
{
  private string _bodyTypeId = CharacterAnimationCatalog.FarmRpg;
  private CharacterClip _locomotionClip = CharacterClip.Idle;
  private float _locomotionTimer;
  private int _locomotionFrame;
  private FacingDirection _facing = FacingDirection.Down;

  private bool _attackPlaying;
  private float _attackTimer;
  private int _attackFrame;
  private FacingDirection _attackFacing = FacingDirection.Down;

  private bool _hurtPlaying;
  private float _hurtTimer;
  private int _hurtFrame;
  private FacingDirection _hurtFacing = FacingDirection.Down;

  private bool _deathPlaying;
  private bool _deathComplete;
  private float _deathTimer;
  private int _deathFrame;
  private FacingDirection _deathFacing = FacingDirection.Down;

  public string BodyTypeId
  {
    get => _bodyTypeId;
    set => _bodyTypeId = string.IsNullOrEmpty(value) ? CharacterAnimationCatalog.FarmRpg : value;
  }

  public bool IsAttackPlaying => _attackPlaying;
  public bool IsHurtPlaying => _hurtPlaying;
  public bool IsDeathPlaying => _deathPlaying;
  public bool IsDeathComplete => _deathComplete;
  public bool IsDead => _deathPlaying || _deathComplete;
  public int AttackFrame => _attackFrame;

  public void BeginDeath(Vector2 facingDir)
  {
    _deathFacing = ResolveFacing(facingDir);
    _deathFrame = 0;
    _deathTimer = 0;
    _deathPlaying = true;
    _deathComplete = false;
    _attackPlaying = false;
    _hurtPlaying = false;
  }

  public void HoldDeathPose(Vector2 facingDir)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Death);
    _deathFacing = ResolveFacing(facingDir);
    _deathFrame = spec.FramesPerDirection - 1;
    _deathTimer = 0;
    _deathPlaying = false;
    _deathComplete = true;
  }

  public void StartAttack(Vector2 facingDir)
  {
    _attackFacing = ResolveFacing(facingDir);
    _attackFrame = 0;
    _attackTimer = 0;
    _attackPlaying = true;
  }

  public void ApplyHit(Vector2 facingDir)
  {
    _hurtFacing = ResolveFacing(facingDir);
    _hurtFrame = 0;
    _hurtTimer = 0;
    _hurtPlaying = true;
  }

  public void Update(float dt, in AnimationInput input)
  {
    if (input.IsDead)
    {
      if (_deathPlaying)
        UpdateDeath(dt);
      return;
    }

    if (_hurtPlaying)
      UpdateHurt(dt);

    if (input.IsWhirlwinding || input.IsDashing)
    {
      UpdateLocomotion(dt, input.FacingDir, true, true, Config.RunAnimSpeed);
      return;
    }

    if (_attackPlaying)
    {
      UpdateAttack(dt);
      return;
    }

    if (input.IsCasting)
    {
      UpdateIdle(dt, input.FacingDir);
      return;
    }

    if (input.IsMoving)
    {
      var speed = input.IsRunning ? Config.RunAnimSpeed : Config.WalkAnimSpeed;
      UpdateLocomotion(dt, input.FacingDir, true, input.IsRunning, speed);
      return;
    }

    UpdateIdle(dt, input.FacingDir);
  }

  public AnimationDrawState GetDrawState()
  {
    if (IsDead)
    {
      var deathSpec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Death);
      return new AnimationDrawState(CharacterClip.Death, deathSpec, _deathFrame, _deathFacing);
    }

    if (_hurtPlaying)
    {
      var hurtSpec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Hurt);
      return new AnimationDrawState(CharacterClip.Hurt, hurtSpec, _hurtFrame, _hurtFacing);
    }

    if (_attackPlaying)
    {
      var attackSpec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Attack);
      return new AnimationDrawState(CharacterClip.Attack, attackSpec, _attackFrame, _attackFacing);
    }

    var clip = _locomotionClip switch
    {
      CharacterClip.Run => CharacterClip.Run,
      CharacterClip.Walk => CharacterClip.Walk,
      _ => CharacterClip.Idle,
    };
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, clip);
    return new AnimationDrawState(clip, spec, _locomotionFrame, _facing);
  }

  private void UpdateIdle(float dt, Vector2 faceDir)
  {
    _locomotionClip = CharacterClip.Idle;
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Idle);

    if (faceDir.LengthSquared() > 0.01f)
    {
      var next = ResolveFacing(faceDir);
      if (next != _facing)
      {
        _facing = next;
        _locomotionFrame = 0;
        _locomotionTimer = 0;
      }
    }

    _locomotionTimer += dt;
    while (_locomotionTimer >= spec.FrameDuration)
    {
      _locomotionTimer -= spec.FrameDuration;
      _locomotionFrame = (_locomotionFrame + 1) % spec.FramesPerDirection;
    }
  }

  private void UpdateLocomotion(float dt, Vector2 moveDir, bool isMoving, bool useRunAnim, float animSpeed)
  {
    _locomotionClip = useRunAnim ? CharacterClip.Run : CharacterClip.Walk;
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, _locomotionClip);

    if (moveDir.LengthSquared() > 0.01f)
      _facing = ResolveFacing(moveDir);

    if (!isMoving)
    {
      _locomotionTimer = 0;
      _locomotionFrame = 0;
      return;
    }

    var frameDuration = spec.FrameDuration / MathF.Max(0.1f, animSpeed);
    _locomotionTimer += dt;
    while (_locomotionTimer >= frameDuration)
    {
      _locomotionTimer -= frameDuration;
      _locomotionFrame = (_locomotionFrame + 1) % spec.FramesPerDirection;
    }
  }

  private void UpdateAttack(float dt)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Attack);
    _attackTimer += dt;
    while (_attackTimer >= spec.FrameDuration)
    {
      _attackTimer -= spec.FrameDuration;
      _attackFrame++;
      if (_attackFrame >= spec.FramesPerDirection)
      {
        _attackPlaying = false;
        _attackFrame = 0;
        return;
      }
    }
  }

  private void UpdateHurt(float dt)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Hurt);
    _hurtTimer += dt;
    while (_hurtTimer >= spec.FrameDuration)
    {
      _hurtTimer -= spec.FrameDuration;
      _hurtFrame++;
      if (_hurtFrame >= spec.FramesPerDirection)
      {
        _hurtPlaying = false;
        _hurtFrame = spec.FramesPerDirection - 1;
        return;
      }
    }
  }

  private void UpdateDeath(float dt)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Death);
    _deathTimer += dt;
    while (_deathTimer >= spec.FrameDuration)
    {
      _deathTimer -= spec.FrameDuration;
      _deathFrame++;
      if (_deathFrame >= spec.FramesPerDirection - 1)
      {
        _deathFrame = spec.FramesPerDirection - 1;
        _deathPlaying = false;
        _deathComplete = true;
        return;
      }
    }
  }

  public static FacingDirection ResolveFacing(Vector2 dir) => FacingResolver.Resolve(dir);
}

public readonly struct AnimationDrawState(
  CharacterClip clip,
  AnimationSpecification spec,
  int frame,
  FacingDirection facing)
{
  public CharacterClip Clip { get; } = clip;
  public AnimationSpecification Spec { get; } = spec;
  public int Frame { get; } = frame;
  public FacingDirection Facing { get; } = facing;
}
