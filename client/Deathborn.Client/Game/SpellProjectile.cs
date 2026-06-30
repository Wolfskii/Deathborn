using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public enum ProjectileStyle { Fire, Ice }

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

    public static SpellProjectile Spawn(
        Vector2 origin,
        Vector2 direction,
        long ownerId,
        ProjectileDefinition? definition = null,
        ProjectileStyle style = ProjectileStyle.Fire)
    {
        var def = definition ?? ProjectileDefinitions.Fireball;
        var dir = direction.LengthSquared() > 0.01f ? Vector2.Normalize(direction) : new Vector2(0, 1);
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
        IReadOnlyDictionary<long, BossEntity> bosses,
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

        _flyAnim += dt * (Style == ProjectileStyle.Ice ? 18f : 14f);
        _ignoreOwnerTimer -= dt;

        var step = Definition.Speed * dt;
        Position += Direction * step;
        _traveled += step;

        if (_traveled >= Definition.MaxRange)
        {
            Position = _start + Direction * Definition.MaxRange;
            StartBurst();
            return;
        }

        if (Position.X < 0 || Position.Y < 0
            || Position.X > WorldMap.Realik.WorldWidth || Position.Y > WorldMap.Realik.WorldHeight)
        {
            StartBurst();
            return;
        }

        if (_ignoreOwnerTimer <= 0)
        {
            foreach (var (id, boss) in bosses)
            {
                var hit = Definition.Radius + boss.Radius;
                if (Vector2.DistanceSquared(Position, boss.Position) <= hit * hit)
                {
                    if (reportHits && Definition.Damage > 0)
                        onNpcHit?.Invoke(id, Definition.Damage);
                    StartBurst();
                    return;
                }
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
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (Style == ProjectileStyle.Ice)
            DrawIce(sb, screenPos, zoom);
        else
            DrawFire(sb, screenPos, zoom);
    }

    private void DrawFire(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
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
}
