using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Rendering;

/// <summary>Top-down interior room view when a player is inside a homestead.</summary>
public static class HouseInteriorRenderer
{
    private const float TileWorldSize = 32f;

    private static readonly Color VoidFill = new(8, 6, 10);
    private static readonly Color FloorA = new(118, 92, 64);
    private static readonly Color FloorB = new(102, 78, 54);
    private static readonly Color WallFill = new(82, 66, 50);
    private static readonly Color WallTop = new(96, 78, 60);
    private static readonly Color WallTrim = new(56, 44, 34);
    private static readonly Color DoorFill = new(72, 44, 26);
    private static readonly Color DoorFillHi = new(98, 60, 36);
    private static readonly Color DoorHighlight = new(255, 235, 115, 170);
    private static readonly Color RugAccent = new(140, 50, 46, 90);

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

        var floor = InteriorScreenRect(house.Center, camera, screenCenter, worldZoom);
        if (floor.Width <= 0 || floor.Height <= 0) return;

        var wall = Math.Max(8, (int)(12 * worldZoom));

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), VoidFill);
        DrawTiledFloor(sb, floor, worldZoom);
        DrawCenterRug(sb, floor);
        DrawWalls(sb, floor, wall, worldZoom, exitHighlighted);

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

    private static void DrawTiledFloor(SpriteBatch sb, Rectangle floor, float worldZoom)
    {
        DrawPrimitives.FillRect(sb, floor, FloorA);

        var tilePx = Math.Max(8, (int)MathF.Round(TileWorldSize * worldZoom));
        for (var y = 0; y < floor.Height; y += tilePx)
        {
            for (var x = 0; x < floor.Width; x += tilePx)
            {
                var col = x / tilePx;
                var row = y / tilePx;
                if ((row + col) % 2 == 0) continue;

                var w = Math.Min(tilePx + 1, floor.Width - x);
                var h = Math.Min(tilePx + 1, floor.Height - y);
                DrawPrimitives.FillRect(sb, new Rectangle(floor.X + x, floor.Y + y, w, h), FloorB);
            }
        }
    }

    private static void DrawCenterRug(SpriteBatch sb, Rectangle floor)
    {
        if (floor.Width < 48 || floor.Height < 48) return;
        var rugW = Math.Max(16, (int)(floor.Width * 0.36f));
        var rugH = Math.Max(12, (int)(floor.Height * 0.22f));
        var rug = new Rectangle(floor.Center.X - rugW / 2, floor.Center.Y - rugH / 2 - 6, rugW, rugH);
        if (rug.Width <= 0 || rug.Height <= 0) return;
        DrawPrimitives.FillRect(sb, rug, RugAccent);
    }

    private static void DrawWalls(SpriteBatch sb, Rectangle floor, int wall, float worldZoom, bool exitHighlighted)
    {
        var topH = wall + Math.Max(2, (int)(4 * worldZoom));
        var topRect = SafeRect(floor.X - wall, floor.Y - topH, floor.Width + wall * 2, topH);
        DrawPrimitives.FillRect(sb, topRect, WallTop);
        DrawBorder(sb, topRect, WallTrim, 2);

        DrawPrimitives.FillRect(sb, SafeRect(floor.X - wall, floor.Y, wall, floor.Height), WallFill);
        DrawPrimitives.FillRect(sb, SafeRect(floor.Right, floor.Y, wall, floor.Height), WallFill);

        var doorW = Math.Clamp((int)(22 * worldZoom), 18, floor.Width / 3);
        var doorH = Math.Clamp((int)(18 * worldZoom), 14, floor.Height / 2);
        var gapX = floor.Center.X - doorW / 2;

        var leftWallW = gapX - (floor.X - wall);
        var rightWallX = gapX + doorW;
        var rightWallW = (floor.Right + wall) - rightWallX;

        DrawPrimitives.FillRect(sb, SafeRect(floor.X - wall, floor.Bottom, leftWallW, wall), WallFill);
        DrawPrimitives.FillRect(sb, SafeRect(rightWallX, floor.Bottom, rightWallW, wall), WallFill);

        var doorY = Math.Clamp(floor.Bottom - doorH + 2, floor.Y, floor.Bottom);
        var doorRect = SafeRect(gapX, doorY, doorW, Math.Min(doorH, floor.Bottom - doorY + wall));
        DrawPrimitives.FillRect(sb, doorRect, exitHighlighted ? DoorFillHi : DoorFill);
        DrawBorder(sb, doorRect, exitHighlighted ? DoorHighlight : WallTrim, exitHighlighted ? 2 : 1);
    }

    private static void DrawTitle(SpriteBatch sb, SpriteFont font, string title, Rectangle floor)
    {
        var size = font.MeasureString(title);
        var pos = new Vector2(floor.Center.X - size.X / 2f, floor.Y - 22 - size.Y);
        if (pos.Y < 8) pos.Y = 8;
        sb.DrawString(font, title, pos + new Vector2(1, 1), new Color(0, 0, 0, 180));
        SpriteFontSafe.DrawString(sb, font, title, pos, new Color(220, 200, 150));
    }

    private static Rectangle SafeRect(int x, int y, int w, int h) =>
        new(x, y, Math.Max(0, w), Math.Max(0, h));

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
