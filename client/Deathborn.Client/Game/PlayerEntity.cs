using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class PlayerEntity
{
    public const float Radius = 12f;
    private const float SpriteScale = 1.5f;

    private FourDirectionRunAnimation? _runAnim;
    private FourDirectionIdleAnimation? _idleAnim;
    private FourDirectionAttackAnimation? _attackAnim;
    private FourDirectionRunAnimation RunAnim => _runAnim ??= CharacterSprites.CreateSwordsmanRun();
    private FourDirectionIdleAnimation IdleAnim => _idleAnim ??= CharacterSprites.CreateSwordsmanIdle();
    private FourDirectionAttackAnimation AttackAnim => _attackAnim ??= CharacterSprites.CreateSwordsmanAttack();
    private readonly PlayerChatBubble _chatBubble = new();
    private readonly ThinkingBubble _thinking = new();
    private readonly HashSet<long> _meleeHitThisSwing = [];
    private float _hitBlinkTimer;
    private float _hitBlinkFlashAccum;
    private bool _hitBlinkVisible = true;
    private float _abilityLockTimer;

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
    public bool IsHitBlinking => _hitBlinkTimer > 0f;
    public bool IsTyping
    {
        get => _thinking.Active;
        set => _thinking.Active = value;
    }

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

    public void ApplyHit(int damage)
    {
        if (damage <= 0) return;
        _hitBlinkTimer = Config.HitBlinkDuration;
        _hitBlinkFlashAccum = 0f;
        _hitBlinkVisible = true;
    }

    public void CheckLocalMeleeHits(
        IReadOnlyDictionary<long, PlayerEntity> players,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !IsAttacking) return;

        var def = MeleeAbilityDefinitions.Slash;
        var frame = AttackAnim.Frame;
        if (frame < def.HitFrameStart || frame > def.HitFrameEnd) return;

        var facing = CardinalFacing(FacingDir);
        foreach (var (id, other) in players)
        {
            if (id == Id || _meleeHitThisSwing.Contains(id)) continue;
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
        if (IsBusy) return false;
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
                StartAbilityLock(Config.FireballCastLockDuration);
                if (facingDir.LengthSquared() > 0.01f)
                    MoveDir = Vector2.Normalize(facingDir);
                break;
            case PlayerActions.Interact:
                // Interaction animations can hook in here when added.
                break;
        }
    }

    public bool IsMoving =>
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

        if (IsAttacking)
        {
            AttackAnim.Update(dt);
            _chatBubble.Update(dt);
            _thinking.Update(dt);
            UpdateHitBlink(dt);
            return;
        }

        if (IsCasting)
        {
            IdleAnim.Update(dt, FacingDir);
            _chatBubble.Update(dt);
            _thinking.Update(dt);
            UpdateHitBlink(dt);
            return;
        }

        if (IsMoving)
            RunAnim.Update(dt, FacingDir, true);
        else
            IdleAnim.Update(dt, FacingDir);

        _chatBubble.Update(dt);
        _thinking.Update(dt);
        UpdateHitBlink(dt);
    }

    private void UpdateHitBlink(float dt)
    {
        if (_hitBlinkTimer <= 0f)
        {
            _hitBlinkVisible = true;
            return;
        }

        _hitBlinkTimer -= dt;
        _hitBlinkFlashAccum += dt;
        if (_hitBlinkFlashAccum >= Config.HitBlinkInterval)
        {
            _hitBlinkFlashAccum = 0f;
            _hitBlinkVisible = !_hitBlinkVisible;
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        var tint = IsLocal ? Color.White : new Color(0.92f, 0.82f, 0.78f);
        var scale = SpriteScale * zoom;

        if (_hitBlinkVisible)
        {
            if (IsAttacking)
                AttackAnim.Draw(sb, screenPos, tint, scale);
            else if (IsMoving)
                RunAnim.Draw(sb, screenPos, tint, scale);
            else
                IdleAnim.Draw(sb, screenPos, tint, scale);
        }

        var nameY = screenPos.Y + (-Radius - 28f) * zoom;
        var label = font.MeasureString(Name);
        var namePos = new Vector2(screenPos.X - label.X / 2f, nameY);
        sb.DrawString(font, Name, namePos, Color.White);

        var nameTop = nameY;
        var thinkingAnchor = new Vector2(screenPos.X, nameTop - 22f * zoom);
        if (_thinking.Active)
            _thinking.Draw(sb, thinkingAnchor, zoom);

        if (_chatBubble.IsVisible)
        {
            var speechTarget = new Vector2(screenPos.X, nameTop - 6f * zoom);
            _chatBubble.Draw(sb, font, speechTarget, zoom);
        }
    }

    public static PlayerEntity FromState(PlayerState s, bool isLocal) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Position = new Vector2((float)s.X, (float)s.Y),
        Target = new Vector2((float)s.X, (float)s.Y),
        IsLocal = isLocal,
    };
}
