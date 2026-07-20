using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Three connected homestead rooms (bedroom | hall | kitchen) using Farm RPG house tiles.</summary>
public static class HouseInteriorRenderer
{
    private static readonly Color VoidFill = new(8, 6, 10);
    private static readonly Color DoorFill = new(72, 44, 26);
    private static readonly Color DoorFillHi = new(98, 60, 36);
    private static readonly Color DoorHighlight = new(255, 235, 115, 170);
    private static readonly Color FallbackFloor = new(118, 92, 64);
    private static readonly Color FallbackWall = new(82, 66, 50);

    public static Rectangle InteriorScreenRect(Vector2 houseCenter, Vector2 camera, Vector2 screenCenter, float worldZoom)
    {
        var worldMin = houseCenter + HousingConstants.InteriorLocalMin;
        var worldMax = houseCenter + HousingConstants.InteriorLocalMax;
        var tl = screenCenter + (worldMin - camera) * worldZoom;
        var br = screenCenter + (worldMax - camera) * worldZoom;
        var w = Math.Max(32, (int)MathF.Round(br.X - tl.X));
        var h = Math.Max(32, (int)MathF.Round(br.Y - tl.Y));
        return new Rectangle((int)MathF.Round(tl.X), (int)MathF.Round(tl.Y), w, h);
    }

