using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Net;

namespace Deathborn.Client.Rendering;

/// <summary>Tilled soil + crop sprites on homestead yards.</summary>
public static class FarmRenderer
{
    private static Texture2D? _dryHole;
    private static Texture2D? _wetHole;

    public static void Load(ContentManager content)
    {
        _dryHole = Try(content, "Tiles/farm/tilled_dry_hole");
        _wetHole = Try(content, "Tiles/farm/tilled_wet_hole");
    }

    private static Texture2D? Try(ContentManager content, string path)
    {
        try { return content.Load<Texture2D>(path); }
        catch (ContentLoadException) { return null; }
    }

    public static void DrawSoil(
        SpriteBatch sb,
        HousePlotZone house,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        // Tiles are solid squares so neighbours meet with no seams. Only the outer corners of
        // a patch are rounded, so the patch has a soft outline instead of a block edge.
        var tilled = new HashSet<(int, int)>();
        foreach (var tile in house.Crops)
            tilled.Add((tile.Tx, tile.Ty));

        var size = Math.Max(1, (int)MathF.Ceiling(FarmCatalog.TileSize * zoom));
        var edge = Math.Max(1, (int)MathF.Round(zoom));
        var radius = Math.Max(2, size / 4);
        foreach (var tile in house.Crops)
        {
            var origin = FarmCatalog.TileOrigin(tile.Tx, tile.Ty);
            var screen = WorldToScreen(origin, camera, screenCenter, zoom);
            var dest = new Rectangle((int)MathF.Floor(screen.X), (int)MathF.Floor(screen.Y), size, size);

            var top = !tilled.Contains((tile.Tx, tile.Ty - 1));
            var bottom = !tilled.Contains((tile.Tx, tile.Ty + 1));
            var left = !tilled.Contains((tile.Tx - 1, tile.Ty));
            var right = !tilled.Contains((tile.Tx + 1, tile.Ty));
            var corners = new SoilCorners(
                TopLeft: top && left,
                TopRight: top && right,
                BottomRight: bottom && right,
                BottomLeft: bottom && left);

            DrawSoilTile(sb, dest, tile.Watered ? WetSoil : DrySoil, tile.Watered ? WetSoilEdge : DrySoilEdge,
                top, right, bottom, left, corners, radius, edge);

            // A seeded hole keeps its sprite on top of the solid tile.
            var hole = !string.IsNullOrEmpty(tile.Crop) && tile.Stage <= 0;
            var holeTex = tile.Watered ? _wetHole : _dryHole;
            if (hole && holeTex != null)
                sb.Draw(holeTex, dest, Color.White);
        }
    }

    private readonly record struct SoilCorners(bool TopLeft, bool TopRight, bool BottomRight, bool BottomLeft);

    /// <summary>
    /// Draws one tile one pixel row at a time. Exterior corners are rounded with a circular
    /// inset; exterior sides get a darker rim.
    /// </summary>
    private static void DrawSoilTile(
        SpriteBatch sb, Rectangle dest, Color fill, Color rim,
        bool topOpen, bool rightOpen, bool bottomOpen, bool leftOpen,
        SoilCorners corners, int radius, int edge)
    {
        var h = dest.Height;
        for (var y = 0; y < h; y++)
        {
            var insetL = 0;
            var insetR = 0;
            if (corners.TopLeft && y < radius) insetL = CornerInset(radius, y);
            if (corners.BottomLeft && y >= h - radius) insetL = CornerInset(radius, h - 1 - y);
            if (corners.TopRight && y < radius) insetR = CornerInset(radius, y);
            if (corners.BottomRight && y >= h - radius) insetR = CornerInset(radius, h - 1 - y);

            var x0 = dest.X + insetL;
            var x1 = dest.Right - insetR;
            if (x1 <= x0) continue;

            var rowY = dest.Y + y;
            var rimRow = (topOpen && y < edge) || (bottomOpen && y >= h - edge);
            DrawPrimitives.FillRect(sb, new Rectangle(x0, rowY, x1 - x0, 1), rimRow ? rim : fill);

            if (rimRow) continue;
            if (leftOpen) DrawPrimitives.FillRect(sb, new Rectangle(x0, rowY, edge, 1), rim);
            if (rightOpen) DrawPrimitives.FillRect(sb, new Rectangle(x1 - edge, rowY, edge, 1), rim);
        }
    }

