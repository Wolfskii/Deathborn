using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Instant lightning strike to a nearby target in aim direction.</summary>
public sealed class ArcBoltEffect : IWorldEffect
{
    public Vector2 CasterPos;
    public Vector2 TargetPos;
    public long OwnerId { get; }
    public string AbilityId { get; }
    public bool DrawUnderEntities => false;
    public bool CanClash => false;
    public float HitRadius => 0;

    public Vector2 Position => TargetPos;
    public bool Alive { get; private set; } = true;

    private float _timer;
    private readonly List<Vector2> _boltPoints = [];

    public ArcBoltEffect(Vector2 casterPos, Vector2 targetPos, long ownerId, string abilityId = "arc_bolt")
    {
        CasterPos = casterPos;
        TargetPos = targetPos;
        OwnerId = ownerId;
        AbilityId = abilityId;
        BuildBoltPath();
    }

    private void BuildBoltPath()
    {
        _boltPoints.Clear();
        _boltPoints.Add(CasterPos);
        var segments = 5;
        var rng = new Random((int)(CasterPos.X * 17 + TargetPos.Y * 31));
        for (var i = 1; i < segments; i++)
        {
            var t = i / (float)segments;
            var p = Vector2.Lerp(CasterPos, TargetPos, t);
            var perp = new Vector2(-(TargetPos.Y - CasterPos.Y), TargetPos.X - CasterPos.X);
            if (perp.LengthSquared() > 0.01f)
                perp = Vector2.Normalize(perp);
            p += perp * (rng.NextSingle() * 2f - 1f) * 18f * (1f - MathF.Abs(t - 0.5f) * 2f);
            _boltPoints.Add(p);
        }
        _boltPoints.Add(TargetPos);
    }

    public void Update(float dt, IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyList<InteractableEntity> interactables, bool reportHits, Action<long, int>? onPlayerHit)
    {
        _timer += dt;
        if (_timer >= 0.35f)
            Alive = false;
    }

    public void CancelByClash() => Alive = false;

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var t = MathHelper.Clamp(_timer / 0.35f, 0f, 1f);
        var alpha = 1f - t;
        var casterScreen = screenPos - (TargetPos - CasterPos) * zoom;

        for (var i = 0; i < _boltPoints.Count - 1; i++)
        {
            var a = _boltPoints[i];
            var b = _boltPoints[i + 1];
            var segT = MathHelper.Clamp((_timer * 3f) - i * 0.15f, 0f, 1f);
            if (segT <= 0f) continue;

            var from = casterScreen + (a - CasterPos) * zoom;
            var to = casterScreen + (b - CasterPos) * zoom;
            var end = Vector2.Lerp(from, to, segT);

            DrawPrimitives.DrawLine(sb, from, end, new Color(0.35f, 0.55f, 1f, alpha * 0.45f), 5f * zoom);
            DrawPrimitives.DrawLine(sb, from, end, BoltCoreColor(alpha), 2f * zoom);
            DrawPrimitives.DrawLine(sb, from, end, new Color(1f, 1f, 1f, alpha), 1f * zoom);
        }

        if (t < 0.7f)
        {
            var impact = screenPos;
            var flashR = 14f * zoom * (1f - t / 0.7f);
            var flash = AbilityId == "blood_bolt"
                ? new Color(0.85f, 0.2f, 0.25f, alpha * 0.35f)
                : new Color(0.4f, 0.65f, 1f, alpha * 0.35f);
            DrawPrimitives.FillCircle(sb, impact, flashR * 1.4f, flash);
            DrawPrimitives.FillCircle(sb, impact, flashR, BoltCoreColor(alpha * 0.75f));
            for (var i = 0; i < 4; i++)
            {
                var angle = i / 4f * MathHelper.TwoPi + _timer * 8f;
                var spark = impact + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * flashR * 1.2f;
                DrawPrimitives.FillCircle(sb, spark, 2.5f * zoom, new Color(1f, 1f, 1f, alpha * 0.8f));
            }
        }
    }

    private Color BoltCoreColor(float alpha) => AbilityId == "blood_bolt"
        ? new Color(0.95f, 0.35f, 0.4f, alpha)
        : new Color(0.75f, 0.9f, 1f, alpha);
}
