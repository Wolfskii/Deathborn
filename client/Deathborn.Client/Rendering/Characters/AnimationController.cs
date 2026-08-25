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
  private CharacterClip _lastLocomotionClip = CharacterClip.Idle;
  private FacingDirection _lastLocomotionFacing = FacingDirection.Down;

  private bool _attackPlaying;
  private float _attackTimer;
  private int _attackFrame;
  private FacingDirection _attackFacing = FacingDirection.Down;
  private CharacterClip _attackClip = CharacterClip.Attack;

  private bool _castPlaying;
  private float _castTimer;
  private int _castFrame;
  private FacingDirection _castFacing = FacingDirection.Down;

  private bool _fishingPlaying;
  private CharacterClip _fishingClip = CharacterClip.FishWait;
  private float _fishingTimer;
  private int _fishingFrame;
  private FacingDirection _fishingFacing = FacingDirection.Down;

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
  public bool IsCastPlaying => _castPlaying;
  public bool IsFishingPlaying => _fishingPlaying;
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
    _castPlaying = false;
    _fishingPlaying = false;
    _hurtPlaying = false;
  }

  public void BeginCast(Vector2 facingDir)
  {
    _castFacing = ResolveFacing(facingDir);
    _castFrame = 0;
    _castTimer = 0;
    _castPlaying = true;
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

  /// <summary>Freeze on idle frame 0 — facing updates, no locomotion cycle.</summary>
  public void HoldIdlePose(Vector2 facingDir)
  {
    _facing = ResolveFacing(facingDir);
    _locomotionClip = CharacterClip.Idle;
    _locomotionFrame = 0;
    _locomotionTimer = 0;
    _lastLocomotionClip = CharacterClip.Idle;
    _lastLocomotionFacing = _facing;
    _attackPlaying = false;
    _castPlaying = false;
    _fishingPlaying = false;
    _hurtPlaying = false;
  }

  public void StartFishing(Vector2 facingDir, bool reeling)
  {
    _fishingFacing = ResolveFacing(facingDir);
    var next = reeling ? CharacterClip.FishReel : CharacterClip.FishWait;
    if (!_fishingPlaying || next != _fishingClip)
    {
      _fishingFrame = 0;
      _fishingTimer = 0;
    }
    _fishingClip = next;
    _fishingPlaying = true;
    _attackPlaying = false;
    _castPlaying = false;
  }

  public void StopFishing()
  {
    _fishingPlaying = false;
  }

  public void StartAttack(Vector2 facingDir, CharacterClip clip = CharacterClip.Attack)
  {
    _attackClip = clip;
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
      UpdateLocomotion(dt, input.LocomotionDir, true, true, Config.RunAnimSpeed);
      return;
    }

    if (_attackPlaying)
    {
      UpdateAttack(dt);
      return;
    }

    if (input.IsFishing)
    {
      StartFishing(input.FacingDir, input.FishingReeling);
      UpdateFishing(dt, input.FacingDir);
      return;
    }

    _fishingPlaying = false;

    if (input.IsCasting)
    {
      if (!_castPlaying)
        BeginCast(input.FacingDir);
      UpdateCast(dt, input.FacingDir);
      return;
    }

    _castPlaying = false;

    if (input.IsMoving)
    {
      var speed = input.IsRunning ? Config.RunAnimSpeed : Config.WalkAnimSpeed;
      UpdateLocomotion(dt, input.LocomotionDir, true, input.IsRunning, speed);
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
      var attackSpec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, _attackClip);
      return new AnimationDrawState(_attackClip, attackSpec, _attackFrame, _attackFacing);
    }

    if (_castPlaying)
    {
      var castSpec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Cast);
      return new AnimationDrawState(CharacterClip.Cast, castSpec, _castFrame, _castFacing);
    }

    if (_fishingPlaying)
    {
      var fishSpec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, _fishingClip);
      return new AnimationDrawState(_fishingClip, fishSpec, _fishingFrame, _fishingFacing);
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

  private void UpdateFishing(float dt, Vector2 faceDir)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, _fishingClip);
    if (faceDir.LengthSquared() > 0.01f)
      _fishingFacing = ResolveFacing(faceDir);

    _fishingTimer += dt;
    while (_fishingTimer >= spec.FrameDuration)
    {
      _fishingTimer -= spec.FrameDuration;
      _fishingFrame = (_fishingFrame + 1) % spec.FramesPerDirection;
    }
  }

  private void UpdateIdle(float dt, Vector2 faceDir)
  {
    if (_lastLocomotionClip != CharacterClip.Idle)
    {
      _lastLocomotionClip = CharacterClip.Idle;
      _locomotionFrame = 0;
      _locomotionTimer = 0;
    }

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
    var nextClip = useRunAnim ? CharacterClip.Run : CharacterClip.Walk;
    var nextFacing = moveDir.LengthSquared() > 0.01f ? ResolveFacing(moveDir) : _facing;

    if (nextClip != _lastLocomotionClip || nextFacing != _lastLocomotionFacing)
    {
      _locomotionFrame = 0;
      _locomotionTimer = 0;
      _lastLocomotionClip = nextClip;
      _lastLocomotionFacing = nextFacing;
    }

    _locomotionClip = nextClip;
    _facing = nextFacing;

    if (!isMoving)
    {
      _locomotionTimer = 0;
      _locomotionFrame = 0;
      return;
    }

    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, _locomotionClip);
    var frameDuration = spec.FrameDuration / MathF.Max(0.1f, animSpeed);
    _locomotionTimer += dt;

    var advanced = 0;
    while (_locomotionTimer >= frameDuration && advanced < 2)
    {
      _locomotionTimer -= frameDuration;
      _locomotionFrame = (_locomotionFrame + 1) % spec.FramesPerDirection;
      advanced++;
    }
  }

  private void UpdateCast(float dt, Vector2 faceDir)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, CharacterClip.Cast);

    if (faceDir.LengthSquared() > 0.01f)
      _castFacing = ResolveFacing(faceDir);

    _castTimer += dt;
    while (_castTimer >= spec.FrameDuration)
    {
      _castTimer -= spec.FrameDuration;
      _castFrame = (_castFrame + 1) % spec.FramesPerDirection;
    }
  }

  private void UpdateAttack(float dt)
  {
    var spec = CharacterAnimationCatalog.GetSpec(_bodyTypeId, _attackClip);
    var frameLimit = FarmRpgAnimationSpecs.FramesFor(_attackClip, _attackFacing);
    _attackTimer += dt;
    while (_attackTimer >= spec.FrameDuration)
    {
      _attackTimer -= spec.FrameDuration;
      _attackFrame++;
      if (_attackFrame >= frameLimit)
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
