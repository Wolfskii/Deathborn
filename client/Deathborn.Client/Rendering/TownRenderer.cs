using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Procedural town walls, buildings, and dirt paths between safe havens.</summary>
public static class TownRenderer
{
    private static readonly Color PathFill = new(92, 78, 58);
    private static readonly Color PathEdge = new(72, 60, 44);
    private static readonly Color WallStone = new(118, 112, 104);
    private static readonly Color WallTop = new(148, 142, 132);
    private static readonly Color Roof = new(88, 52, 38);
    private static readonly Color RoofLight = new(110, 68, 48);
    private static readonly Color Plaster = new(196, 188, 172);
    private static readonly Color PlasterDark = new(168, 158, 142);
    private static readonly Color Keep = new(132, 128, 138);
    private static readonly Color KeepDark = new(98, 94, 108);
    private static readonly Color Tower = new(156, 152, 162);
    private static readonly Color Wood = new(118, 88, 52);
    private static readonly Color Dock = new(102, 82, 58);

    public static void Draw(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (WorldZones.Towns.Count == 0)
            WorldZones.Initialize(WorldMap.Realik);

        DrawPaths(sb, camera, screenCenter, zoom);
        foreach (var town in WorldZones.Towns)
            DrawTown(sb, town, camera, screenCenter, zoom);
    }

    private static void DrawPaths(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var map = WorldMap.Realik;
        var tilePx = map.TileSize * zoom;
        if (tilePx < 0.5f) return;

        foreach (var (aId, bId) in WorldZones.PathLinks)
        {
            var a = WorldZones.Get(aId);
            var b = WorldZones.Get(bId);
            if (a == null || b == null) continue;
            DrawPathSegment(sb, a.Center, b.Center, map, camera, screenCenter, zoom);
        }
    }

