using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Deathborn.Client.Rendering.Characters;

namespace Deathborn.Client.Gameplay;

public sealed class PlayerEntity
{
    public const float Radius = 12f;
    /// <summary>Extra vertical reach above the foot-anchored bottom (ellipse grows upward only).</summary>
    private const float CollisionVerticalExtraPx = 4f;
    public static float CollisionRadiusX => Radius;
    public static float CollisionRadiusY => Radius + CollisionVerticalExtraPx;
    public const float SpriteDrawScale = 1.5f;
    /// <summary>Feet anchor → collision ellipse center (bottom sits on foot pixels).</summary>
    private const float CollisionCenterFineTuneDownPx = 2f;
    public static float CollisionCenterYOffset =>
        -Rendering.Characters.FarmRpgAnimationSpecs.FootBottomInsetPx *
        CharacterAnimationCatalog.GetDrawScale(CharacterAnimationCatalog.FarmRpg) - CollisionRadiusY +
        CollisionCenterFineTuneDownPx;

    public static bool EllipseContainsPoint(Vector2 center, float rx, float ry, Vector2 point)
    {
        var dx = (point.X - center.X) / rx;
        var dy = (point.Y - center.Y) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    public static bool EllipseOverlapsCircle(Vector2 center, float rx, float ry, Vector2 otherCenter, float otherRadius)
    {
        if (EllipseContainsPoint(center, rx, ry, otherCenter))
            return true;

        var dx = otherCenter.X - center.X;
        var dy = otherCenter.Y - center.Y;
        var distNorm = MathF.Sqrt((dx / rx) * (dx / rx) + (dy / ry) * (dy / ry));
        if (distNorm < 0.0001f)
            return otherRadius >= MathF.Min(rx, ry);

        var closest = center + new Vector2(dx / distNorm * rx, dy / distNorm * ry);
        var cdx = otherCenter.X - closest.X;
        var cdy = otherCenter.Y - closest.Y;
        return cdx * cdx + cdy * cdy <= otherRadius * otherRadius;
    }

    public static bool EllipseOverlapsRect(
        Vector2 center, float rx, float ry, float left, float right, float top, float bottom)
    {
        var closestX = Math.Clamp(center.X, left, right);
        var closestY = Math.Clamp(center.Y, top, bottom);
        if (EllipseContainsPoint(center, rx, ry, new Vector2(closestX, closestY)))
            return true;

        Span<(float X, float Y)> corners =
        [
            (left, top), (right, top), (left, bottom), (right, bottom),
        ];
        foreach (var (x, y) in corners)
        {
            if (EllipseContainsPoint(center, rx, ry, new Vector2(x, y)))
                return true;
        }
        return false;
    }

    public static Vector2 PushEllipseOutOfRect(
        Vector2 center, float rx, float ry, float left, float right, float top, float bottom)
    {
        var closestX = Math.Clamp(center.X, left, right);
        var closestY = Math.Clamp(center.Y, top, bottom);
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        var distSq = dx * dx + dy * dy;
        if (!EllipseContainsPoint(center, rx, ry, new Vector2(closestX, closestY)) && distSq >= 0.0001f)
            return center;

        if (distSq < 0.0001f)
        {
            dx = 0f;
            dy = 1f;
            distSq = 1f;
        }

        var dist = MathF.Sqrt(distSq);
        var nx = dx / dist;
        var ny = dy / dist;
        var effR = 1f / MathF.Sqrt((nx / rx) * (nx / rx) + (ny / ry) * (ny / ry));
        var push = (effR - dist + 0.35f);
        return center + new Vector2(nx * push, ny * push);
    }
    private const float MinMoveDisplacementSq = 0.36f;

    public static float SpriteWorldHalfWidth =>
        Rendering.Characters.FarmRpgAnimationSpecs.FrameWidth * 0.5f *
        CharacterAnimationCatalog.GetDrawScale(CharacterAnimationCatalog.FarmRpg);

    public static float SpriteWorldHeight =>
        Rendering.Characters.FarmRpgAnimationSpecs.FrameHeight *
        CharacterAnimationCatalog.GetDrawScale(CharacterAnimationCatalog.FarmRpg);
    private float VisualDrawScale => CharacterAnimationCatalog.GetDrawScale(_visual.Appearance.BodyTypeId);

    /// <summary>World Y of lowest opaque body pixel (feet), excluding shadow padding below the draw anchor.</summary>
    public float SortY => Position.Y - Rendering.Characters.FarmRpgAnimationSpecs.FootBottomInsetPx * VisualDrawScale;

    public static Vector2 CollisionCenter(Vector2 feetPosition) =>
        feetPosition + new Vector2(0, CollisionCenterYOffset);

    /// <summary>Southern edge of the collision ellipse (used for southward shore checks).</summary>
    public static float CollisionBottomY(float feetY) =>
        CollisionCenter(new Vector2(0, feetY)).Y + CollisionRadiusY;

    public static Vector2 CollisionCenterToFeet(Vector2 center) =>
        center - new Vector2(0, CollisionCenterYOffset);

    private readonly CharacterVisual _visual = new();
    private readonly PlayerChatBubble _chatBubble = new();
    private readonly ThinkingBubble _thinking = new();
    private readonly HashSet<long> _meleeHitThisSwing = [];
    private readonly HashSet<long> _whirlwindHit = [];
    private readonly HashSet<long> _dashHit = [];
    private MeleeAbilityDefinition? _activeMeleeDef;
    private float _abilityLockTimer;
    private float _bandageHoTTimer;
    private float _bandageAnim;
    private float _lastFrameDisplacementSq;
    private float _whirlwindTimer;
    private bool _whirlwindHitPulse;
    private bool _isDashing;
    private float _dashTimer;
    private float _dashDuration;
    private Vector2 _dashStart;
    private Vector2 _dashEnd;
    private float _postDashSettle;
    private string? _channeledAbilityId;

    public long Id;
    public string Name = "";
    public Vector2 Position;
    public Vector2 Target;
    public bool IsLocal;
    public long InsideHouseId;
    /// <summary>House plot center while inside — used for interior movement when zone list is stale.</summary>
    public Vector2? InteriorCenter;
    public string? HeadCosmetic;
    public Vector2 MoveDir;
    public Vector2 InputDir;
    /// <summary>Mouse-facing while idle (local player only).</summary>
    public Vector2 AimDir;
    /// <summary>Local sprint held (Shift) with enough stamina.</summary>
    public bool IsRunning;

    public bool IsAttacking => _visual.Controller.IsAttackPlaying;
    public bool IsCasting => _abilityLockTimer > 0f;
    public bool IsDashing => _isDashing;
    public bool IsPostDashSettling => _postDashSettle > 0f;
    public bool IsWhirlwinding => _whirlwindTimer > 0f;
    public bool IsBusy => IsAttacking || IsCasting || IsDashing || IsWhirlwinding;

    /// <summary>Blocks WASD movement — separate from IsBusy so channeled abilities can allow walking.</summary>
    public bool BlocksMovement
    {
        get
        {
            if (IsAttacking || IsCasting || IsDashing) return true;
            if (_channeledAbilityId != null &&
                AbilityCatalog.Get(_channeledAbilityId) is { CanMoveWhileUsing: true })
                return false;
            return _channeledAbilityId != null;
        }
    }
    public bool IsHurt => _visual.Controller.IsHurtPlaying;
    public bool IsTyping
    {
        get => _thinking.Active;
        set => _thinking.Active = value;
    }

    public bool IsBandaging => _bandageHoTTimer > 0f;
    public bool IsDying => _visual.Controller.IsDeathPlaying;
    public bool IsCorpse => _visual.Controller.IsDeathComplete;
    public bool IsDead => IsDying || IsCorpse;

    public CharacterStats Stats { get; } = CharacterStats.CreateStarter();

    public void BeginDeath(Vector2? facing = null)
    {
        if (IsDead) return;

        var dir = facing ?? FacingDir;
        if (dir.LengthSquared() > 0.01f)
            MoveDir = Vector2.Normalize(dir);
        _visual.BeginDeath(dir);
    }

    public void StartBandageHoT() => _bandageHoTTimer = Config.BandageDuration;

    public static Vector2 CardinalFacing(Vector2 dir)
    {
        if (dir.LengthSquared() < 0.01f) return new Vector2(0, 1);
        return FacingResolver.Resolve(dir) switch
        {
            FacingDirection.Right => Vector2.UnitX,
            FacingDirection.Left => -Vector2.UnitX,
            FacingDirection.Up => new Vector2(0, -1),
            _ => new Vector2(0, 1),
        };
    }

    public Vector2 GetProjectileSpawnPoint(Vector2? direction = null)
    {
        var dir = direction ?? FacingDir;
        if (dir.LengthSquared() > 0.01f)
            dir = Vector2.Normalize(dir);
        else
            dir = new Vector2(0, 1);
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
        _visual.ApplyHit(FacingDir);
    }

    public void CheckLocalMeleeHits(
        IReadOnlyDictionary<long, PlayerEntity> players,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || IsDead || !IsAttacking) return;

        var def = _activeMeleeDef ?? MeleeAbilityDefinitions.Slash;
        var frame = _visual.Controller.AttackFrame;
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
        var clip = def.Id == "shield_bash" ? CharacterClip.ShieldBash : CharacterClip.Attack;
        return StartAttack(facing, clip);
    }

