using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Circular minimap in the top-right showing local terrain and nearby entities.</summary>
public sealed class MinimapHud
{
    private static readonly Color WaterFill = new(0.04f, 0.18f, 0.22f, 0.95f);
    private static readonly Color FrameFill = new(0.33f, 0.14f, 0.1f, 0.94f);
    private static readonly Color CompassColor = new(0.92f, 0.22f, 0.2f, 0.95f);

    public Vector2 Center =>
        new(
            GameViewport.Width - Config.MinimapRightMargin - Config.MinimapScreenRadius,
            Config.MinimapTopMargin + Config.MinimapScreenRadius);

    public void Draw(
        SpriteBatch sb,
        SpriteFont font,
        Vector2 cameraWorld,
        long localId,
        IEnumerable<PlayerEntity> players,
        IEnumerable<WorldNpcEntity>? npcs = null,
        HousePlotZone? ownHouse = null)
    {
        var center = Center;
        var r = Config.MinimapScreenRadius;
        var map = WorldMap.SwaroviaMainland;
        var worldRadius = Config.MinimapWorldRadius;

        DrawPrimitives.FillCircle(sb, center, r + 3f, FrameFill);
        DrawPrimitives.FillCircle(sb, center, r, WaterFill);

        map.DrawLocalMinimap(sb, center, r, cameraWorld, worldRadius);

        DrawCompassMarkers(sb, font, center, r);

        DrawPrimitives.DrawCircleOutline(sb, center, r, FarmRpgUi.Parchment * 0.95f, 48, 2.5f);
        DrawPrimitives.DrawCircleOutline(sb, center, r + 3f, FarmRpgUi.Ink * 0.9f, 48, 1.5f);

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

        if (npcs != null)
        {
            foreach (var npc in npcs)
            {
                if (!npc.IsBoss && !npc.IsAttackable) continue;
                if (!InLocalRange(npc.Position, cameraWorld, worldRadius)) continue;
                var mapPos = WorldToMinimap(npc.Position, cameraWorld, worldRadius, center, r);
                if (npc.IsBoss)
                    MonsterMapIcon.Draw(sb, mapPos, 0.62f);
                else
                {
                    DrawPrimitives.FillCircle(sb, mapPos, 3f, new Color(0.92f, 0.24f, 0.22f, 0.9f));
                    DrawPrimitives.DrawCircleOutline(sb, mapPos, 3f, new Color(0.45f, 0.08f, 0.08f, 0.92f), 10, 1f);
                }
            }
        }

        if (ownHouse != null && InLocalRange(ownHouse.Center, cameraWorld, worldRadius))
        {
            var mapPos = WorldToMinimap(ownHouse.Center, cameraWorld, worldRadius, center, r);
            HomesteadMapIcon.Draw(sb, mapPos, 0.55f);
        }
    }

    private static void DrawCompassMarkers(SpriteBatch sb, SpriteFont font, Vector2 center, float radius)
    {
        const float scale = 1.05f;
        const float pad = 16f;
        DrawCompassLabel(sb, font, "N", center + new Vector2(0, -radius - pad), scale);
        DrawCompassLabel(sb, font, "S", center + new Vector2(0, radius + pad - 2), scale);
        DrawCompassLabel(sb, font, "W", center + new Vector2(-radius - pad, 0), scale);
        DrawCompassLabel(sb, font, "E", center + new Vector2(radius + pad - 2, 0), scale);
    }

    private static void DrawCompassLabel(SpriteBatch sb, SpriteFont font, string label, Vector2 pos, float scale)
    {
        var size = SpriteFontSafe.MeasureString(font, label) * scale;
        var drawPos = new Vector2(pos.X - size.X / 2f, pos.Y - size.Y / 2f);
        SpriteFontSafe.DrawOutlined(sb, font, label, drawPos, CompassColor, Color.Black,
            scale, outlinePx: 1.5f);
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
