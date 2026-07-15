using Microsoft.Xna.Framework;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Axis-aligned hit boxes for NPC sprites — aligned to painted body pixels, not the shadow anchor.
/// Uses world simulation scale (tile size), not render-only DisplayScale.
/// </summary>
public static class NpcHitboxes
{
    private static float WorldScale => Config.WorldTileSize / Config.LegacyTileSize;

    public static void GetWorldAabb(WorldNpcEntity npc, Vector2 footAnchor, out Vector2 center, out float halfW, out float halfH)
    {
        if (npc.IsBoss || !npc.UsesSprite)
        {
            var r = npc.Radius * 1.15f;
            center = footAnchor;
            halfW = halfH = r;
            return;
        }

        var scale = WorldScale;
        var pad = NpcCatalog.Get(npc.DefId).HitPadding;

        if (TinyRpgCharacterSprites.TryGetBodyHitMetrics(npc.SpriteId, out var metrics))
        {
            center = footAnchor + new Vector2(0, metrics.CenterOffsetFromAnchorY * scale);
            halfW = metrics.HalfWidth * scale * pad;
            halfH = metrics.HalfHeight * scale * pad;
            return;
        }

        var entry = NpcCatalog.Get(npc.DefId);
        center = footAnchor + new Vector2(0, entry.HitCenterYOffset * scale);
        halfW = entry.HitHalfWidth * scale * pad;
        halfH = entry.HitHalfHeight * scale * pad;
    }

    /// <summary>Prefer server-synced target for hit registration (reduces miss from interpolation lag).</summary>
    public static Vector2 HitTestAnchor(WorldNpcEntity npc) => npc.Target;

    public static bool ContainsPoint(Vector2 point, Vector2 center, float halfW, float halfH, float padding = 0f) =>
        MathF.Abs(point.X - center.X) <= halfW + padding
        && MathF.Abs(point.Y - center.Y) <= halfH + padding;

    public static bool ProjectileHits(
        Vector2 previous,
        Vector2 current,
        float projectileRadius,
        WorldNpcEntity npc)
    {
        GetWorldAabb(npc, HitTestAnchor(npc), out var center, out var halfW, out var halfH);
        halfW += projectileRadius;
        halfH += projectileRadius;

        if (ContainsPoint(current, center, halfW, halfH))
            return true;
        if (ContainsPoint(previous, center, halfW, halfH))
            return true;

        return SegmentIntersectsAabb(previous, current, center, halfW, halfH);
    }

    public static bool CircleOverlaps(Vector2 circleCenter, float circleRadius, WorldNpcEntity npc)
    {
        GetWorldAabb(npc, HitTestAnchor(npc), out var center, out var halfW, out var halfH);
        var dx = MathF.Abs(circleCenter.X - center.X);
        var dy = MathF.Abs(circleCenter.Y - center.Y);
        var closestDx = MathF.Max(dx - halfW, 0f);
        var closestDy = MathF.Max(dy - halfH, 0f);
        return closestDx * closestDx + closestDy * closestDy <= circleRadius * circleRadius;
    }

    private static bool SegmentIntersectsAabb(
        Vector2 a, Vector2 b, Vector2 center, float halfW, float halfH)
    {
        var minX = center.X - halfW;
        var maxX = center.X + halfW;
        var minY = center.Y - halfH;
        var maxY = center.Y + halfH;

        if (ContainsPoint(a, center, halfW, halfH) || ContainsPoint(b, center, halfW, halfH))
            return true;

        var d = b - a;
        var tEnter = 0f;
        var tExit = 1f;

        if (!ClipSlab(a.X, d.X, minX, maxX, ref tEnter, ref tExit)) return false;
        if (!ClipSlab(a.Y, d.Y, minY, maxY, ref tEnter, ref tExit)) return false;

        return tEnter <= tExit;
    }

    private static bool ClipSlab(float origin, float dir, float min, float max, ref float tEnter, ref float tExit)
    {
        if (MathF.Abs(dir) < 0.0001f)
            return origin >= min && origin <= max;

        var t0 = (min - origin) / dir;
        var t1 = (max - origin) / dir;
        if (t0 > t1)
            (t0, t1) = (t1, t0);

        tEnter = MathF.Max(tEnter, t0);
        tExit = MathF.Min(tExit, t1);
        return tEnter <= tExit;
    }
}
