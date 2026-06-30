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
    private float _abilityLockTimer;
    private float _bandageHoTTimer;
    private float _bandageAnim;

    public long Id;
    public string Name = "";
    public Vector2 Position;
    public Vector2 Target;
    public bool IsLocal;
    public Vector2 MoveDir;
    public Vector2 InputDir;
    /// <summary>Mouse-facing while idle (local player only).</summary>
    public Vector2 AimDir;

    public bool IsAttacking => AttackAnim.IsPlaying;
    public bool IsCasting => _abilityLockTimer > 0f;
    public bool IsBusy => IsAttacking || IsCasting;
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

        var def = MeleeAbilityDefinitions.Slash;
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
            case PlayerActions.CastPoisonCloud:
                StartAbilityLock(action switch
                {
                    PlayerActions.CastIceShard => Config.IceShardCastLockDuration,
                    PlayerActions.CastArcBolt => Config.ArcBoltCastLockDuration,
                    PlayerActions.CastPoisonCloud => Config.PoisonCloudCastLockDuration,
                    _ => Config.FireballCastLockDuration,
                });
                if (facingDir.LengthSquared() > 0.01f)
                    MoveDir = Vector2.Normalize(facingDir);
                break;
            case PlayerActions.UseBandage:
                StartBandageHoT();
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

        Position = Vector2.Lerp(Position, Target, MathHelper.Clamp(dt * Config.PlayerLerpSpeed, 0, 1));
        UpdateBandageVisual(dt);

        if (IsDying || IsCorpse)
        {
            if (IsDying)
                DeathAnim.Update(dt);
            return;
        }

        if (HurtAnim.IsPlaying)
            HurtAnim.Update(dt);

        if (IsAttacking)
        {
            AttackAnim.Update(dt);
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
            RunAnim.Update(dt, FacingDir, true);
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
                var crossY = screenPos.Y + (-Radius - 20f) * zoom;
                CorpseMarkerDraw.DrawCross(sb, new Vector2(screenPos.X, crossY), zoom);
            }
            return;
        }

        if (IsHurt)
            HurtAnim.Draw(sb, screenPos, tint, scale);
        else if (IsAttacking)
            AttackAnim.Draw(sb, screenPos, tint, scale);
        else if (IsMoving)
            RunAnim.Draw(sb, screenPos, tint, scale);
        else
            IdleAnim.Draw(sb, screenPos, tint, scale);

        var nameTop = screenPos.Y + (-Radius - 28f) * zoom;
        if (!IsLocal)
        {
            var label = font.MeasureString(Name);
            var namePos = new Vector2(screenPos.X - label.X / 2f, nameTop);
            sb.DrawString(font, Name, namePos, Color.White);
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
        };
        if (s.HpMax > 0)
            entity.SyncStats((float)s.Hp, (float)s.HpMax);
        return entity;
    }
}