    /// <summary>Horizontal inset of a rounded corner at <paramref name="fromEdge"/> rows in (0 = the outer edge).</summary>
    private static int CornerInset(int radius, int fromEdge)
    {
        var dy = radius - fromEdge - 0.5f;
        var dx = MathF.Sqrt(MathF.Max(0f, radius * radius - dy * dy));
        return (int)MathF.Round(radius - dx);
    }

    private static readonly Color DrySoil = new(193, 111, 72);
    private static readonly Color DrySoilEdge = new(118, 60, 40);
    private static readonly Color WetSoil = new(120, 127, 222);
    private static readonly Color WetSoilEdge = new(48, 48, 108);

    public static void DrawCrops(
        SpriteBatch sb,
        HousePlotZone house,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        foreach (var tile in house.Crops)
        {
            if (string.IsNullOrEmpty(tile.Crop)) continue;
            DrawCrop(sb, tile, camera, screenCenter, zoom, Color.White);
        }
    }

    public static void DrawCrop(
        SpriteBatch sb,
        FarmCropState tile,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color tint)
    {
        var center = FarmCatalog.TileCenter(tile.Tx, tile.Ty);
        var screen = WorldToScreen(new Vector2(center.X, center.Y + FarmCatalog.TileSize * 0.5f), camera, screenCenter, zoom);
        var sheet = FarmCropSprites.Get(tile.Crop);
        if (sheet == null)
        {
            DrawPrimitives.FillRect(sb,
                new Rectangle((int)screen.X - 4, (int)screen.Y - 10, 8, 10),
                new Color(0.35f, 0.62f, 0.28f) * (tint.A / 255f));
            return;
        }

        var stages = FarmCropSprites.StageCount(sheet);
        var stage = Math.Clamp(tile.Stage, 0, stages - 1);
        var src = new Rectangle(stage * 16, 0, 16, sheet.Height);
        var origin = new Vector2(8f, sheet.Height);
        sb.Draw(sheet, screen, src, tint, 0f, origin, zoom, SpriteEffects.None, 0f);
    }

    public static void DrawTileHighlight(
        SpriteBatch sb,
        int tx,
        int ty,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color fill,
        Color edge)
    {
        var origin = FarmCatalog.TileOrigin(tx, ty);
        var tl = WorldToScreen(origin, camera, screenCenter, zoom);
        var br = WorldToScreen(origin + new Vector2(FarmCatalog.TileSize, FarmCatalog.TileSize), camera, screenCenter, zoom);
        var rect = new Rectangle(
            (int)MathF.Floor(tl.X),
            (int)MathF.Floor(tl.Y),
            Math.Max(1, (int)MathF.Ceiling(br.X - tl.X)),
            Math.Max(1, (int)MathF.Ceiling(br.Y - tl.Y)));
        DrawPrimitives.FillRect(sb, rect, fill);
        DrawPrimitives.DrawRectOutline(sb, rect, edge, Math.Max(1f, 1.5f * zoom));
    }

    public static Vector2 AnimalWorldPos(FarmAnimalState animal, double timeSeconds)
    {
        var t = timeSeconds * 0.55 + animal.Id * 1.37;
        var ox = (float)Math.Sin(t) * 10f;
        var oy = (float)Math.Cos(t * 0.73 + animal.Id) * 7f;
        return new Vector2((float)animal.X + ox, (float)animal.Y + oy);
    }

    public static Vector2 AnimalFacing(FarmAnimalState animal, double timeSeconds)
    {
        var t = timeSeconds * 0.55 + animal.Id * 1.37;
        var dx = (float)Math.Cos(t);
        var dy = -(float)Math.Sin(t * 0.73 + animal.Id);
        var v = new Vector2(dx, dy);
        return v.LengthSquared() < 0.001f ? Vector2.UnitX : Vector2.Normalize(v);
    }

    public static bool AnimalMoving(double timeSeconds, long animalId)
    {
        var pulse = Math.Sin(timeSeconds * 0.35 + animalId * 0.9);
        return pulse > -0.15;
    }

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        screenCenter + (world - camera) * zoom;
}
