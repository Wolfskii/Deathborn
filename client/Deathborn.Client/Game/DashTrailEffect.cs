using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Afterimage trail during a warrior charge.</summary>
public sealed class DashTrailEffect : IWorldEffect
{
    private float _timer;
    private readonly float _duration;
    private readonly Vector2 _dir;

    public DashTrailEffect(Vector2 position, Vector2 dir, long ownerId, float duration)
    {
        Position = position;
        _dir = dir.LengthSquared() > 0.01f ? Vector2.Normalize(dir) : new Vector2(0, 1);
        OwnerId = ownerId;
        _duration = duration;
        AbilityId = "warrior_dash";
    }

    public Vector2 Position { get; private set; }
    public long OwnerId { get; }
    public string AbilityId { get; }
    public bool Alive => _timer < _duration;
    public bool DrawUnderEntities => true;
    public bool CanClash => false;
    public float HitRadius => 0f;

    public void CancelByClash() { }

    public void Update(float dt, IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyList<InteractableEntity> interactables, bool reportHits, Action<long, int>? onPlayerHit)
    {
        _timer += dt;
        if (players.TryGetValue(OwnerId, out var owner))
            Position = owner.Position;
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var t = _timer / _duration;
        var alpha = (1f - t) * 0.55f;
        for (var i = 1; i <= 4; i++)
        {
            var back = screenPos - _dir * (i * 14f * zoom);
            var size = (12f - i * 2f) * zoom;
            DrawPrimitives.FillCircle(sb, back + new Vector2(0, -8 * zoom), size, new Color(0.6f, 0.65f, 0.9f, alpha));
        }
    }
}
