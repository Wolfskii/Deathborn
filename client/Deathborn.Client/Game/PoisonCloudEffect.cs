using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Lingering poison cloud that damages players standing inside.</summary>
public sealed class PoisonCloudEffect : IWorldEffect
{
    public Vector2 Position { get; set; }
    public long OwnerId { get; }
    public string AbilityId => "poison_cloud";
    public bool DrawUnderEntities => true;
    public bool CanClash => false;
    public float HitRadius => Radius;

    public bool Alive { get; private set; } = true;

    public const float Radius = 46f;
    public const float Duration = 3.6f;
    public const float TickInterval = 0.6f;

    private float _timer;
    private float _tickAccum;

    public PoisonCloudEffect(Vector2 position, long ownerId)
    {
        Position = position;
        OwnerId = ownerId;
    }

    public void Update(
        float dt,
        IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyList<InteractableEntity> interactables,
        bool reportHits,
        Action<long, int>? onPlayerHit)
    {
        _timer += dt;
        if (_timer >= Duration)
        {
            Alive = false;
            return;
        }

        _tickAccum += dt;
        if (_tickAccum < TickInterval)
            return;

        _tickAccum -= TickInterval;

        if (!reportHits) return;

        foreach (var (id, player) in players)
        {
            if (id == OwnerId) continue;
            if (Vector2.DistanceSquared(Position, player.Position) > (Radius + PlayerEntity.Radius) * (Radius + PlayerEntity.Radius))
                continue;
            onPlayerHit?.Invoke(id, Config.PoisonCloudDamage);
        }
    }

    public void CancelByClash() => Alive = false;

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var life = 1f - _timer / Duration;
        var pulse = 1f + MathF.Sin(_timer * 5f) * 0.08f;
        var baseR = Radius * pulse * zoom;

        DrawPrimitives.FillCircle(sb, screenPos, baseR * 1.15f, new Color(0.15f, 0.35f, 0.08f, 0.22f * life));
        DrawPrimitives.DrawCircleOutline(sb, screenPos, baseR, new Color(0.35f, 0.75f, 0.2f, 0.45f * life), 28, 2f * zoom);

        for (var i = 0; i < 7; i++)
        {
            var angle = i / 7f * MathHelper.TwoPi + _timer * 1.8f;
            var wobble = MathF.Sin(_timer * 3f + i * 1.7f) * 6f * zoom;
            var blobR = baseR * (0.35f + i % 3 * 0.12f);
            var blob = screenPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (baseR * 0.55f + wobble);
            DrawPrimitives.FillCircle(sb, blob, blobR, new Color(0.25f, 0.7f, 0.15f, 0.35f * life));
        }

        for (var i = 0; i < 5; i++)
        {
            var rise = (_timer * 0.35f + i * 0.17f) % 1f;
            var bubble = screenPos + new Vector2(
                MathF.Sin(_timer * 2f + i) * baseR * 0.4f,
                -rise * baseR * 0.9f);
            DrawPrimitives.FillCircle(sb, bubble, 3f * zoom * (1f - rise), new Color(0.5f, 1f, 0.35f, (1f - rise) * 0.5f * life));
        }

        DrawPrimitives.FillCircle(sb, screenPos, baseR * 0.35f, new Color(0.45f, 0.9f, 0.25f, 0.25f * life));
    }
}