    public bool StartAttack(Vector2? facing = null, CharacterClip clip = CharacterClip.Attack)
    {
        if (IsBusy || IsDead) return false;
        _meleeHitThisSwing.Clear();
        var card = CardinalFacing(facing ?? FacingDir);
        MoveDir = card;
        _visual.StartAttack(card, clip);
        return true;
    }

    public bool StartWhirlwind()
    {
        if (IsBusy || IsDead) return false;
        _channeledAbilityId = "whirlwind";
        _whirlwindTimer = Config.WhirlwindDuration;
        _whirlwindHit.Clear();
        _whirlwindHitPulse = false;
        if (AbilityCatalog.Get("whirlwind") is not { CanMoveWhileUsing: true })
            StartAbilityLock(Config.WhirlwindDuration);
        return true;
    }

    public bool StartDash(Vector2 dir, float distance, float duration)
    {
        if (IsBusy || IsDead || _isDashing) return false;
        if (dir.LengthSquared() > 0.01f)
            dir = Vector2.Normalize(dir);
        else
            dir = new Vector2(0, 1);
        MoveDir = CardinalFacing(dir);
        _dashStart = Position;
        _dashEnd = ResolvePosition(Position, dir * distance);
        _dashDuration = duration;
        _dashTimer = duration;
        _dashHit.Clear();
        _isDashing = true;
        _postDashSettle = 0f;
        Target = _dashEnd;
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
        IReadOnlyDictionary<long, WorldNpcEntity> npcs,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || IsDead || !IsAttacking || npcs.Count == 0) return;

