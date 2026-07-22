using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public enum ProjectileStyle { Fire, Ice, Arrow, Blood, Poison }

public enum SpellProjectilePhase { Flying, Bursting }

public sealed class SpellProjectile : IWorldEffect
{
    public Vector2 Position { get; set; }
    public Vector2 Direction;
    public long OwnerId { get; private set; }
    public ProjectileDefinition Definition { get; private set; } = ProjectileDefinitions.Fireball;
    public ProjectileStyle Style { get; private set; } = ProjectileStyle.Fire;
    public SpellProjectilePhase Phase = SpellProjectilePhase.Flying;
    public bool Alive { get; private set; } = true;

    public string AbilityId => Definition.Id;
    public bool DrawUnderEntities => false;
    public float HitRadius => Definition.Radius;

    public bool CanClash =>
        Alive && Phase == SpellProjectilePhase.Flying && Definition.ClashWithProjectiles;

    private Vector2 _start;
    private float _traveled;
    private float _ignoreOwnerTimer = 0.12f;
    private float _flyAnim;
    private float _burstTimer;
    public Action? OnImpact;

    public static SpellProjectile Spawn(
        Vector2 origin,
        Vector2 direction,
        long ownerId,
        ProjectileDefinition? definition = null,
        ProjectileStyle style = ProjectileStyle.Fire)
    {
        var def = definition ?? ProjectileDefinitions.Fireball;
        var raw = direction.LengthSquared() > 0.01f ? Vector2.Normalize(direction) : new Vector2(0, 1);
        var dir = PlayerEntity.CardinalFacing(raw);
        return new SpellProjectile
        {
            Definition = def,
            Style = style,
            OwnerId = ownerId,
            Direction = dir,
            _start = origin,
            Position = origin,
        };
    }

    public void Update(
        float dt,
        IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyDictionary<long, WorldNpcEntity> npcs,
        IReadOnlyList<InteractableEntity> interactables,
        bool reportHits,
        Action<long, int>? onPlayerHit,
        Action<long, int>? onNpcHit)
    {
        if (!Alive) return;

        if (Phase == SpellProjectilePhase.Bursting)
        {
            _burstTimer += dt;
            if (_burstTimer >= Definition.BurstDuration)
                Alive = false;
            return;
        }

        _flyAnim += dt * (Style == ProjectileStyle.Ice ? 18f : Style == ProjectileStyle.Poison ? 16f : 14f);
        _ignoreOwnerTimer -= dt;

        var step = Definition.Speed * dt;
        var previous = Position;
        Position += Direction * step;
        _traveled += step;

        if (_traveled >= Definition.MaxRange)
        {
            Position = _start + Direction * Definition.MaxRange;
            StartBurst();
            return;
        }

        if (Position.X < 0 || Position.Y < 0
            || Position.X > WorldMap.SwaroviaMainland.WorldWidth || Position.Y > WorldMap.SwaroviaMainland.WorldHeight)
        {
            StartBurst();
            return;
        }

        if (_ignoreOwnerTimer <= 0)
        {
            foreach (var (id, npc) in npcs)
            {
                if (!npc.IsAttackable) continue;
                if (!NpcHitboxes.ProjectileHits(previous, Position, Definition.Radius, npc)) continue;
                if (reportHits && Definition.Damage > 0)
                    onNpcHit?.Invoke(id, Definition.Damage);
                StartBurst();
                return;
            }

            foreach (var (id, player) in players)
            {
                if (id == OwnerId || player.IsDead) continue;
                var hit = Definition.Radius + PlayerEntity.Radius;
                if (Vector2.DistanceSquared(Position, player.Position) <= hit * hit)
                {
                    if (reportHits && Definition.Damage > 0)
                        onPlayerHit?.Invoke(id, Definition.Damage);
                    StartBurst();
                    return;
                }
            }
        }

        foreach (var obj in interactables)
        {
            var hit = Definition.Radius + obj.PickRadius * 0.85f;
            if (Vector2.DistanceSquared(Position, obj.Position) <= hit * hit)
            {
                StartBurst();
                return;
            }
        }

        if (WorldFoliage.BlocksCircle(Position, Definition.Radius))
        {
            Position -= Direction * (Definition.Radius * 0.5f + 2f);
            StartBurst();
            return;
        }
    }

