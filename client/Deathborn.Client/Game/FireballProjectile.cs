using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public enum FireballPhase { Flying, Bursting }

public sealed class FireballProjectile
{
    public Vector2 Position;
    public Vector2 Direction;
    public long OwnerId;
    public ProjectileDefinition Definition { get; private set; } = ProjectileDefinitions.Fireball;
    public FireballPhase Phase = FireballPhase.Flying;
    public bool Alive = true;

    private Vector2 _start;
    private float _traveled;
    private float _ignoreOwnerTimer = 0.12f;
    private float _flyAnim;
    private float _burstTimer;

    public float HitRadius => Definition.Radius;

    public bool CanClash =>
        Alive && Phase == FireballPhase.Flying && Definition.ClashWithProjectiles;

    public static FireballProjectile Spawn(
        Vector2 origin,
        Vector2 direction,
        long ownerId,
        ProjectileDefinition? definition = null)
    {
        var def = definition ?? ProjectileDefinitions.Fireball;
        var dir = direction.LengthSquared() > 0.01f ? Vector2.Normalize(direction) : new Vector2(0, 1);
        return new FireballProjectile
        {
            Definition = def,
            OwnerId = ownerId,
            Direction = dir,
            _start = origin,
            Position = origin,
        };
    }

    public void Update(
        float dt,
        IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyList<InteractableEntity> interactables,
        bool reportHits,
        Action<long, int>? onPlayerHit)
    {
        if (!Alive) return;

        if (Phase == FireballPhase.Bursting)
        {
            _burstTimer += dt;
            if (_burstTimer >= Definition.BurstDuration)
                Alive = false;
            return;
        }

        _flyAnim += dt * 14f;
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
            foreach (var (id, player) in players)
            {
                if (id == OwnerId) continue;
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
        if (Phase != FireballPhase.Flying) return;
        StartBurst();
    }

    private void StartBurst()
    {
        Phase = FireballPhase.Bursting;
        _burstTimer = 0;
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        if (Phase == FireballPhase.Flying)
        {
            var pulse = 1f + MathF.Sin(_flyAnim) * 0.12f;
            var coreR = Definition.Radius * pulse * zoom;
            var glowR = coreR * 1.7f;

            for (var i = 3; i >= 1; i--)
            {
                var trail = screenPos - Direction * (i * 7f * zoom);
                var a = 0.25f / i;
                DrawPrimitives.FillCircle(sb, trail, coreR * 0.55f, new Color(1f, 0.45f, 0.1f, a));
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
}