    public static void Draw(
        SpriteBatch sb,
        SpriteFont font,
        HousePlotZone house,
        Vector2 camera,
        Vector2 screenCenter,
        float worldZoom,
        bool exitHighlighted,
        string title)
    {
        if (worldZoom < 0.01f) worldZoom = 1f;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), VoidFill);

        if (FarmRpgHouseInteriorTiles.IsLoaded)
            DrawTiledRooms(sb, house.Center, camera, screenCenter, worldZoom);
        else
            DrawFallbackFloor(sb, house.Center, camera, screenCenter, worldZoom);

        DrawInteriorWallsOverlay(sb, house.Center, camera, screenCenter, worldZoom);
        DrawExitDoor(sb, house.Center, camera, screenCenter, worldZoom, exitHighlighted);

        var floor = InteriorScreenRect(house.Center, camera, screenCenter, worldZoom);
        if (!string.IsNullOrEmpty(title))
            DrawTitle(sb, font, SpriteFontSafe.Filter(title), floor);

        var furniture = house.Furniture;
        if (furniture == null) return;
        foreach (var item in furniture)
        {
            if (item == null || string.IsNullOrEmpty(item.Type)) continue;
            HouseRenderer.DrawFurnitureItem(sb, item, house.Center, camera, screenCenter, worldZoom, 1f);
        }
    }

    private static void DrawTiledRooms(
        SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var tile = Config.WorldTileSize; // 32
        var artScale = tile / (float)FarmRpgHouseInteriorTiles.Cell;
        var drawScale = artScale * zoom;

        var min = center + HousingConstants.InteriorLocalMin;
        var max = center + HousingConstants.InteriorLocalMax;
        var midL = center.X - HousingConstants.InteriorHalfW / 3f;
        var midR = center.X + HousingConstants.InteriorHalfW / 3f;

        for (var y = min.Y; y < max.Y; y += tile)
        {
            for (var x = min.X; x < max.X; x += tile)
            {
                var src = FloorFor(x, midL, midR, (int)((x + y) / tile));
                var screen = screenCenter + (new Vector2(x, y) - camera) * zoom;
                FarmRpgHouseInteriorTiles.DrawTile(sb, src, screen, drawScale, Color.White);
            }
        }
    }

    private static Rectangle FloorFor(float x, float midL, float midR, int checker)
    {
        if (x < midL)
            return FarmRpgHouseInteriorTiles.FloorBedroom;
        if (x >= midR)
            return (checker & 1) == 0
                ? FarmRpgHouseInteriorTiles.FloorKitchen
                : FarmRpgHouseInteriorTiles.FloorHallAlt;
        return (checker & 1) == 0
            ? FarmRpgHouseInteriorTiles.FloorHall
            : FarmRpgHouseInteriorTiles.FloorHallAlt;
    }

    private static void DrawInteriorWallsOverlay(
        SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var walls = new List<(float L, float R, float T, float B)>(12);
        HousingCollision.GetInteriorWalls(center, walls);
        foreach (var w in walls)
        {
            var tl = screenCenter + (new Vector2(w.L, w.T) - camera) * zoom;
            var br = screenCenter + (new Vector2(w.R, w.B) - camera) * zoom;
            var rect = new Rectangle(
                (int)tl.X, (int)tl.Y,
                Math.Max(1, (int)(br.X - tl.X)),
                Math.Max(1, (int)(br.Y - tl.Y)));

            if (FarmRpgHouseInteriorTiles.IsLoaded)
                FillWallWithTiles(sb, center, w, camera, screenCenter, zoom);
            else
                DrawPrimitives.FillRect(sb, rect, FallbackWall);
        }
    }

    private static void FillWallWithTiles(
        SpriteBatch sb,
        Vector2 houseCenter,
        (float L, float R, float T, float B) wall,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var tile = Config.WorldTileSize;
        var artScale = tile / (float)FarmRpgHouseInteriorTiles.Cell;
        var drawScale = artScale * zoom;
        var midX = (wall.L + wall.R) * 0.5f;
        var (fill, trim) = WallTheme(midX, houseCenter);

        for (var y = wall.T; y < wall.B - 0.5f; y += tile)
        {
            for (var x = wall.L; x < wall.R - 0.5f; x += tile)
            {
                var remainX = Math.Min(tile, wall.R - x);
                var remainY = Math.Min(tile, wall.B - y);
                if (remainX < tile * 0.25f || remainY < tile * 0.25f) continue;
                var src = y <= wall.T + tile * 0.5f ? trim : fill;
                var screen = screenCenter + (new Vector2(x, y) - camera) * zoom;
                FarmRpgHouseInteriorTiles.DrawTile(sb, src, screen, drawScale, Color.White);
            }
        }
    }

    private static (Rectangle fill, Rectangle trim) WallTheme(float x, Vector2 center)
    {
        var midL = center.X - HousingConstants.InteriorHalfW / 3f;
        var midR = center.X + HousingConstants.InteriorHalfW / 3f;
        if (x < midL)
            return (FarmRpgHouseInteriorTiles.WallPeach, FarmRpgHouseInteriorTiles.WallPeachTrim);
        if (x >= midR)
            return (FarmRpgHouseInteriorTiles.WallTeal, FarmRpgHouseInteriorTiles.WallTealTrim);
        return (FarmRpgHouseInteriorTiles.WallWood, FarmRpgHouseInteriorTiles.WallWoodTrim);
    }

    private static void DrawExitDoor(
        SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom, bool highlighted)
    {
        var door = HousingConstants.InteriorDoorWorldPosition(center);
        var doorW = 44f;
        var doorH = 28f;
        var tl = screenCenter + (door + new Vector2(-doorW * 0.5f, -doorH) - camera) * zoom;
        var rect = new Rectangle((int)tl.X, (int)tl.Y, (int)(doorW * zoom), (int)(doorH * zoom));
        DrawPrimitives.FillRect(sb, rect, highlighted ? DoorFillHi : DoorFill);
        DrawBorder(sb, rect, highlighted ? DoorHighlight : new Color(56, 44, 34), highlighted ? 2 : 1);
    }

    private static void DrawFallbackFloor(
        SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var floor = InteriorScreenRect(center, camera, screenCenter, zoom);
        DrawPrimitives.FillRect(sb, floor, FallbackFloor);
    }

    private static void DrawTitle(SpriteBatch sb, SpriteFont font, string title, Rectangle floor)
    {
        var size = font.MeasureString(title);
        var pos = new Vector2(floor.Center.X - size.X / 2f, floor.Y - 22 - size.Y);
        if (pos.Y < 8) pos.Y = 8;
        sb.DrawString(font, title, pos + new Vector2(1, 1), new Color(0, 0, 0, 180));
        SpriteFontSafe.DrawString(sb, font, title, pos, new Color(220, 200, 150));
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0) return;
        thickness = Math.Min(thickness, Math.Min(rect.Width, rect.Height));
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