    public void CancelByClash()
    {
        if (Phase != SpellProjectilePhase.Flying) return;
        StartBurst();
    }

    private void StartBurst()
    {
        Phase = SpellProjectilePhase.Bursting;
        _burstTimer = 0;
        OnImpact?.Invoke();
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (DrawElementalBall(sb, screenPos, zoom))
            return;

        if (ProjectileSprites.IsLoaded && Style is ProjectileStyle.Arrow or ProjectileStyle.Blood)
        {
            DrawArrow(sb, screenPos, zoom);
            return;
        }

        if (Style == ProjectileStyle.Ice)
            DrawIce(sb, screenPos, zoom);
        else if (Style == ProjectileStyle.Poison)
            DrawPoisonFallback(sb, screenPos, zoom);
        else
            DrawFire(sb, screenPos, zoom);
    }

    private bool DrawElementalBall(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (Style is not (ProjectileStyle.Fire or ProjectileStyle.Ice or ProjectileStyle.Poison))
            return false;

        if (Phase == SpellProjectilePhase.Bursting)
            return DrawElementalBurst(sb, screenPos, zoom);

        if (!ProjectileSprites.TryGetBallFlyFrame(Style, _flyAnim, out var tex, out var src))
            return false;

        // Sheets face left; rotate so the head leads the velocity. No flip needed for right —
        // Pi offset handles all directions including rightward.
        var angle = MathF.Atan2(Direction.Y, Direction.X) + ProjectileSprites.BallArtFacingOffset;
        // Fly cells are wide/short (~68×9). Size by thickness vs hit radius, then cap length
        // so they read clearly without becoming screen-filling streaks.
        var targetH = MathF.Max(16f, Definition.Radius * 0.85f * zoom);
        var scale = targetH / MathF.Max(1f, src.Height);
        var maxLen = MathF.Max(56f, Definition.Radius * 3.4f * zoom);
        if (src.Width * scale > maxLen)
            scale = maxLen / MathF.Max(1f, src.Width);
        var origin = ProjectileSprites.BallFlyOrigin(src);
        sb.Draw(tex, screenPos, src, Color.White, angle, origin, scale, SpriteEffects.None, 0f);
        return true;
    }

