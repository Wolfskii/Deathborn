using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class PlayerEntity
{
    public const float Radius = 12f;
    public const float SpriteDrawScale = 1.5f;
    private const float SpriteScale = SpriteDrawScale;

    private FourDirectionRunAnimation? _runAnim;
    private FourDirectionIdleAnimation? _idleAnim;
    private FourDirectionAttackAnimation? _attackAnim;
    private FourDirectionDeathAnimation? _deathAnim;
    private FourDirectionHurtAnimation? _hurtAnim;
    private FourDirectionRunAnimation RunAnim => _runAnim ??= CharacterSprites.CreateSwordsmanRun();
    private FourDirectionIdleAnimation IdleAnim => _idleAnim ??= CharacterSprites.CreateSwordsmanIdle();
    private FourDirectionAttackAnimation AttackAnim => _attackAnim ??= CharacterSprites.CreateSwordsmanAttack();
    private FourDirectionDeathAnimation DeathAnim => _deathAnim ??= CharacterSprites.CreateSwordsmanDeath();
    private FourDirectionHurtAnimation HurtAnim => _hurtAnim ??= CharacterSprites.CreateSwordsmanHurt();
    private readonly PlayerChatBubble _chatBubble = new();
    private readonly ThinkingBubble _thinking = new();
    private readonly HashSet<long> _meleeHitThisSwing = [];
    private readonly HashSet<long> _whirlwindHit = [];
    private readonly HashSet<long> _dashHit = [];
    private MeleeAbilityDefinition? _activeMeleeDef;
    private float _abilityLockTimer;
    private float _bandageHoTTimer;
    private float _bandageAnim;
    private float _whirlwindTimer;
    private bool _whirlwindHitPulse;
    private bool _isDashing;
    private float _dashTimer;
    private float _dashDuration;
    private Vector2 _dashStart;
    private Vector2 _dashEnd;

    public long Id;
    public string Name = "";
    public Vector2 Position;
    public Vector2 Target;
    public bool IsLocal;
    public long InsideHouseId;
    public string? HeadCosmetic;
    public Vector2 MoveDir;
    public Vector2 InputDir;
    /// <summary>Mouse-facing while idle (local player only).</summary>
    public Vector2 AimDir;
    /// <summary>Local sprint held (Shift) with enough stamina.</summary>
    public bool IsRunning;

    public bool IsAttacking => AttackAnim.IsPlaying;
    public bool IsCasting => _abilityLockTimer > 0f;
    public bool IsDashing => _isDashing;
    public bool IsWhirlwinding => _whirlwindTimer > 0f;
    public bool IsBusy => IsAttacking || IsCasting || IsDashing || IsWhirlwinding;
    public bool IsHurt => HurtAnim.IsPlaying;
    public bool IsTyping
    {
        get => _thinking.Active;
        set => _thinking.Active = value;
    }

    public bool IsBandaging => _bandageHoTTimer > 0f;
    public bool IsDying => DeathAnim.IsPlaying;
    public bool IsCorpse => DeathAnim.IsComplete;
    public bool IsDead => IsDying || IsCorpse;

    public CharacterStats Stats { get; } = CharacterStats.CreateStarter();

    public void BeginDeath(Vector2? facing = null)
    {
        if (IsDead) return;

        var dir = facing ?? FacingDir;
        if (dir.LengthSquared() > 0.01f)
            MoveDir = Vector2.Normalize(dir);
        DeathAnim.Start(dir);
    }

    public void StartBandageHoT() => _bandageHoTTimer = Config.BandageDuration;

    public static Vector2 CardinalFacing(Vector2 dir)
    {
        if (dir.LengthSquared() < 0.01f) return new Vector2(0, 1);
        return FourDirectionRunAnimation.ResolveDirection(dir) switch
        {
            FacingDirection.Right => Vector2.UnitX,
            FacingDirection.Left => -Vector2.UnitX,
            FacingDirection.Up => new Vector2(0, -1),
            _ => new Vector2(0, 1),
        };
    }

    public Vector2 GetProjectileSpawnPoint(Vector2? direction = null)
    {
        var dir = CardinalFacing(direction ?? FacingDir);
        var torso = Position + new Vector2(0, Config.CastTorsoOffsetY);
        return torso + dir * (Radius + Config.CastSpawnDistance);
    }

    public void ShowChatMessage(string text) => _chatBubble.Show(text);

    public void SyncStats(float hp, float hpMax)
    {
        Stats.Hp = hp;
        Stats.HpMax = hpMax;
    }

    public void ApplyHit(int damage)
    {
        if (damage <= 0 || IsDead) return;
        HurtAnim.Start(FacingDir);
    }

    public void CheckLocalMeleeHits(
        IReadOnlyDictionary<long, PlayerEntity> players,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || IsDead || !IsAttacking) return;

        var def = _activeMeleeDef ?? MeleeAbilityDefinitions.Slash;
        var frame = AttackAnim.Frame;
        if (frame < def.HitFrameStart || frame > def.HitFrameEnd) return;

        var facing = CardinalFacing(FacingDir);
        foreach (var (id, other) in players)
        {
            if (id == Id || _meleeHitThisSwing.Contains(id) || other.IsDead) continue;
            if (!IsInMeleeArc(Position, facing, other.Position, def.Range, def.HalfWidth)) continue;

            _meleeHitThisSwing.Add(id);
            reportHit(id, def.Damage, def.Id);
        }
    }

    private static bool IsInMeleeArc(
        Vector2 attackerPos, Vector2 facing, Vector2 targetPos, float range, float halfWidth)
    {
        var toTarget = targetPos - attackerPos;
        var forward = Vector2.Dot(toTarget, facing);
        if (forward < 0f || forward > range + PlayerEntity.Radius) return false;

        var lateral = MathF.Abs(toTarget.X * facing.Y - toTarget.Y * facing.X);
        return lateral <= halfWidth + PlayerEntity.Radius;
    }

    public void SetTarget(Vector2 pos)
    {
        if (Vector2.DistanceSquared(pos, Target) > 0.01f)
            MoveDir = Vector2.Normalize(pos - Position);
        Target = pos;
    }

    public bool StartMeleeAbility(MeleeAbilityDefinition def, Vector2? facing = null)
    {
        _activeMeleeDef = def;
        return StartAttack(facing);
    }

    public bool StartWhirlwind()
    {
        if (IsBusy || IsDead) return false;
        _whirlwindTimer = Config.WhirlwindDuration;
        _whirlwindHit.Clear();
        _whirlwindHitPulse = false;
        StartAbilityLock(Config.WhirlwindDuration);
        return true;
    }

    public bool StartDash(Vector2 dir, float distance, float duration)
    {
        if (IsBusy || IsDead || _isDashing) return false;
        var facing = CardinalFacing(dir);
        MoveDir = facing;
        _dashStart = Position;
        _dashEnd = Position + facing * distance;
        _dashDuration = duration;
        _dashTimer = duration;
        _dashHit.Clear();
        _isDashing = true;
        StartAbilityLock(duration);
        return true;
    }

    public void CheckWhirlwindHits(
        IReadOnlyDictionary<long, PlayerEntity> players,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !IsWhirlwinding || _whirlwindHitPulse) return;
        if (_whirlwindTimer > Config.WhirlwindDuration * 0.55f) return;

        _whirlwindHitPulse = true;
        var radius = Config.WhirlwindRadius;
        var radiusSq = radius * radius;
        foreach (var (id, other) in players)
        {
            if (id == Id || other.IsDead || _whirlwindHit.Contains(id)) continue;
            if (Vector2.DistanceSquared(Position, other.Position) > radiusSq) continue;
            _whirlwindHit.Add(id);
            reportHit(id, Config.WhirlwindDamage, "whirlwind");
        }
    }

    public void CheckDashHits(
        IReadOnlyDictionary<long, PlayerEntity> players,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !_isDashing) return;
        var t = 1f - MathF.Max(0f, _dashTimer) / MathF.Max(0.001f, _dashDuration);
        if (t < 0.35f || t > 0.85f) return;

        var radiusSq = (PlayerEntity.Radius + 18f) * (PlayerEntity.Radius + 18f);
        foreach (var (id, other) in players)
        {
            if (id == Id || other.IsDead || _dashHit.Contains(id)) continue;
            if (Vector2.DistanceSquared(Position, other.Position) > radiusSq) continue;
            _dashHit.Add(id);
            reportHit(id, Config.WarriorDashDamage, "warrior_dash");
        }
    }
    public void CheckLocalBossMeleeHits(
        IReadOnlyDictionary<long, BossEntity> bosses,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || IsDead || !IsAttacking || bosses.Count == 0) return;

        var def = _activeMeleeDef ?? MeleeAbilityDefinitions.Slash;
        var frame = AttackAnim.Frame;
        if (frame < def.HitFrameStart || frame > def.HitFrameEnd) return;

        var facing = CardinalFacing(FacingDir);
        foreach (var (id, boss) in bosses)
        {
            if (_meleeHitThisSwing.Contains(id)) continue;
            if (!IsInMeleeArc(Position, facing, boss.Position, def.Range, def.HalfWidth + boss.Radius * 0.4f)) continue;
            _meleeHitThisSwing.Add(id);
            reportHit(id, def.Damage, def.Id);
        }
    }

    public void CheckWhirlwindBossHits(
        IReadOnlyDictionary<long, BossEntity> bosses,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !IsWhirlwinding || !_whirlwindHitPulse || bosses.Count == 0) return;
        var radius = Config.WhirlwindRadius + BossEntity.DefaultRadius;
        var radiusSq = radius * radius;
        foreach (var (id, boss) in bosses)
        {
            if (_whirlwindHit.Contains(id)) continue;
            if (Vector2.DistanceSquared(Position, boss.Position) > radiusSq) continue;
            _whirlwindHit.Add(id);
            reportHit(id, Config.WhirlwindDamage, "whirlwind");
        }
    }

    public void CheckDashBossHits(
        IReadOnlyDictionary<long, BossEntity> bosses,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !_isDashing || bosses.Count == 0) return;
        var t = 1f - MathF.Max(0f, _dashTimer) / MathF.Max(0.001f, _dashDuration);
        if (t < 0.35f || t > 0.85f) return;

        var radiusSq = (PlayerEntity.Radius + BossEntity.DefaultRadius) * (PlayerEntity.Radius + BossEntity.DefaultRadius);
        foreach (var (id, boss) in bosses)
        {
            if (_dashHit.Contains(id)) continue;
            if (Vector2.DistanceSquared(Position, boss.Position) > radiusSq) continue;
            _dashHit.Add(id);
            reportHit(id, Config.WarriorDashDamage, "warrior_dash");
        }
    }

    public bool StartAttack(Vector2? facing = null)
    {
        if (IsBusy || IsDead) return false;
        _meleeHitThisSwing.Clear();
        var dir = facing ?? FacingDir;
        if (dir.LengthSquared() > 0.01f)
            MoveDir = Vector2.Normalize(dir);
        AttackAnim.Start(dir);
        return true;
    }

    public void StartAbilityLock(float duration)
    {
        if (duration <= 0f) return;
        _abilityLockTimer = MathF.Max(_abilityLockTimer, duration);
    }

    /// <summary>Play a networked action on this player (remote or echoed local).</summary>
    public void PlayAction(string action, Vector2 facingDir, string? targetId = null)
    {
        switch (action)
        {
            case PlayerActions.MeleeAttack:
                StartAttack(facingDir);
                break;
            case PlayerActions.CastFireball:
            case PlayerActions.CastIceShard:
            case PlayerActions.CastArcBolt:
            case PlayerActions.CastBloodBolt:
            case PlayerActions.CastPoisonCloud:
                StartAbilityLock(action switch
                {
                    PlayerActions.CastIceShard => Config.IceShardCastLockDuration,
                    PlayerActions.CastArcBolt => Config.ArcBoltCastLockDuration,
                    PlayerActions.CastBloodBolt => Config.BloodBoltCastLockDuration,
                    PlayerActions.CastPoisonCloud => Config.PoisonCloudCastLockDuration,
                    _ => Config.FireballCastLockDuration,
                });
                if (facingDir.LengthSquared() > 0.01f)
                    MoveDir = Vector2.Normalize(facingDir);
                break;
            case PlayerActions.UseBandage:
                StartBandageHoT();
                break;
            case PlayerActions.ShieldBash:
                StartMeleeAbility(MeleeAbilityDefinitions.ShieldBash, facingDir);
                break;
            case PlayerActions.Whirlwind:
                StartWhirlwind();
                break;
            case PlayerActions.WarriorDash:
                StartDash(facingDir, Config.WarriorDashDistance, Config.WarriorDashDuration);
                break;
            case PlayerActions.BattleShout:
            case PlayerActions.IronSkin:
            case PlayerActions.SecondWind:
                StartAbilityLock(0.35f);
                break;
            case PlayerActions.HunterMark:
                StartAbilityLock(0.3f);
                if (facingDir.LengthSquared() > 0.01f)
                    MoveDir = Vector2.Normalize(facingDir);
                break;
            case PlayerActions.Interact:
                // Interaction animations can hook in here when added.
                break;
        }
    }

    public bool IsMoving =>
        !IsDead &&
        !IsHurt &&
        !IsBusy &&
        ((IsLocal && InputDir.LengthSquared() > 0.01f) ||
         Vector2.DistanceSquared(Position, Target) > 0.5f);

    public bool IsWalking => IsMoving && !IsRunning;

    public Vector2 FacingDir
    {
        get
        {
            if (IsLocal && InputDir.LengthSquared() > 0.01f) return InputDir;
            if (IsLocal && AimDir.LengthSquared() > 0.01f) return AimDir;
            if (MoveDir.LengthSquared() > 0.01f) return MoveDir;
            return new Vector2(0, 1);
        }
    }

    public void Update(float dt)
    {
        if (_abilityLockTimer > 0f)
            _abilityLockTimer = MathF.Max(0f, _abilityLockTimer - dt);

        if (_whirlwindTimer > 0f)
            _whirlwindTimer = MathF.Max(0f, _whirlwindTimer - dt);

        if (IsDead)
        {
            if (IsDying)
                DeathAnim.Update(dt);
            return;
        }

        if (_isDashing)
        {
            _dashTimer = MathF.Max(0f, _dashTimer - dt);
            var t = 1f - _dashTimer / MathF.Max(0.001f, _dashDuration);
            Position = Vector2.Lerp(_dashStart, _dashEnd, t);
            Target = _dashEnd;
            if (_dashTimer <= 0f)
                _isDashing = false;
        }
        else
        {
            Position = Vector2.Lerp(Position, Target, MathHelper.Clamp(dt * Config.PlayerLerpSpeed, 0, 1));
        }

        UpdateBandageVisual(dt);

        if (HurtAnim.IsPlaying)
            HurtAnim.Update(dt);

        if (IsWhirlwinding)
        {
            RunAnim.Update(dt, FacingDir, true, Config.RunAnimSpeed);
            _chatBubble.Update(dt);
            _thinking.Update(dt);
            return;
        }

        if (_isDashing)
        {
            RunAnim.Update(dt, FacingDir, true, Config.RunAnimSpeed);
            _chatBubble.Update(dt);
            _thinking.Update(dt);
            return;
        }

        if (IsAttacking)
        {
            AttackAnim.Update(dt);
            if (!AttackAnim.IsPlaying)
                _activeMeleeDef = null;
            _chatBubble.Update(dt);
            _thinking.Update(dt);
            return;
        }

        if (IsCasting)
        {
            IdleAnim.Update(dt, FacingDir);
            _chatBubble.Update(dt);
            _thinking.Update(dt);
            return;
        }

        if (IsMoving)
        {
            var animSpeed = IsRunning ? Config.RunAnimSpeed : Config.WalkAnimSpeed;
            RunAnim.Update(dt, FacingDir, true, animSpeed);
        }
        else
            IdleAnim.Update(dt, FacingDir);

        _chatBubble.Update(dt);
        _thinking.Update(dt);
    }

    private void UpdateBandageVisual(float dt)
    {
        if (_bandageHoTTimer <= 0f) return;
        _bandageHoTTimer -= dt;
        _bandageAnim += dt;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        var tint = IsLocal ? Color.White : new Color(0.92f, 0.82f, 0.78f);
        var scale = SpriteScale * zoom;

        if (IsDead)
        {
            DeathAnim.Draw(sb, screenPos, tint, scale);
            if (IsCorpse)
            {
                var markerY = screenPos.Y + (-Radius - 28f) * zoom;
                CorpseMarkerDraw.DrawCorpseMarker(sb, new Vector2(screenPos.X, markerY), zoom);
            }
            return;
        }

        if (IsHurt)
            HurtAnim.Draw(sb, screenPos, tint, scale);
        else if (IsWhirlwinding)
            RunAnim.Draw(sb, screenPos, tint, scale);
        else if (IsDashing)
            RunAnim.Draw(sb, screenPos, tint, scale);
        else if (IsAttacking)
            AttackAnim.Draw(sb, screenPos, tint, scale);
        else if (IsMoving)
            RunAnim.Draw(sb, screenPos, tint, scale);
        else
            IdleAnim.Draw(sb, screenPos, tint, scale);

        PlayerCosmeticDraw.DrawHead(sb, HeadCosmetic, screenPos, FacingDir, zoom);

        var nameTop = screenPos.Y + (-Radius - 28f) * zoom;
        if (!IsLocal)
        {
            var label = SpriteFontSafe.MeasureString(font, Name);
            var namePos = new Vector2(screenPos.X - label.X / 2f, nameTop);
            SpriteFontSafe.DrawString(sb, font, Name, namePos, Color.White);
        }
        var thinkingAnchor = new Vector2(screenPos.X, nameTop - 22f * zoom);
        if (_thinking.Active)
            _thinking.Draw(sb, thinkingAnchor, zoom);

        if (_chatBubble.IsVisible)
        {
            var speechTarget = new Vector2(screenPos.X, nameTop - 6f * zoom);
            _chatBubble.Draw(sb, font, speechTarget, zoom);
        }

        if (IsBandaging)
            DrawBandageHoT(sb, screenPos, zoom);
    }

    public static void DrawHunterMark(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var y = screenPos.Y + (-Radius - 34f) * zoom;
        var c = new Vector2(screenPos.X, y);
        DrawPrimitives.DrawCircleOutline(sb, c, 10f * zoom, new Color(0.95f, 0.35f, 0.4f, 0.9f), 16, 2f);
        DrawPrimitives.FillCircle(sb, c, 4f * zoom, new Color(0.95f, 0.25f, 0.3f, 0.85f));
    }

    /// <summary>Sprite-fitted hover border using the current animation frame.</summary>
    public void DrawHoverHighlight(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (IsDead || IsLocal) return;

        var scale = SpriteScale * zoom;
        var outerThick = Math.Max(2.5f, 3.1f * zoom);
        var innerThick = Math.Max(2f, 2.5f * zoom);

        DrawHoverOutlinePass(sb, screenPos, new Color(255, 255, 255, 0.45f), scale, outerThick);
        DrawHoverOutlinePass(sb, screenPos, new Color(255, 252, 185, 1f), scale, innerThick);
        DrawHoverOutlinePass(sb, screenPos, new Color(255, 238, 120, 0.92f), scale, innerThick * 0.72f);
    }

    private void DrawHoverOutlinePass(
        SpriteBatch sb, Vector2 screenPos, Color outline, float scale, float thickness)
    {
        if (IsHurt)
            HurtAnim.DrawOutline(sb, screenPos, outline, scale, thickness);
        else if (IsWhirlwinding || IsDashing)
            RunAnim.DrawOutline(sb, screenPos, outline, scale, thickness);
        else if (IsAttacking)
            AttackAnim.DrawOutline(sb, screenPos, outline, scale, thickness);
        else if (IsMoving)
            RunAnim.DrawOutline(sb, screenPos, outline, scale, thickness);
        else
            IdleAnim.DrawOutline(sb, screenPos, outline, scale, thickness);
    }

    private void DrawBandageHoT(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        for (var i = 0; i < 4; i++)
        {
            var phase = (_bandageAnim * 1.2f + i * 0.25f) % 1f;
            var offset = new Vector2(MathF.Sin(_bandageAnim * 3f + i) * 8f * zoom, -phase * 28f * zoom);
            var pos = screenPos + new Vector2(0, -Radius * zoom) + offset;
            var alpha = (1f - phase) * 0.75f;
            DrawPrimitives.FillCircle(sb, pos, 3f * zoom, new Color(0.45f, 0.95f, 0.5f, alpha));
        }

        var pulse = 1f + MathF.Sin(_bandageAnim * 6f) * 0.15f;
        DrawPrimitives.DrawCircleOutline(sb, screenPos + new Vector2(0, -Radius * 0.5f * zoom),
            14f * zoom * pulse, new Color(0.5f, 0.95f, 0.55f, 0.45f), 20, 1.5f);
    }

    public static PlayerEntity FromState(PlayerState s, bool isLocal)
    {
        var entity = new PlayerEntity
        {
            Id = s.Id,
            Name = s.Name,
            Position = new Vector2((float)s.X, (float)s.Y),
            Target = new Vector2((float)s.X, (float)s.Y),
            IsLocal = isLocal,
            InsideHouseId = s.InsideHouseId,
            HeadCosmetic = string.IsNullOrEmpty(s.HeadCosmetic) ? null : s.HeadCosmetic,
        };
        if (s.HpMax > 0)
            entity.SyncStats((float)s.Hp, (float)s.HpMax);
        return entity;
    }
}
