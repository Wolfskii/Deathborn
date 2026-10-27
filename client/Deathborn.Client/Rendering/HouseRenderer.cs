using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Draws player homestead houses and interior furniture.</summary>
public static class HouseRenderer
{
    private static readonly Color Plaster = new(0.88f, 0.82f, 0.72f);
    private static readonly Color PlasterDark = new(0.72f, 0.66f, 0.58f);
    private static readonly Color Roof = new(0.55f, 0.28f, 0.22f);
    private static readonly Color RoofLight = new(0.68f, 0.38f, 0.28f);
    private static readonly Color Door = new(0.35f, 0.22f, 0.14f);

    public static void Draw(
        SpriteBatch sb,
        Vector2 camera,
        Vector2 screenCenter,
        float positionZoom,
        float visualZoom,
        IEnumerable<HousePlotZone> houses)
    {
        // Ground layer only. Fence cells and the cottage are drawn in the exterior Y-sort with the
        // players (DrawCottage / FarmRpgFenceSprites.AppendCells), so they hide whoever walks
        // behind them. Furniture positions are interior coordinates (interior view only).
        foreach (var house in houses)
        {
            FarmRenderer.DrawSoil(sb, house, camera, screenCenter, positionZoom);
            FarmRenderer.DrawCrops(sb, house, camera, screenCenter, positionZoom);
        }
        FarmRenderer.DrawWaterSplashes(sb, camera, screenCenter, positionZoom);
    }

    /// <summary>
    /// Depth-sort Y for the cottage. A player's sort Y is about 14 units above their feet
    /// (PlayerEntity.SortYFromFeet), and feet stop 8 units below the cottage base at the door, so
    /// the base minus 12 keeps a player at the door in front (opaque) and only players north of the
    /// house behind it (see-through).
    /// </summary>
    public static float CottageSortY(Vector2 center) => center.Y - 12f;

    /// <summary>Cottage sprite; <paramref name="alpha"/> below 1 fades it while the player is behind it.</summary>
    public static void DrawCottage(
        SpriteBatch sb, Vector2 center, Vector2 camera, Vector2 screenCenter,
        float positionZoom, float visualZoom, float alpha) =>
        DrawHouseStructure(sb, center, camera, screenCenter, positionZoom, visualZoom, alpha);

    public static void DrawInteriorFloors(
        SpriteBatch sb,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        IEnumerable<HousePlotZone> houses,
        IEnumerable<PlayerEntity> players)
    {
        var occupied = new HashSet<long>();
        foreach (var p in players)
        {
            if (p.InsideHouseId > 0)
                occupied.Add(p.InsideHouseId);
        }

        foreach (var house in houses)
        {
            if (!occupied.Contains(house.Id)) continue;
            var c = house.Center;
            var tl = WorldToScreen(c + new Vector2(-HousingConstants.HouseHalfW, -HousingConstants.HouseHalfH - 12), camera, screenCenter, zoom);
            var br = WorldToScreen(c + new Vector2(HousingConstants.HouseHalfW, HousingConstants.HouseHalfH - 28), camera, screenCenter, zoom);
            var interior = new Rectangle((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));
            DrawPrimitives.FillRect(sb, interior, new Color(0.42f, 0.36f, 0.28f, 0.92f));
            DrawBorder(sb, interior, new Color(0.28f, 0.22f, 0.16f), Math.Max(1, (int)(2 * zoom)));
        }
    }

