using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Homestead interior: bedroom | hall | kitchen on the <see cref="HouseInteriorLayout"/> grid,
/// tiled from the Farm RPG house tileset. Furniture is drawn by the caller, depth-sorted with players.
/// </summary>
public static class HouseInteriorRenderer
{
    private static readonly Color VoidFill = new(8, 6, 10);
    private static readonly Color WallTop = new(46, 26, 36);
    private static readonly Color WallEdge = new(122, 62, 40);
    private static readonly Color DoorMat = new(110, 66, 40);
    private static readonly Color DoorMatEdge = new(72, 40, 24);
    private static readonly Color DoorHighlight = new(255, 235, 115, 190);
    private static readonly Color WindowPane = new(150, 205, 235);
    private static readonly Color WindowShine = new(220, 240, 250);
    private static readonly Color FallbackFloor = new(118, 92, 64);
    private static readonly Color FallbackWall = new(150, 110, 90);

    /// <summary>Per-room look: floor cells (1 = plain, 4 = 2x2 pattern), wallpaper group column, wall rows.</summary>
    private readonly record struct RoomTheme(Point[] Floor, int WallGroup, int WallUpperRow, int WallLowerRow, int[] WindowCols, string Curtain);

    private static readonly RoomTheme[] Themes =
    [
        // Bedroom: pale planks, pink stripes over a skirting board.
        new([new(26, 0)], 16, 10, 11, [4], "curtain_bedroom"),
        // Hall: herringbone parquet, peach wall over wood wainscot.
        new([new(19, 0), new(20, 0), new(19, 1), new(20, 1)], 0, 22, 23, [10, 15], "curtain_hall"),
        // Kitchen: diamond checker tiles, teal stripes over a skirting board.
        new([new(23, 0), new(24, 0), new(24, 0), new(23, 0)], 32, 16, 17, [22], "curtain_kitchen"),
    ];

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

        var center = house.Center;
        for (var row = 0; row < HouseInteriorLayout.Rows; row++)
        {
            for (var col = 0; col < HouseInteriorLayout.Cols; col++)
            {
                var dest = TileDest(center, col, row, camera, screenCenter, worldZoom);
                if (HouseInteriorLayout.IsWallTop(col, row))
                    continue;
                if (HouseInteriorLayout.IsWallFace(col, row))
                    DrawWallFace(sb, col, row, dest);
                else
                    DrawFloor(sb, col, row, dest);
            }
        }

        DrawWindows(sb, center, camera, screenCenter, worldZoom);
        DrawWallTops(sb, center, camera, screenCenter, worldZoom);
        DrawExitDoor(sb, center, camera, screenCenter, worldZoom, exitHighlighted);