    private static void DrawPathSegment(
        SpriteBatch sb, Vector2 from, Vector2 to, WorldMap map,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var delta = to - from;
        var len = delta.Length();
        if (len < 1f) return;

        var step = map.TileSize * 0.85f;
        var dir = delta / len;
        var count = (int)(len / step);
        var roadW = MathF.Max(2f, 22f * zoom);

        for (var i = 0; i <= count; i++)
        {
            var world = from + dir * (i * step);
            if (!map.IsWalkable(world.X, world.Y, 6f)) continue;

            var screen = WorldToScreen(world, camera, screenCenter, zoom);
            if (!OnScreen(screen, screenCenter)) continue;

            var rect = new Rectangle(
                (int)(screen.X - roadW * 0.5f),
                (int)(screen.Y - roadW * 0.35f),
                (int)roadW,
                (int)(roadW * 0.7f));
            DrawPrimitives.FillRect(sb, rect, PathFill);
            DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 1), PathEdge);
        }
    }

    private static void DrawTown(
        SpriteBatch sb, WorldZone town, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (!InView(town.Bounds, camera, screenCenter, zoom)) return;

        var b = town.Bounds;
        DrawWalls(sb, town, camera, screenCenter, zoom);
        DrawBuildings(sb, town, camera, screenCenter, zoom);

        // Town name plaque near the south gate
        var signPos = new Vector2(town.Center.X, town.Center.Y + town.HalfHeight - 8);
        var signScreen = WorldToScreen(signPos, camera, screenCenter, zoom);
        if (OnScreen(signScreen, screenCenter))
        {
            var plaque = new Rectangle((int)signScreen.X - 28, (int)signScreen.Y - 6, 56, 12);
            DrawPrimitives.FillRect(sb, plaque, new Color(48, 42, 34, 220));
            DrawPrimitives.FillRect(sb, new Rectangle(plaque.X, plaque.Y, plaque.Width, 1), new Color(180, 150, 90));
        }
    }

    private static void DrawWalls(
        SpriteBatch sb, WorldZone town, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var thick = MathF.Max(2f, 6f * zoom);
        var b = town.Bounds;
        var gateW = town.HalfWidth * 0.35f;

        // North wall
        DrawWallSegment(sb,
            new Vector2(b.Left, b.Top), new Vector2(b.Right, b.Top),
            thick, camera, screenCenter, zoom);
        // East wall
        DrawWallSegment(sb,
            new Vector2(b.Right, b.Top), new Vector2(b.Right, b.Bottom),
            thick, camera, screenCenter, zoom);
        // West wall
        DrawWallSegment(sb,
            new Vector2(b.Left, b.Top), new Vector2(b.Left, b.Bottom),
            thick, camera, screenCenter, zoom);
        // South wall with gate gap
        var cx = town.Center.X;
        DrawWallSegment(sb,
            new Vector2(b.Left, b.Bottom), new Vector2(cx - gateW, b.Bottom),
            thick, camera, screenCenter, zoom);
        DrawWallSegment(sb,
            new Vector2(cx + gateW, b.Bottom), new Vector2(b.Right, b.Bottom),
            thick, camera, screenCenter, zoom);

        // Corner towers for castles
        if (town.Style is TownStyle.Castle or TownStyle.Starter)
        {
            var tr = 14f;
            DrawTower(sb, new Vector2(b.Left, b.Top), tr, camera, screenCenter, zoom);
            DrawTower(sb, new Vector2(b.Right, b.Top), tr, camera, screenCenter, zoom);
            DrawTower(sb, new Vector2(b.Left, b.Bottom), tr, camera, screenCenter, zoom);
            DrawTower(sb, new Vector2(b.Right, b.Bottom), tr, camera, screenCenter, zoom);
        }
    }

    private static void DrawWallSegment(
        SpriteBatch sb, Vector2 aWorld, Vector2 bWorld, float thick,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var a = WorldToScreen(aWorld, camera, screenCenter, zoom);
        var b = WorldToScreen(bWorld, camera, screenCenter, zoom);
        DrawPrimitives.DrawLine(sb, a, b, WallStone, thick + 2f);
        DrawPrimitives.DrawLine(sb, a, b, WallTop, thick);
    }

    private static void DrawTower(
        SpriteBatch sb, Vector2 world, float radius, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var c = WorldToScreen(world, camera, screenCenter, zoom);
        var r = radius * zoom;
        DrawPrimitives.FillCircle(sb, c, r + 2f, WallStone);
        DrawPrimitives.FillCircle(sb, c, r, Tower);
        DrawPrimitives.DrawCircleOutline(sb, c, r, KeepDark, 12, 1.5f);
    }

    private static void DrawBuildings(
        SpriteBatch sb, WorldZone town, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        switch (town.Style)
        {
            case TownStyle.Castle:
                DrawKeep(sb, town.Center + new Vector2(0, -18), 52, 44, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(-58, 28), 36, 28, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(58, 28), 36, 28, camera, screenCenter, zoom);
                break;
            case TownStyle.Port:
                DrawHouse(sb, town.Center + new Vector2(-48, -10), 40, 30, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(42, -8), 38, 28, camera, screenCenter, zoom);
                DrawDock(sb, town.Center + new Vector2(0, 52), 70, camera, screenCenter, zoom);
                break;
            case TownStyle.Village:
                DrawHouse(sb, town.Center + new Vector2(-44, -16), 34, 26, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(0, -28), 38, 30, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(46, -12), 32, 24, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(-20, 36), 30, 22, camera, screenCenter, zoom);
                break;
            default:
                DrawHall(sb, town.Center + new Vector2(0, -32), 56, 40, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(-62, 18), 38, 28, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(62, 18), 38, 28, camera, screenCenter, zoom);
                DrawHouse(sb, town.Center + new Vector2(0, 42), 34, 26, camera, screenCenter, zoom);
                break;
        }
    }

    private static void DrawHouse(
        SpriteBatch sb, Vector2 world, float w, float h,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var screen = WorldToScreen(world, camera, screenCenter, zoom);
        var body = new Rectangle(
            (int)(screen.X - w * 0.5f * zoom),
            (int)(screen.Y - h * 0.35f * zoom),
            (int)(w * zoom),
            (int)(h * 0.65f * zoom));
        DrawPrimitives.FillRect(sb, body, Plaster);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, body.Width, 2), PlasterDark);

        var roofH = h * 0.4f * zoom;
        var roofBase = new Vector2(body.Center.X, body.Y);
        DrawPrimitives.FillTriangle(sb,
            roofBase + new Vector2(-body.Width * 0.55f, 0),
            roofBase + new Vector2(body.Width * 0.55f, 0),
            roofBase + new Vector2(0, -roofH),
            Roof);
        DrawPrimitives.DrawLine(sb,
            roofBase + new Vector2(-body.Width * 0.55f, 0),
            roofBase + new Vector2(0, -roofH),
            RoofLight, 2f);
    }

    private static void DrawHall(
        SpriteBatch sb, Vector2 world, float w, float h,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        DrawHouse(sb, world, w, h, camera, screenCenter, zoom);
        var screen = WorldToScreen(world, camera, screenCenter, zoom);
        var banner = new Rectangle((int)(screen.X - 6 * zoom), (int)(screen.Y - h * 0.55f * zoom), (int)(12 * zoom), (int)(16 * zoom));
        DrawPrimitives.FillRect(sb, banner, new Color(160, 50, 45));
    }

    private static void DrawKeep(
        SpriteBatch sb, Vector2 world, float w, float h,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var screen = WorldToScreen(world, camera, screenCenter, zoom);
        var body = new Rectangle(
            (int)(screen.X - w * 0.5f * zoom),
            (int)(screen.Y - h * 0.5f * zoom),
            (int)(w * zoom),
            (int)(h * zoom));
        DrawPrimitives.FillRect(sb, body, Keep);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, body.Width, 3), KeepDark);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X + 4, body.Y + 8, body.Width - 8, body.Height - 14), new Color(72, 68, 82));

        var battlement = body.Width / 6;
        for (var i = 0; i < 5; i++)
        {
            var bx = body.X + i * battlement + 2;
            DrawPrimitives.FillRect(sb, new Rectangle(bx, body.Y - (int)(6 * zoom), battlement - 3, (int)(6 * zoom)), KeepDark);
        }
    }

    private static void DrawDock(
        SpriteBatch sb, Vector2 world, float length, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var screen = WorldToScreen(world, camera, screenCenter, zoom);
        var dock = new Rectangle(
            (int)(screen.X - length * 0.5f * zoom),
            (int)(screen.Y - 4 * zoom),
            (int)(length * zoom),
            (int)(8 * zoom));
        DrawPrimitives.FillRect(sb, dock, Dock);
        for (var i = 0; i < 4; i++)
        {
            var px = dock.X + (i + 1) * dock.Width / 5;
            DrawPrimitives.FillRect(sb, new Rectangle(px, dock.Y - (int)(6 * zoom), 2, dock.Height + (int)(6 * zoom)), Wood);
        }
    }

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        (world - camera) * zoom + screenCenter;

    private static bool OnScreen(Vector2 screen, Vector2 screenCenter) =>
        screen.X > -80 && screen.Y > -80
        && screen.X < screenCenter.X * 2 + 80 && screen.Y < screenCenter.Y * 2 + 80;

    private static bool InView(Rectangle worldBounds, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var halfW = screenCenter.X / zoom + 80;
        var halfH = screenCenter.Y / zoom + 80;
        return worldBounds.Intersects(new Rectangle(
            (int)(camera.X - halfW),
            (int)(camera.Y - halfH),
            (int)(halfW * 2),
            (int)(halfH * 2)));
    }
}
