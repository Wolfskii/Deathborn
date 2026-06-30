using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Circular minimap in the top-right showing the world map and player positions.</summary>
public sealed class MinimapHud
{
    public Vector2 Center =>
        new(
            GameViewport.Width - Config.MinimapMargin - Config.MinimapScreenRadius,
            Config.MinimapMargin + Config.MinimapScreenRadius);

    public void Draw(
        SpriteBatch sb,
        Vector2 cameraWorld,
        long localId,
        IEnumerable<PlayerEntity> players)
    {
        var center = Center;
        var r = Config.MinimapScreenRadius;
        var map = WorldMap.Realik;
        var mapSize = (int)MathF.Ceiling(r * 2f);
        var mapBounds = new Rectangle(
            (int)MathF.Floor(center.X - r),
            (int)MathF.Floor(center.Y - r),
            mapSize,
            mapSize);

        DrawPrimitives.FillCircle(sb, center, r + 3f, new Color(0.08f, 0.08f, 0.1f, 0.92f));
        DrawPrimitives.FillCircle(sb, center, r, new Color(0.06f, 0.12f, 0.24f, 0.95f));

        map.DrawMinimapLand(sb, mapBounds);

        DrawViewportRect(sb, mapBounds, cameraWorld, map);

        DrawPrimitives.DrawCircleOutline(sb, center, r, new Color(0.75f, 0.62f, 0.38f, 0.9f), 48, 2.5f);

        foreach (var player in players)
        {
            var mapPos = WorldToMinimap(player.Position, mapBounds, map);
            mapPos = ClampToCircle(mapPos, center, r - 4f);

            var isLocal = player.Id == localId;
            var dotR = isLocal ? 4f : 3f;
            var color = isLocal
                ? new Color(0.95f, 0.92f, 0.55f)
                : new Color(0.88f, 0.55f, 0.45f);

            DrawPrimitives.FillCircle(sb, mapPos, dotR + 1.5f, new Color(0, 0, 0, 0.45f));
            DrawPrimitives.FillCircle(sb, mapPos, dotR, color);
        }
    }

    private static void DrawViewportRect(
        SpriteBatch sb,
        Rectangle mapBounds,
        Vector2 cameraWorld,
        WorldMap map)
    {
        var zoom = GameViewport.WorldZoom;
        var halfW = GameViewport.Width / (2f * zoom);
        var halfH = GameViewport.Height / (2f * zoom);
        var topLeft = cameraWorld - new Vector2(halfW, halfH);
        var bottomRight = cameraWorld + new Vector2(halfW, halfH);

        var a = WorldToMinimap(topLeft, mapBounds, map);
        var b = WorldToMinimap(bottomRight, mapBounds, map);
        var color = new Color(1f, 1f, 1f, 0.22f);

        DrawPrimitives.DrawLine(sb, new Vector2(a.X, a.Y), new Vector2(b.X, a.Y), color, 1f);
        DrawPrimitives.DrawLine(sb, new Vector2(b.X, a.Y), new Vector2(b.X, b.Y), color, 1f);
        DrawPrimitives.DrawLine(sb, new Vector2(b.X, b.Y), new Vector2(a.X, b.Y), color, 1f);
        DrawPrimitives.DrawLine(sb, new Vector2(a.X, b.Y), new Vector2(a.X, a.Y), color, 1f);
    }

    private static Vector2 WorldToMinimap(Vector2 world, Rectangle bounds, WorldMap map) =>
        new(
            bounds.X + world.X / map.WorldWidth * bounds.Width,
            bounds.Y + world.Y / map.WorldHeight * bounds.Height);

    private static Vector2 ClampToCircle(Vector2 pos, Vector2 center, float maxDist)
    {
        var offset = pos - center;
        if (offset.LengthSquared() <= maxDist * maxDist)
            return pos;
        return center + Vector2.Normalize(offset) * maxDist;
    }
}
