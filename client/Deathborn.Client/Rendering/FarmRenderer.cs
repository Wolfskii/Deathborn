using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Net;

namespace Deathborn.Client.Rendering;

/// <summary>Tilled soil + crop sprites on homestead yards.</summary>
public static class FarmRenderer
{
    private static Texture2D? _dry;
    private static Texture2D? _dryHole;
    private static Texture2D? _wet;
    private static Texture2D? _wetHole;

    public static void Load(ContentManager content)
    {
        _dry = Try(content, "Tiles/farm/tilled_dry");
        _dryHole = Try(content, "Tiles/farm/tilled_dry_hole");
        _wet = Try(content, "Tiles/farm/tilled_wet");
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
        foreach (var tile in house.Crops)
        {
            var origin = FarmCatalog.TileOrigin(tile.Tx, tile.Ty);
            var screen = WorldToScreen(origin, camera, screenCenter, zoom);
            var dest = new Rectangle(
                (int)MathF.Floor(screen.X),
                (int)MathF.Floor(screen.Y),
                Math.Max(1, (int)MathF.Ceiling(FarmCatalog.TileSize * zoom)),
                Math.Max(1, (int)MathF.Ceiling(FarmCatalog.TileSize * zoom)));
            var hole = !string.IsNullOrEmpty(tile.Crop) && tile.Stage <= 0;
            var tex = tile.Watered
                ? (hole ? _wetHole : _wet)
                : (hole ? _dryHole : _dry);
            if (tex != null)
                sb.Draw(tex, dest, Color.White);
            else
                DrawPrimitives.FillRect(sb, dest, tile.Watered
                    ? new Color(0.45f, 0.42f, 0.62f, 0.95f)
                    : new Color(0.55f, 0.38f, 0.22f, 0.95f));
        }
    }

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