    /// <summary>
    /// RTS placement ghost: per-tile green/red overlay under the plot + tinted cottage.
    /// </summary>
    public static void DrawPlacementGhost(
        SpriteBatch sb,
        Vector2 worldCenter,
        Vector2 camera,
        Vector2 screenCenter,
        float positionZoom,
        float visualZoom,
        bool canPlace)
    {
        const float tileFillA = 0.42f;
        const float edgeA = 0.95f;
        var okFill = new Color(0.15f, 0.95f, 0.35f) * tileFillA;
        var badFill = new Color(0.95f, 0.18f, 0.18f) * tileFillA;
        var edge = canPlace
            ? new Color(0.15f, 1f, 0.35f) * edgeA
            : new Color(1f, 0.25f, 0.2f) * edgeA;
        var cottageTint = canPlace
            ? new Color(0.35f, 1f, 0.45f) * 0.7f
            : new Color(1f, 0.3f, 0.3f) * 0.7f;

        DrawPlotTileOverlay(sb, worldCenter, camera, screenCenter, positionZoom, okFill, badFill);

        var plotTl = WorldToScreen(
            worldCenter + new Vector2(-HousingConstants.PlotHalfW, -HousingConstants.PlotHalfH),
            camera, screenCenter, positionZoom);
        var plotBr = WorldToScreen(
            worldCenter + new Vector2(HousingConstants.PlotHalfW, HousingConstants.PlotHalfH),
            camera, screenCenter, positionZoom);
        var plotRect = new Rectangle(
            (int)MathF.Floor(plotTl.X),
            (int)MathF.Floor(plotTl.Y),
            Math.Max(1, (int)MathF.Ceiling(plotBr.X - plotTl.X)),
            Math.Max(1, (int)MathF.Ceiling(plotBr.Y - plotTl.Y)));
        DrawPrimitives.DrawRectOutline(sb, plotRect, edge, Math.Max(2f, 3f * visualZoom));

        var screen = WorldToScreen(worldCenter, camera, screenCenter, positionZoom);
        var tex = FarmRpgHouseSprites.OrangeCottage;
        if (tex != null)
        {
            var scale = FarmRpgHouseSprites.DisplayScale * visualZoom;
            var origin = FarmRpgHouseSprites.FootAnchor();
            sb.Draw(tex, screen, null, cottageTint, 0f, origin, scale, SpriteEffects.None, 0f);

            var w = tex.Width * scale;
            var h = tex.Height * scale;
            var rect = new Rectangle(
                (int)(screen.X - origin.X * scale),
                (int)(screen.Y - origin.Y * scale),
                (int)MathF.Ceiling(w),
                (int)MathF.Ceiling(h));
            DrawPrimitives.DrawRectOutline(sb, rect, edge, Math.Max(2f, 2.5f * visualZoom));
            return;
        }

        HousingCollision.BodyBounds(worldCenter, out var left, out var right, out var top, out var bottom);
        var tl = WorldToScreen(new Vector2(left, top), camera, screenCenter, positionZoom);
        var br = WorldToScreen(new Vector2(right, bottom), camera, screenCenter, positionZoom);
        var fallback = new Rectangle(
            (int)tl.X, (int)tl.Y,
            Math.Max(1, (int)(br.X - tl.X)),
            Math.Max(1, (int)(br.Y - tl.Y)));
        DrawPrimitives.FillRect(sb, fallback, cottageTint);
        DrawPrimitives.DrawRectOutline(sb, fallback, edge, Math.Max(2f, 2.5f * visualZoom));
    }

    /// <summary>Draw translucent per-tile cells for the homestead plot (green walkable / red blocked).</summary>
    private static void DrawPlotTileOverlay(
        SpriteBatch sb,
        Vector2 worldCenter,
        Vector2 camera,
        Vector2 screenCenter,
        float positionZoom,
        Color okFill,
        Color badFill)
    {
        var map = WorldMap.SwaroviaMainland;
        if (map == null) return;

        var tile = map.TileSize;
        var minX = worldCenter.X - HousingConstants.PlotHalfW;
        var maxX = worldCenter.X + HousingConstants.PlotHalfW;
        var minY = worldCenter.Y - HousingConstants.PlotHalfH;
        var maxY = worldCenter.Y + HousingConstants.PlotHalfH;
        var tx0 = (int)MathF.Floor(minX / tile);
        var ty0 = (int)MathF.Floor(minY / tile);
        var tx1 = (int)MathF.Floor((maxX - 0.001f) / tile);
        var ty1 = (int)MathF.Floor((maxY - 0.001f) / tile);

        for (var ty = ty0; ty <= ty1; ty++)
        for (var tx = tx0; tx <= tx1; tx++)
        {
            var wx = (tx + 0.5f) * tile;
            var wy = (ty + 0.5f) * tile;
            var clear = map.IsWalkable(wx, wy, 4f);
            var tl = WorldToScreen(new Vector2(tx * tile, ty * tile), camera, screenCenter, positionZoom);
            var br = WorldToScreen(new Vector2((tx + 1) * tile, (ty + 1) * tile), camera, screenCenter, positionZoom);
            var rect = new Rectangle(
                (int)MathF.Floor(tl.X),
                (int)MathF.Floor(tl.Y),
                Math.Max(1, (int)MathF.Ceiling(br.X - tl.X)),
                Math.Max(1, (int)MathF.Ceiling(br.Y - tl.Y)));
            DrawPrimitives.FillRect(sb, rect, clear ? okFill : badFill);
        }
    }

