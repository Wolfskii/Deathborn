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
    private static readonly Color WetSoil = new(122, 70, 46);
    private static readonly Color WetSoilEdge = new(70, 34, 36);

    /// <summary>Last seen stage per tile and when it changed, for the little "grew" pop.</summary>
    private static readonly Dictionary<(int, int), (int Stage, double ChangedAt)> SeenStages = [];
    private const double GrowPopSeconds = 0.35;

    private static readonly Color WiltTint = new(196, 164, 112);
    private static readonly Color DropFill = new(92, 170, 236);
    private static readonly Color DropLight = new(200, 236, 255);
    private static readonly Color DropEdge = new(32, 66, 110);
    private static readonly Color Sparkle = new(255, 238, 140);

    public static void DrawCrops(
        SpriteBatch sb,
        HousePlotZone house,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        var now = Environment.TickCount64 / 1000.0;
        foreach (var tile in house.Crops)
        {
            if (string.IsNullOrEmpty(tile.Crop))
            {
                SeenStages.Remove((tile.Tx, tile.Ty));
                continue;
            }

            // Pop when the server reports a new stage (not on the first sighting after load).
            var key = (tile.Tx, tile.Ty);
            if (!SeenStages.TryGetValue(key, out var seen))
                SeenStages[key] = seen = (tile.Stage, double.NegativeInfinity);
            else if (seen.Stage != tile.Stage)
                SeenStages[key] = seen = (tile.Stage, now);
            var popT = (now - seen.ChangedAt) / GrowPopSeconds;
            var pop = popT is >= 0 and < 1 ? 1f + 0.28f * MathF.Sin((float)popT * MathF.PI) : 1f;

            // Wilted plants droop (squashed) and turn straw-coloured.
            var scale = new Vector2(pop, tile.Wilted ? pop * 0.82f : pop);
            DrawCrop(sb, tile, camera, screenCenter, zoom, tile.Wilted ? WiltTint : Color.White, scale);

            var top = CropTopScreen(tile, camera, screenCenter, zoom);
            if (tile.Thirsty)
                DrawThirstyDrop(sb, top, zoom, (float)now + tile.Tx * 0.37f, tile.Wilted);
            else if (tile.Ready)
                DrawReadySparkle(sb, top, zoom, (float)now + tile.Ty * 0.53f);
        }
    }

    /// <summary>Screen point just above the crop sprite's top edge.</summary>
    private static Vector2 CropTopScreen(FarmCropState tile, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var center = FarmCatalog.TileCenter(tile.Tx, tile.Ty);
        var bottom = WorldToScreen(new Vector2(center.X, center.Y + FarmCatalog.TileSize * 0.5f), camera, screenCenter, zoom);
        var sheet = FarmCropSprites.Get(tile.Crop);
        var height = FarmCropSprites.VisibleHeight(tile.Crop, tile.Stage, sheet?.Height ?? 16);
        // The visible top of this growth stage, so the marker sits above the leaves, not in them.
        return bottom - new Vector2(0, (height + 1) * zoom);
    }

    /// <summary>Bobbing water drop over a crop that needs watering (red-rimmed when wilted).</summary>
    private static void DrawThirstyDrop(SpriteBatch sb, Vector2 anchor, float zoom, float time, bool urgent)
    {
        // Floats above the plant's top leaves (anchor), small enough not to hide the crop.
        var bob = MathF.Sin(time * 3.2f) * 0.8f * zoom;
        var c = anchor + new Vector2(0, -7f * zoom + bob);
        var r = 1.8f * zoom;
        var edge = urgent ? new Color(170, 52, 40) : DropEdge;
        DrawPrimitives.FillCircle(sb, c, r + MathF.Max(1f, zoom * 0.5f), edge, 12);
        DrawPrimitives.FillTriangle(sb,
            c + new Vector2(-r - zoom * 0.5f, -r * 0.2f), c + new Vector2(r + zoom * 0.5f, -r * 0.2f),
            c + new Vector2(0, -r * 2.6f), edge);
        DrawPrimitives.FillCircle(sb, c, r, DropFill, 12);
        DrawPrimitives.FillTriangle(sb,
            c + new Vector2(-r, -r * 0.2f), c + new Vector2(r, -r * 0.2f), c + new Vector2(0, -r * 2.3f), DropFill);
        DrawPrimitives.FillCircle(sb, c + new Vector2(-r * 0.35f, -r * 0.3f), MathF.Max(1f, r * 0.3f), DropLight, 8);
    }

    /// <summary>Twinkling star over a crop that is ready to harvest.</summary>
    private static void DrawReadySparkle(SpriteBatch sb, Vector2 anchor, float zoom, float time)
    {
        var twinkle = 0.5f + 0.5f * MathF.Sin(time * 4f);
        if (twinkle < 0.15f) return;
        var c = anchor + new Vector2(3f * zoom, -3f * zoom);
        var arm = (1.6f + 1.4f * twinkle) * zoom;
        var thick = MathF.Max(1f, zoom * 0.7f);
        var color = Sparkle * (0.5f + 0.5f * twinkle);
        DrawPrimitives.DrawLine(sb, c - new Vector2(arm, 0), c + new Vector2(arm, 0), color, thick);
        DrawPrimitives.DrawLine(sb, c - new Vector2(0, arm), c + new Vector2(0, arm), color, thick);
    }

    // ---- Watering splash (local feedback while the server confirms).

    private static readonly List<(Vector2 World, double StartedAt)> Splashes = [];
    private const double SplashSeconds = 0.5;

    public static void AddWaterSplash(Vector2 worldCenter) =>
        Splashes.Add((worldCenter, Environment.TickCount64 / 1000.0));

    /// <summary>Droplets falling onto the tile from the watering can.</summary>
    public static void DrawWaterSplashes(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var now = Environment.TickCount64 / 1000.0;
        Splashes.RemoveAll(s => now - s.StartedAt > SplashSeconds);
        foreach (var (world, started) in Splashes)
        {
            var t = (float)((now - started) / SplashSeconds);
            var center = WorldToScreen(world, camera, screenCenter, zoom);
            for (var i = 0; i < 7; i++)
            {
                var x = ((i * 0.618f) % 1f - 0.5f) * 11f * zoom;
                var startY = -14f * zoom - (i % 3) * 2f * zoom;
                var y = startY + t * 16f * zoom;
                var a = 1f - t;
                DrawPrimitives.FillRect(sb,
                    new Rectangle((int)(center.X + x), (int)(center.Y + y), Math.Max(1, (int)zoom), Math.Max(2, (int)(2.5f * zoom))),
                    DropLight * a);
            }
            var ring = (2f + t * 6f) * zoom;
            DrawPrimitives.DrawEllipseOutline(sb, center + new Vector2(0, 2f * zoom), ring, ring * 0.45f, DropLight * (1f - t), 16,
                MathF.Max(1f, zoom * 0.5f));
        }
    }

    public static void DrawCrop(
        SpriteBatch sb,
        FarmCropState tile,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color tint) =>
        DrawCrop(sb, tile, camera, screenCenter, zoom, tint, Vector2.One);

    /// <summary>Crop sprite on its tile; <paramref name="scale"/> stretches it around the base (pop / droop).</summary>
    public static void DrawCrop(
        SpriteBatch sb,
        FarmCropState tile,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color tint,
        Vector2 scale)
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
        sb.Draw(sheet, screen, src, tint, 0f, origin, scale * zoom, SpriteEffects.None, 0f);
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
