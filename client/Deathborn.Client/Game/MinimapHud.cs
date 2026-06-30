using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Circular minimap in the top-right showing local terrain and nearby entities.</summary>
public sealed class MinimapHud
{
    private static readonly Color WaterFill = new(0.06f, 0.12f, 0.24f, 0.95f);
    private static readonly Color FrameFill = new(0.08f, 0.08f, 0.1f, 0.92f);

    public Vector2 Center =>
        new(
            GameViewport.Width - Config.MinimapMargin - Config.MinimapScreenRadius,
            Config.MinimapMargin + Config.MinimapScreenRadius);

    public void Draw(
        SpriteBatch sb,
        Vector2 cameraWorld,
        long localId,
        IEnumerable<PlayerEntity> players,
        IEnumerable<BossEntity>? bosses = null,
        HousePlotZone? ownHouse = null)
    {
        var center = Center;
        var r = Config.MinimapScreenRadius;
        var map = WorldMap.Realik;
        var worldRadius = Config.MinimapWorldRadius;

        DrawPrimitives.FillCircle(sb, center, r + 3f, FrameFill);
        DrawPrimitives.FillCircle(sb, center, r, WaterFill);

        map.DrawLocalMinimap(sb, center, r, cameraWorld, worldRadius);

        DrawViewportRect(sb, cameraWorld, worldRadius, center, r);

        DrawPrimitives.DrawCircleOutline(sb, center, r, new Color(0.75f, 0.62f, 0.38f, 0.9f), 48, 2.5f);

        if (WorldZones.Towns.Count == 0)
            WorldZones.Initialize(map);

        foreach (var town in WorldZones.Towns)
        {
            if (!InLocalRange(town.Center, cameraWorld, worldRadius)) continue;
            var mapPos = WorldToMinimap(town.Center, cameraWorld, worldRadius, center, r);
            DrawPrimitives.FillCircle(sb, mapPos, 3f, new Color(0.35f, 0.75f, 0.45f, 0.85f));
            DrawPrimitives.DrawCircleOutline(sb, mapPos, 3f, new Color(0.2f, 0.45f, 0.28f, 0.9f), 10, 1f);
        }

        foreach (var player in players)
        {
            if (player.IsDead) continue;
            if (!InLocalRange(player.Position, cameraWorld, worldRadius)) continue;

            var mapPos = WorldToMinimap(player.Position, cameraWorld, worldRadius, center, r);
            var isLocal = player.Id == localId;
            var dotR = isLocal ? 4f : 3f;
            var color = isLocal
                ? new Color(0.95f, 0.92f, 0.55f)
                : new Color(0.88f, 0.55f, 0.45f);

            DrawPrimitives.FillCircle(sb, mapPos, dotR + 1.5f, new Color(0, 0, 0, 0.45f));
            DrawPrimitives.FillCircle(sb, mapPos, dotR, color);
        }

        if (bosses != null)
        {
            foreach (var boss in bosses)
            {
                if (!InLocalRange(boss.Position, cameraWorld, worldRadius)) continue;
                var mapPos = WorldToMinimap(boss.Position, cameraWorld, worldRadius, center, r);
                DrawPrimitives.FillCircle(sb, mapPos, 4.5f, new Color(0.92f, 0.35f, 0.32f, 0.9f));
                DrawPrimitives.DrawCircleOutline(sb, mapPos, 4.5f, new Color(0.55f, 0.12f, 0.12f, 0.95f), 12, 1.5f);
            }
        }

        if (ownHouse != null)
        {
            var mapPos = WorldToMinimap(ownHouse.Center, cameraWorld, worldRadius, center, r);
            DrawPrimitives.FillCircle(sb, mapPos, 4f, new Color(0.55f, 0.82f, 0.95f, 0.95f));
            DrawPrimitives.DrawCircleOutline(sb, mapPos, 4f, new Color(0.25f, 0.45f, 0.62f, 0.95f), 12, 1.5f);
        }
    }

    private static void DrawViewportRect(
        SpriteBatch sb,
        Vector2 cameraWorld,
        float worldRadius,
        Vector2 center,
        float radius)
    {
        var zoom = GameViewport.WorldZoom;
        var halfW = GameViewport.Width / (2f * zoom);
        var halfH = GameViewport.Height / (2f * zoom);
        var topLeft = cameraWorld - new Vector2(halfW, halfH);
        var bottomRight = cameraWorld + new Vector2(halfW, halfH);

        var a = WorldToMinimap(topLeft, cameraWorld, worldRadius, center, radius);
        var b = WorldToMinimap(bottomRight, cameraWorld, worldRadius, center, radius);
        var color = new Color(1f, 1f, 1f, 0.22f);

        DrawClippedLine(sb, a, new Vector2(b.X, a.Y), color, center, radius);
        DrawClippedLine(sb, new Vector2(b.X, a.Y), b, color, center, radius);
        DrawClippedLine(sb, b, new Vector2(a.X, b.Y), color, center, radius);
        DrawClippedLine(sb, new Vector2(a.X, b.Y), a, color, center, radius);
    }

    private static void DrawClippedLine(
        SpriteBatch sb, Vector2 a, Vector2 b, Color color, Vector2 center, float radius)
    {
        if (!SegmentIntersectsCircle(a, b, center, radius)) return;
        a = ClampToCircle(a, center, radius - 1f);
        b = ClampToCircle(b, center, radius - 1f);
        DrawPrimitives.DrawLine(sb, a, b, color, 1f);
    }

    private static bool SegmentIntersectsCircle(Vector2 a, Vector2 b, Vector2 center, float radius)
    {
        a = ClampToCircle(a, center, radius);
        b = ClampToCircle(b, center, radius);
        return Vector2.DistanceSquared(a, center) <= radius * radius
            || Vector2.DistanceSquared(b, center) <= radius * radius;
    }

    private static bool InLocalRange(Vector2 world, Vector2 worldCenter, float worldRadius)
    {
        var dx = MathF.Abs(world.X - worldCenter.X);
        var dy = MathF.Abs(world.Y - worldCenter.Y);
        return dx <= worldRadius && dy <= worldRadius;
    }

    private static Vector2 WorldToMinimap(
        Vector2 world, Vector2 worldCenter, float worldRadius, Vector2 minimapCenter, float minimapRadius)
    {
        var scale = minimapRadius / worldRadius;
        var pos = minimapCenter + (world - worldCenter) * scale;
        return ClampToCircle(pos, minimapCenter, minimapRadius - 2f);
    }

    private static Vector2 ClampToCircle(Vector2 pos, Vector2 center, float maxDist)
    {
        var offset = pos - center;
        if (offset.LengthSquared() <= maxDist * maxDist)
            return pos;
        return center + Vector2.Normalize(offset) * maxDist;
    }
}
