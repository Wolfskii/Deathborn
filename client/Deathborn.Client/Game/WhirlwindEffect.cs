using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Spinning blade arc around a warrior during Whirlwind.</summary>
public sealed class WhirlwindEffect : IWorldEffect
{
    private float _timer;
    private readonly float _duration;

    public WhirlwindEffect(Vector2 position, long ownerId, float duration)
    {
        Position = position;
        OwnerId = ownerId;
        _duration = duration;
        AbilityId = "whirlwind";
    }

    public Vector2 Position { get; set; }
    public long OwnerId { get; }
    public string AbilityId { get; }
    public bool Alive => _timer < _duration;
    public bool DrawUnderEntities => true;
    public bool CanClash => false;
    public float HitRadius => 0f;

    public void CancelByClash() { }

    public void Update(float dt, IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyDictionary<long, WorldNpcEntity> bosses,
        IReadOnlyList<InteractableEntity> interactables, bool reportHits,
        Action<long, int>? onPlayerHit, Action<long, int>? onNpcHit)
    {
        _timer += dt;
        AbilityEffectBehavior.SyncOwnerPosition(this, players);
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var t = _timer / _duration;
        var radius = Config.WhirlwindRadius * zoom * 0.85f;
        var alpha = 1f - t * 0.6f;
        var spins = 2.5f;
        var angle = t * MathHelper.TwoPi * spins;

        for (var i = 0; i < 3; i++)
        {
            var a = angle + i * MathHelper.TwoPi / 3f;
            var tip = screenPos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            var tail = screenPos + new Vector2(MathF.Cos(a + 0.4f), MathF.Sin(a + 0.4f)) * (radius * 0.55f);
            DrawPrimitives.DrawLine(sb, tail, tip, new Color(0.85f, 0.9f, 1f, alpha * 0.7f), 3f * zoom);
        }

        DrawPrimitives.DrawCircleOutline(sb, screenPos, radius, new Color(0.7f, 0.75f, 0.95f, alpha * 0.45f), 24, 1.5f);
    }
}