    private static void DrawHouseStructure(
        SpriteBatch sb, Vector2 world,
        Vector2 camera, Vector2 screenCenter, float positionZoom, float visualZoom, float alpha = 1f)
    {
        var screen = WorldToScreen(world, camera, screenCenter, positionZoom);
        var tex = FarmRpgHouseSprites.OrangeCottage;
        if (tex != null)
        {
            var scale = FarmRpgHouseSprites.DisplayScale * visualZoom;
            sb.Draw(tex, screen, null, Color.White * alpha, 0f, FarmRpgHouseSprites.FootAnchor(), scale, SpriteEffects.None, 0f);
            return;
        }

        // Procedural fallback if Farm RPG cottage content is missing.
        var w = HousingConstants.HouseHalfW * 2f;
        var h = HousingConstants.HouseHalfH * 2f;
        var body = new Rectangle(
            (int)(screen.X - w * 0.5f * visualZoom),
            (int)(screen.Y - h * 0.35f * visualZoom),
            (int)(w * visualZoom),
            (int)(h * 0.65f * visualZoom));
        DrawPrimitives.FillRect(sb, body, Plaster);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, body.Width, 2), PlasterDark);

        var doorW = (int)(14 * visualZoom);
        var doorH = (int)(22 * visualZoom);
        DrawPrimitives.FillRect(sb,
            new Rectangle(body.Center.X - doorW / 2, body.Bottom - doorH, doorW, doorH),
            Door);

        var roofH = h * 0.4f * visualZoom;
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

    public static void DrawFurnitureItem(
        SpriteBatch sb,
        FurnitureItemState item,
        Vector2 houseCenter,
        Vector2 camera,
        Vector2 screenCenter,
        float worldZoom,
        float zoomScale) =>
        DrawFurniture(sb, item.Type, item.Position, camera, screenCenter, worldZoom * zoomScale, Color.White);

    /// <summary>Furniture piece at a world position (bottom-centre anchor); <paramref name="tint"/> for previews.</summary>
    public static void DrawFurniture(
        SpriteBatch sb, string type, Vector2 world, Vector2 camera, Vector2 screenCenter, float z, Color tint)
    {
        var screen = screenCenter + (world - camera) * z;
        if (FurnitureSprites.TryDraw(sb, type, screen, z, Environment.TickCount64 / 1000f, tint))
            return;

        // Placeholder block when the sprite is not installed.
        var size = FurnitureSprites.Size(type);
        var rect = FurnitureSprites.IsFloorPiece(type)
            ? CenteredRect(screen, size.X * z, size.Y * z)
            : CenteredRect(screen - new Vector2(0, size.Y * 0.5f * z), size.X * z, size.Y * z);
        DrawPrimitives.FillRect(sb, rect, new Color(0.48f, 0.32f, 0.18f) * (tint.A / 255f));
    }

    private static Rectangle CenteredRect(Vector2 center, float w, float h) =>
        new((int)(center.X - w / 2), (int)(center.Y - h / 2), (int)w, (int)h);

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        screenCenter + (world - camera) * zoom;

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