        var def = _activeMeleeDef ?? MeleeAbilityDefinitions.Slash;
        var frame = _visual.Controller.AttackFrame;
        if (frame < def.HitFrameStart || frame > def.HitFrameEnd) return;

        var facing = CardinalFacing(FacingDir);
        foreach (var (id, npc) in npcs)
        {
            if (!npc.IsAttackable) continue;
            if (_meleeHitThisSwing.Contains(id)) continue;
            NpcHitboxes.GetWorldAabb(npc, NpcHitboxes.HitTestAnchor(npc), out var center, out var halfW, out _);
            if (!IsInMeleeArc(Position, facing, center, def.Range, def.HalfWidth + halfW)) continue;
            _meleeHitThisSwing.Add(id);
            reportHit(id, def.Damage, def.Id);
        }
    }

    public void CheckWhirlwindBossHits(
        IReadOnlyDictionary<long, WorldNpcEntity> npcs,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !IsWhirlwinding || !_whirlwindHitPulse || npcs.Count == 0) return;
        foreach (var (id, npc) in npcs)
        {
            if (!npc.IsAttackable) continue;
            var radius = Config.WhirlwindRadius;
            if (_whirlwindHit.Contains(id)) continue;
            if (!NpcHitboxes.CircleOverlaps(Position, radius, npc)) continue;
            _whirlwindHit.Add(id);
            reportHit(id, Config.WhirlwindDamage, "whirlwind");
        }
    }

    public void CheckDashBossHits(
        IReadOnlyDictionary<long, WorldNpcEntity> npcs,
        Action<long, int, string> reportHit)
    {
        if (!IsLocal || !_isDashing || npcs.Count == 0) return;
        var t = 1f - MathF.Max(0f, _dashTimer) / MathF.Max(0.001f, _dashDuration);
        if (t < 0.35f || t > 0.85f) return;

        foreach (var (id, npc) in npcs)
        {
            if (!npc.IsAttackable) continue;
            if (_dashHit.Contains(id)) continue;
            if (!NpcHitboxes.CircleOverlaps(Position, PlayerEntity.Radius + 18f, npc)) continue;
            _dashHit.Add(id);
            reportHit(id, Config.WarriorDashDamage, "warrior_dash");
        }
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
                    MoveDir = CardinalFacing(facingDir);
                break;
            case PlayerActions.UseBandage:
                StartAbilityLock(0.55f);
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
                if (facingDir.LengthSquared() > 0.01f)
                    MoveDir = CardinalFacing(facingDir);
                break;
            case PlayerActions.HunterMark:
                StartAbilityLock(0.3f);
                if (facingDir.LengthSquared() > 0.01f)
                    MoveDir = CardinalFacing(facingDir);
                break;
            case PlayerActions.Interact:
                // Interaction animations can hook in here when added.
                break;
        }
    }

    public bool IsMoving =>
        !IsDead &&
        !IsHurt &&
        !BlocksMovement &&
        (IsLocal
            ? InputDir.LengthSquared() > 0.01f
            : Vector2.DistanceSquared(Position, Target) > 0.5f);

    public bool IsWalking => IsMoving && !IsRunning;

    public Vector2 FacingDir
    {
        get
        {
            if (IsLocal && InputDir.LengthSquared() > 0.01f) return CardinalFacing(InputDir);
            if (IsLocal && AimDir.LengthSquared() > 0.01f) return CardinalFacing(AimDir);
            if (MoveDir.LengthSquared() > 0.01f) return CardinalFacing(MoveDir);
            return new Vector2(0, 1);
        }
    }

    /// <summary>Walk/run facing follows movement keys — not mouse aim.</summary>
    public Vector2 LocomotionDir
    {
        get
        {
            if (IsLocal && InputDir.LengthSquared() > 0.01f) return CardinalFacing(InputDir);
            if (MoveDir.LengthSquared() > 0.01f) return CardinalFacing(MoveDir);
            return FacingDir;
        }
    }

    private Vector2 ResolvePosition(Vector2 feet, Vector2 delta)
    {
        if (InsideHouseId > 0)
        {
            var center = InteriorCenter ?? WorldZones.HouseById(InsideHouseId)?.Center;
            if (center is Vector2 c)
                return HousingConstants.ResolveInteriorMove(feet, delta, c);
        }

        return WorldMap.SwaroviaMainland.ResolveMove(feet, delta, Radius);
    }

    public void Update(float dt)
    {
        if (_abilityLockTimer > 0f)
            _abilityLockTimer = MathF.Max(0f, _abilityLockTimer - dt);

        if (_whirlwindTimer > 0f)
            _whirlwindTimer = MathF.Max(0f, _whirlwindTimer - dt);
        else if (_channeledAbilityId == "whirlwind")
            _channeledAbilityId = null;

        if (IsDead)
        {
            _visual.UpdateAnimation(dt, BuildAnimationInput());
            return;
        }

        var prevPos = Position;

        if (_postDashSettle > 0f)
        {
            _postDashSettle = MathF.Max(0f, _postDashSettle - dt);
            Position = Target;
        }

        if (_isDashing)
        {
            _dashTimer = MathF.Max(0f, _dashTimer - dt);
            var t = 1f - _dashTimer / MathF.Max(0.001f, _dashDuration);
            var next = Vector2.Lerp(_dashStart, _dashEnd, t);
            var step = next - Position;
            Position = ResolvePosition(Position, step);
            Target = _dashEnd;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
                Position = _dashEnd;
                Target = _dashEnd;
                _postDashSettle = 0.15f;
            }
        }
        else if (IsLocal && InputDir.LengthSquared() > 0.01f && !BlocksMovement)
        {
            var dir = Vector2.Normalize(InputDir);
            var speed = IsRunning ? Config.RunSpeed : Config.WalkSpeed;
            var requested = dir * speed * dt;
            var predicted = ResolvePosition(Position, requested);
            var movedSq = Vector2.DistanceSquared(predicted, Position);
            var requestedSq = requested.LengthSquared();
            var movementClipped = requestedSq > 0.01f && movedSq < requestedSq * 0.64f;

            if (movedSq < MinMoveDisplacementSq || movementClipped)
            {
                // Blocked or foliage/terrain clipped the step — keep local contact, no reconcile pull-back.
                Position = predicted;
            }
            else
            {
                var err = Target - predicted;
                var errLenSq = err.LengthSquared();
                if (errLenSq > Config.LocalSnapDistance * Config.LocalSnapDistance)
                    Position = Target;
                else if (errLenSq > 2f)
                {
                    predicted += err * MathHelper.Clamp(dt * Config.LocalReconcileSpeed, 0f, 0.35f);
                    Position = predicted;
                }
                else
                    Position = predicted;
            }
        }
        else
        {
            if (IsLocal && _postDashSettle > 0f)
            {
                Position = Target;
            }
            else
            {
                var lerpSpeed = IsLocal ? Config.LocalReconcileSpeed : Config.PlayerLerpSpeed;
                var lerped = Vector2.Lerp(Position, Target, MathHelper.Clamp(dt * lerpSpeed, 0, 1));
                // Do not re-run foliage push-out on idle reconcile — it caused bounce-back at tree contact.
                Position = lerped;
            }
        }

        _lastFrameDisplacementSq = Vector2.DistanceSquared(prevPos, Position);

        UpdateBandageVisual(dt);

        var wasAttacking = IsAttacking;
        _visual.UpdateAnimation(dt, BuildAnimationInput());
        if (wasAttacking && !IsAttacking)
            _activeMeleeDef = null;

        _chatBubble.Update(dt);
        _thinking.Update(dt);
    }

    private AnimationInput BuildAnimationInput() => new()
    {
        IsDead = IsDead,
        IsWhirlwinding = IsWhirlwinding,
        IsDashing = _isDashing,
        IsCasting = IsCasting,
        IsMoving = IsMoving,
        IsRunning = IsRunning,
        LocomotionDir = LocomotionDir,
        FacingDir = FacingDir,
    };

    private void UpdateBandageVisual(float dt)
    {
        if (_bandageHoTTimer <= 0f) return;
        _bandageHoTTimer -= dt;
        _bandageAnim += dt;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        var tint = IsLocal ? Color.White : new Color(0.92f, 0.82f, 0.78f);
        var scale = VisualDrawScale * zoom;

        if (IsDead)
        {
            _visual.Draw(sb, screenPos, tint, scale);
            if (IsCorpse)
            {
                var markerY = screenPos.Y + (-Radius - 28f) * zoom;
                CorpseMarkerDraw.DrawCorpseMarker(sb, new Vector2(screenPos.X, markerY), zoom);
            }
            return;
        }

        _visual.Draw(sb, screenPos, tint, scale);

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

        var scale = VisualDrawScale * zoom;
        var outerThick = Math.Max(2.5f, 3.1f * zoom);
        var innerThick = Math.Max(2f, 2.5f * zoom);

        DrawHoverOutlinePass(sb, screenPos, new Color(255, 255, 255, 0.45f), scale, outerThick);
        DrawHoverOutlinePass(sb, screenPos, new Color(255, 252, 185, 1f), scale, innerThick);
        DrawHoverOutlinePass(sb, screenPos, new Color(255, 238, 120, 0.92f), scale, innerThick * 0.72f);
    }

    private void DrawHoverOutlinePass(
        SpriteBatch sb, Vector2 screenPos, Color outline, float scale, float thickness)
    {
        _visual.DrawOutline(sb, screenPos, outline, scale, thickness);
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
            InteriorCenter = s.InsideHouseId > 0
                ? WorldZones.HouseById(s.InsideHouseId)?.Center
                : null,
            HeadCosmetic = string.IsNullOrEmpty(s.HeadCosmetic) ? null : s.HeadCosmetic,
        };
        if (s.HpMax > 0)
            entity.SyncStats((float)s.Hp, (float)s.HpMax);
        return entity;
    }
}