    private bool DrawElementalBurst(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var t = MathHelper.Clamp(_burstTimer / MathF.Max(0.01f, Definition.BurstDuration), 0f, 1f);
        if (ProjectileSprites.TryGetBallBurstFrame(Style, t, out var tex, out var src))
        {
            var alpha = 1f - t * 0.85f;
            var targetH = MathF.Max(22f, Definition.Radius * (1.6f + t * 1.8f) * zoom);
            var scale = targetH / MathF.Max(1f, src.Height);
            var origin = new Vector2(src.Width * 0.5f, src.Height * 0.55f);
            sb.Draw(tex, screenPos, src, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
            return true;
        }

        // Fall through to procedural burst for this style.
        return false;
    }

    private void DrawArrow(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var tex = ProjectileSprites.ForStyle(Style);
        if (tex == null)
        {
            if (Style == ProjectileStyle.Ice) DrawIce(sb, screenPos, zoom);
            else DrawFire(sb, screenPos, zoom);
            return;
        }

        if (Phase == SpellProjectilePhase.Bursting)
        {
            var t = _burstTimer / Definition.BurstDuration;
            var alpha = 1f - t;
            DrawPrimitives.FillCircle(sb, screenPos, Definition.Radius * zoom * (1f + t * 2f),
                (Style == ProjectileStyle.Blood ? new Color(180, 40, 60) : new Color(200, 220, 240)) * (0.35f * alpha));
            return;
        }

        var angle = MathF.Atan2(Direction.Y, Direction.X);
        var size = Math.Max(8f, Definition.Radius * 2.2f * zoom);
        var origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
        sb.Draw(tex, screenPos, null, Color.White, angle, origin, size / tex.Width, SpriteEffects.None, 0f);
    }

    private void DrawFire(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (ProjectileSprites.HasMagicSheet && DrawMagicSprite(sb, screenPos, zoom, ProjectileStyle.Fire))
            return;

        if (Phase == SpellProjectilePhase.Flying)
        {
            var pulse = 1f + MathF.Sin(_flyAnim) * 0.12f;
            var coreR = Definition.Radius * pulse * zoom;
            var glowR = coreR * 1.7f;

            for (var i = 3; i >= 1; i--)
            {
                var trail = screenPos - Direction * (i * 7f * zoom);
                DrawPrimitives.FillCircle(sb, trail, coreR * 0.55f, new Color(1f, 0.45f, 0.1f, 0.25f / i));
            }

            DrawPrimitives.FillCircle(sb, screenPos, glowR, new Color(1f, 0.55f, 0.12f, 0.45f));
            DrawPrimitives.FillCircle(sb, screenPos, coreR, new Color(1f, 0.85f, 0.25f));
            DrawPrimitives.FillCircle(sb, screenPos, coreR * 0.45f, new Color(1f, 1f, 0.75f));
            return;
        }

        var t = _burstTimer / Definition.BurstDuration;
        var expand = MathHelper.Lerp(Definition.Radius, Definition.Radius * 3.8f, t) * zoom;
        var alpha = 1f - t;

        DrawPrimitives.FillCircle(sb, screenPos, expand * 1.4f, new Color(1f, 0.35f, 0.05f, alpha * 0.35f));
        DrawPrimitives.DrawCircleOutline(sb, screenPos, expand, new Color(1f, 0.7f, 0.15f, alpha * 0.85f), 24, 3f * zoom);
        DrawPrimitives.FillCircle(sb, screenPos, expand * 0.55f, new Color(1f, 0.9f, 0.4f, alpha * 0.6f));

        if (t > 0.35f)
        {
            var sparkT = (t - 0.35f) / 0.65f;
            for (var i = 0; i < 6; i++)
            {
                var angle = i / 6f * MathHelper.TwoPi + _burstTimer * 4f;
                var dist = expand * (0.8f + sparkT * 1.2f);
                var spark = screenPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
                DrawPrimitives.FillCircle(sb, spark, 3f * zoom * (1f - sparkT), new Color(1f, 0.6f, 0.1f, (1f - sparkT) * 0.7f));
            }
        }
    }

    private void DrawIce(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (ProjectileSprites.HasMagicSheet && DrawMagicSprite(sb, screenPos, zoom, ProjectileStyle.Ice))
            return;

        if (Phase == SpellProjectilePhase.Flying)
        {
            var spin = _flyAnim * 1.4f;
            var size = Definition.Radius * zoom * (1f + MathF.Sin(_flyAnim * 2f) * 0.08f);

            for (var i = 4; i >= 1; i--)
            {
                var trail = screenPos - Direction * (i * 6f * zoom);
                DrawIceCrystal(sb, trail, size * 0.7f, spin - i * 0.2f, new Color(0.55f, 0.85f, 1f, 0.18f / i));
            }

            DrawPrimitives.FillCircle(sb, screenPos, size * 1.5f, new Color(0.4f, 0.75f, 1f, 0.28f));
            DrawIceCrystal(sb, screenPos, size, spin, new Color(0.75f, 0.95f, 1f));
            DrawIceCrystal(sb, screenPos, size * 0.55f, -spin * 1.3f, new Color(1f, 1f, 1f, 0.9f));
            return;
        }

        var t = _burstTimer / Definition.BurstDuration;
        var alpha = 1f - t;
        var shards = 8;
        for (var i = 0; i < shards; i++)
        {
            var angle = i / (float)shards * MathHelper.TwoPi + _burstTimer * 3f;
            var dist = Definition.Radius * zoom * (1f + t * 3.5f);
            var shardPos = screenPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
            DrawIceCrystal(sb, shardPos, 5f * zoom * (1f - t * 0.6f), angle, new Color(0.7f, 0.92f, 1f, alpha * 0.85f));
        }
        DrawPrimitives.FillCircle(sb, screenPos, Definition.Radius * zoom * (1f + t), new Color(0.85f, 0.95f, 1f, alpha * 0.35f));
    }

    private void DrawPoisonFallback(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (ProjectileSprites.HasMagicSheet && DrawMagicSprite(sb, screenPos, zoom, ProjectileStyle.Poison))
            return;

        if (Phase == SpellProjectilePhase.Flying)
        {
            var pulse = 1f + MathF.Sin(_flyAnim) * 0.1f;
            var coreR = Definition.Radius * pulse * zoom;
            for (var i = 3; i >= 1; i--)
            {
                var trail = screenPos - Direction * (i * 6f * zoom);
                DrawPrimitives.FillCircle(sb, trail, coreR * 0.5f, new Color(0.35f, 0.85f, 0.2f, 0.22f / i));
            }
            DrawPrimitives.FillCircle(sb, screenPos, coreR * 1.6f, new Color(0.25f, 0.7f, 0.15f, 0.4f));
            DrawPrimitives.FillCircle(sb, screenPos, coreR, new Color(0.55f, 0.95f, 0.3f));
            DrawPrimitives.FillCircle(sb, screenPos, coreR * 0.4f, new Color(0.9f, 1f, 0.75f));
            return;
        }

        var t = _burstTimer / Definition.BurstDuration;
        var expand = MathHelper.Lerp(Definition.Radius, Definition.Radius * 3.2f, t) * zoom;
        var alpha = 1f - t;
        DrawPrimitives.FillCircle(sb, screenPos, expand * 1.3f, new Color(0.25f, 0.7f, 0.15f, alpha * 0.35f));
        DrawPrimitives.FillCircle(sb, screenPos, expand * 0.5f, new Color(0.6f, 0.95f, 0.35f, alpha * 0.55f));
    }

    private static void DrawIceCrystal(SpriteBatch sb, Vector2 center, float size, float rotation, Color color)
    {
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var a = rotation + i / 6f * MathHelper.TwoPi;
            var r = i % 2 == 0 ? size : size * 0.45f;
            points[i] = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
        }

        for (var i = 0; i < 6; i++)
            DrawPrimitives.DrawLine(sb, points[i], points[(i + 1) % 6], color, MathF.Max(1.5f, size * 0.12f));
        DrawPrimitives.FillCircle(sb, center, size * 0.22f, color);
    }

    private bool DrawMagicSprite(SpriteBatch sb, Vector2 screenPos, float zoom, ProjectileStyle style)
    {
        var tex = ProjectileSprites.MagicSheet;
        if (tex == null || Phase != SpellProjectilePhase.Flying)
            return false;

        if (!ProjectileSprites.TryGetMagicFlyFrame(style, (int)_flyAnim, out var flySrc))
            return false;

        var angle = MathF.Atan2(Direction.Y, Direction.X);
        var size = MathF.Max(14f, Definition.Radius * 2f * zoom);
        var origin = new Vector2(flySrc.Width * 0.5f, flySrc.Height * 0.5f);
        sb.Draw(
            tex,
            screenPos,
            flySrc,
            ProjectileSprites.TintForStyle(style),
            angle,
            origin,
            size / flySrc.Width,
            SpriteEffects.None,
            0f);
        return true;
    }
}