        var floor = InteriorScreenRect(center, camera, screenCenter, worldZoom);
        if (!string.IsNullOrEmpty(title))
            DrawTitle(sb, font, SpriteFontSafe.Filter(title), floor);
    }

    /// <summary>Screen rectangle of a tile, snapped to whole pixels so neighbours share edges exactly.</summary>
    private static Rectangle TileDest(Vector2 center, int col, int row, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var tl = screenCenter + (HouseInteriorLayout.TileWorld(center, col, row) - camera) * zoom;
        var br = screenCenter + (HouseInteriorLayout.TileWorld(center, col + 1, row + 1) - camera) * zoom;
        var x0 = (int)MathF.Round(tl.X);
        var y0 = (int)MathF.Round(tl.Y);
        return new Rectangle(x0, y0, (int)MathF.Round(br.X) - x0, (int)MathF.Round(br.Y) - y0);
    }

    private static void DrawFloor(SpriteBatch sb, int col, int row, Rectangle dest)
    {
        var theme = Themes[HouseInteriorLayout.RoomAt(col)];
        if (!FarmRpgHouseInteriorTiles.IsLoaded)
        {
            DrawPrimitives.FillRect(sb, dest, FallbackFloor);
            return;
        }
        var cell = theme.Floor.Length == 4
            ? theme.Floor[(row & 1) * 2 + (col & 1)]
            : theme.Floor[0];
        FarmRpgHouseInteriorTiles.DrawCell(sb, FarmRpgHouseInteriorTiles.CellRect(cell.X, cell.Y), dest, Color.White);
    }

    private static void DrawWallFace(SpriteBatch sb, int col, int row, Rectangle dest)
    {
        var room = HouseInteriorLayout.RoomAt(col);
        var theme = Themes[room];
        if (!FarmRpgHouseInteriorTiles.IsLoaded)
        {
            DrawPrimitives.FillRect(sb, dest, FallbackWall);
            return;
        }
        var (first, last) = HouseInteriorLayout.RoomColumns(room);
        // Left end, repeatable middle, right end of the wallpaper group.
        var groupCol = theme.WallGroup + (col == first ? 1 : col == last ? 3 : 2);
        var sheetRow = row == 1 ? theme.WallUpperRow : theme.WallLowerRow;
        FarmRpgHouseInteriorTiles.DrawCell(sb, FarmRpgHouseInteriorTiles.CellRect(groupCol, sheetRow), dest, Color.White);
    }

    /// <summary>Windows on the upper wall-face row: a glass pane behind the room's curtain.</summary>
    private static void DrawWindows(SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        foreach (var theme in Themes)
        {
            foreach (var col in theme.WindowCols)
            {
                var tile = HouseInteriorLayout.TileWorld(center, col, 1);
                var paneTl = screenCenter + (tile + new Vector2(1, 2) - camera) * zoom;
                var paneBr = screenCenter + (tile + new Vector2(15, 16) - camera) * zoom;
                var pane = new Rectangle(
                    (int)MathF.Round(paneTl.X), (int)MathF.Round(paneTl.Y),
                    (int)MathF.Round(paneBr.X - paneTl.X), (int)MathF.Round(paneBr.Y - paneTl.Y));
                DrawPrimitives.FillRect(sb, pane, WindowPane);
                DrawPrimitives.FillRect(sb, new Rectangle(pane.X, pane.Y, pane.Width, Math.Max(1, (int)zoom)), WindowShine);

                // Curtain: bottom-centre one art px below the pane.
                var anchor = screenCenter + (tile + new Vector2(8, 18) - camera) * zoom;
                FurnitureSprites.TryDrawSprite(sb, theme.Curtain, anchor, zoom, Color.White);
            }
        }
    }

    /// <summary>Dark wall tops with a wood edge wherever they meet floor or the open side of a wall face.</summary>
    private static void DrawWallTops(SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var edge = Math.Max(1, (int)MathF.Round(2 * zoom));
        for (var row = 0; row < HouseInteriorLayout.Rows; row++)
        {
            for (var col = 0; col < HouseInteriorLayout.Cols; col++)
            {
                if (!HouseInteriorLayout.IsWallTop(col, row)) continue;
                var dest = TileDest(center, col, row, camera, screenCenter, zoom);
                DrawPrimitives.FillRect(sb, dest, WallTop);

                bool Open(int c, int r) =>
                    c >= 0 && r >= 0 && c < HouseInteriorLayout.Cols && r < HouseInteriorLayout.Rows
                    && !HouseInteriorLayout.IsWallTop(c, r);

                if (Open(col + 1, row))
                    DrawPrimitives.FillRect(sb, new Rectangle(dest.Right - edge, dest.Y, edge, dest.Height), WallEdge);
                if (Open(col - 1, row))
                    DrawPrimitives.FillRect(sb, new Rectangle(dest.X, dest.Y, edge, dest.Height), WallEdge);
                if (Open(col, row - 1))
                    DrawPrimitives.FillRect(sb, new Rectangle(dest.X, dest.Y, dest.Width, edge), WallEdge);
                // The wall face already shows the bottom of the north wall top.
                if (Open(col, row + 1) && !HouseInteriorLayout.IsWallFace(col, row + 1))
                    DrawPrimitives.FillRect(sb, new Rectangle(dest.X, dest.Bottom - edge, dest.Width, edge), WallEdge);
            }
        }
    }

    private static void DrawExitDoor(
        SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter, float zoom, bool highlighted)
    {
        var left = TileDest(center, HouseInteriorLayout.ExitLeft, HouseInteriorLayout.Rows - 1, camera, screenCenter, zoom);
        var right = TileDest(center, HouseInteriorLayout.ExitRight, HouseInteriorLayout.Rows - 1, camera, screenCenter, zoom);
        var inset = Math.Max(1, (int)MathF.Round(2 * zoom));
        var mat = new Rectangle(left.X + inset, left.Y, right.Right - left.X - inset * 2, left.Height);
        DrawPrimitives.FillRect(sb, mat, DoorMat);
        DrawBorder(sb, mat, highlighted ? DoorHighlight : DoorMatEdge, highlighted ? Math.Max(2, inset) : Math.Max(1, inset / 2));
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
