using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Tiny Swords multi-elevation terrain (flat ground, cliffs, shadows, stairs).
/// Rules: .tile_debug/tinyswords_guide.json
/// </summary>
public static class TinySwordsTerrain
{
    public const int TilePixelSize = 64;

    private static Texture2D?[] _colorSheets = new Texture2D[5];
    private static Texture2D? _shadowSheet;

    // Mask index 0–15 (NESW) → flat-ground piece id 1–16.
    private static readonly int[] MaskToPieceId =
    [
        16, 12, 13, 3,
        10, 11, 1, 2,
        15, 9, 14, 6,
        7, 8, 4, 5,
    ];

    public static bool IsLoaded => _colorSheets[0] != null;

    public static void Load(ContentManager content)
    {
        _colorSheets[0] = content.Load<Texture2D>("Tiles/tiny_swords_land_tileset");
        _colorSheets[1] = content.Load<Texture2D>("Tiles/tiny_swords_land_color2");
        _colorSheets[2] = content.Load<Texture2D>("Tiles/tiny_swords_land_color3");
        _colorSheets[3] = content.Load<Texture2D>("Tiles/tiny_swords_land_color4");
        _colorSheets[4] = content.Load<Texture2D>("Tiles/tiny_swords_land_color5");
        _shadowSheet = content.Load<Texture2D>("Tiles/tiny_swords_shadow");
    }

    public static void Draw(SpriteBatch sb, WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (!IsLoaded || !map.HasElevation) return;

        var tilePx = map.TileSize * zoom;
        if (tilePx < 0.5f) return;

        var halfViewW = screenCenter.X / zoom + map.TileSize * 3;
        var halfViewH = screenCenter.Y / zoom + map.TileSize * 3;
        var minTx = Math.Clamp((int)((camera.X - halfViewW) / map.TileSize), 0, map.TileWidth - 1);
        var maxTx = Math.Clamp((int)((camera.X + halfViewW) / map.TileSize), 0, map.TileWidth - 1);
        var minTy = Math.Clamp((int)((camera.Y - halfViewH) / map.TileSize), 0, map.TileHeight - 1);
        var maxTy = Math.Clamp((int)((camera.Y + halfViewH) / map.TileSize), 0, map.TileHeight - 1);

        var maxElev = map.MaxElevation;
        for (var elev = 0; elev <= maxElev; elev++)
        {
            if (elev > 0)
                DrawShadowPass(sb, map, minTx, maxTx, minTy, maxTy, elev, camera, screenCenter, zoom);

            for (var ty = minTy; ty <= maxTy; ty++)
            for (var tx = minTx; tx <= maxTx; tx++)
            {
                if (map.GetElevation(tx, ty) != elev) continue;
                if (map.IsRampTop(tx, ty)) continue;
                if (map.IsRampLanding(tx, ty)) continue;
                var rect = WorldMap.GetTileScreenRect(tx, ty, camera, screenCenter, zoom, map.TileSize);
                DrawGroundTop(sb, map, tx, ty, rect, elev);
            }

            for (var ty = minTy; ty <= maxTy; ty++)
            for (var tx = minTx; tx <= maxTx; tx++)
            {
                if (map.GetElevation(tx, ty) != elev) continue;
                DrawCliffBaseBelow(sb, map, tx, ty, elev, camera, screenCenter, zoom);
            }
        }

        DrawRamps(sb, map, minTx, maxTx, minTy, maxTy, camera, screenCenter, zoom);
    }

    private static void DrawRamps(
        SpriteBatch sb, WorldMap map, int minTx, int maxTx, int minTy, int maxTy,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            var ramp = map.GetRamp(tx, ty);
            if (ramp == 0) continue;

            var landingElev = map.GetElevation(tx, ty);
            if (landingElev < 0) continue;

            var sheet = _colorSheets[Math.Clamp(landingElev, 0, 4)];
            if (sheet == null) continue;

            var topTy = ty - 1;
            if (topTy >= 0)
            {
                if (ty >= 2 && map.GetElevation(tx, ty - 2) == landingElev + 1)
                {
                    var backdropRect = WorldMap.GetTileScreenRect(tx, topTy, camera, screenCenter, zoom, map.TileSize);
                    DrawPiece(sb, sheet, 18, elevated: true, backdropRect);
                }

                var topRect = WorldMap.GetTileScreenRect(tx, topTy, camera, screenCenter, zoom, map.TileSize);
                DrawPiece(sb, sheet, ramp == 1 ? 25 : 28, elevated: false, topRect);
            }

            var bottomRect = WorldMap.GetTileScreenRect(tx, ty, camera, screenCenter, zoom, map.TileSize);
            DrawPiece(sb, sheet, ramp == 1 ? 29 : 32, elevated: false, bottomRect);
        }
    }

    private static void DrawShadowPass(
        SpriteBatch sb, WorldMap map, int minTx, int maxTx, int minTy, int maxTy, int elev,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (_shadowSheet == null) return;

        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            if (map.GetElevation(tx, ty) != elev) continue;

            var south = map.GetElevation(tx, ty + 1);
            var south2 = map.GetElevation(tx, ty + 2);
            if (south < 0 || south >= elev || south2 < 0)
                continue;

            var shadowTy = ty + 1;
            if (shadowTy >= map.TileHeight) continue;

            var landRect = WorldMap.GetTileScreenRect(tx, shadowTy, camera, screenCenter, zoom, map.TileSize);
            var dest = ExpandDest(landRect, 2f);
            var src = new Rectangle(32, 32, 128, 128);
            sb.Draw(_shadowSheet, dest, src, Color.White * 0.85f);
        }
    }

    private static void DrawGroundTop(SpriteBatch sb, WorldMap map, int tx, int ty, Rectangle dest, int elev)
    {
        var sheet = _colorSheets[Math.Clamp(elev, 0, 4)];
        if (sheet == null) return;

        var mask = NeighborMask(map, tx, ty, elev);
        var pieceId = MaskToPieceId[mask];
        var south = map.GetElevation(tx, ty + 1);
        var north = map.GetElevation(tx, ty - 1);

        if (IsRampPlatformCorner(map, tx, ty))
            pieceId = 5;
        else if (elev > 0 && south < elev && !IsCliffBaseRow(map, tx, ty, elev))
            pieceId = ResolveCliffLip(pieceId, north);

        var elevatedRegion = elev > 0;
        DrawPiece(sb, sheet, pieceId, elevatedRegion, dest);
    }

    private static bool IsRampPlatformCorner(WorldMap map, int tx, int ty)
    {
        if (map.GetRamp(tx - 1, ty + 1) == 1 && map.GetElevation(tx, ty) == map.GetElevation(tx - 1, ty + 1) + 1)
            return true;
        if (map.GetRamp(tx + 1, ty + 1) == 2 && map.GetElevation(tx, ty) == map.GetElevation(tx + 1, ty + 1) + 1)
            return true;
        return false;
    }

    private static void DrawCliffBaseBelow(
        SpriteBatch sb, WorldMap map, int tx, int ty, int lipElev,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (IsCliffBaseRow(map, tx, ty, lipElev))
            return;

        var south = map.GetElevation(tx, ty + 1);
        if (south >= lipElev) return;

        var baseTy = ty + 1;
        if (baseTy >= map.TileHeight) return;
        if (map.IsRampLanding(tx, baseTy) || map.IsRampTop(tx, baseTy)) return;

        var onWater = south < 0;
        var westDrop = HasSouthDrop(map, tx - 1, ty);
        var eastDrop = HasSouthDrop(map, tx + 1, ty);

        int pieceId;
        if (!westDrop && !eastDrop)
            pieceId = onWater ? 24 : 20;
        else if (!westDrop)
            pieceId = onWater ? 21 : 17;
        else if (!eastDrop)
            pieceId = onWater ? 23 : 19;
        else
            pieceId = onWater ? 22 : 18;

        if (map.GetRamp(tx - 1, baseTy) == 1 && pieceId == 17)
            pieceId = 18;
        if (map.GetRamp(tx + 1, baseTy) == 2 && pieceId == 19)
            pieceId = 18;

        var sheet = _colorSheets[Math.Clamp(lipElev, 0, 4)];
        if (sheet == null) return;

        var rect = WorldMap.GetTileScreenRect(tx, baseTy, camera, screenCenter, zoom, map.TileSize);
        DrawPiece(sb, sheet, pieceId, elevated: true, rect);
    }

    private static void DrawPiece(SpriteBatch sb, Texture2D sheet, int pieceId, bool elevated, Rectangle dest)
    {
        var cell = PieceCell(pieceId, elevated);
        var src = new Rectangle(cell.X * TilePixelSize, cell.Y * TilePixelSize, TilePixelSize, TilePixelSize);
        sb.Draw(sheet, ScaleDest(dest), src, Color.White);
    }

    private static bool HasSouthDrop(WorldMap map, int tx, int ty)
    {
        if (tx < 0 || tx >= map.TileWidth || ty < 0 || ty >= map.TileHeight)
            return false;
        var e = map.GetElevation(tx, ty);
        return e > 0 && map.GetElevation(tx, ty + 1) < e;
    }

    /// <summary>Row immediately below a higher plateau lip — already the cliff-face row.</summary>
    private static bool IsCliffBaseRow(WorldMap map, int tx, int ty, int elev)
    {
        if (ty <= 0) return false;
        return map.GetElevation(tx, ty - 1) > elev;
    }

    private static int ResolveCliffLip(int pieceId, int northElev)
    {
        if (northElev >= 0) return pieceId;
        return pieceId switch
        {
            7 => 10,
            8 => 11,
            9 => 12,
            15 => 16,
            _ => pieceId,
        };
    }

    private static int NeighborMask(WorldMap map, int tx, int ty, int elev)
    {
        var mask = 0;
        if (map.GetElevation(tx, ty - 1) >= elev) mask |= 8;
        if (map.GetElevation(tx + 1, ty) >= elev) mask |= 4;
        if (map.GetElevation(tx, ty + 1) >= elev) mask |= 2;
        if (map.GetElevation(tx - 1, ty) >= elev) mask |= 1;
        return mask;
    }

    private static Point PieceCell(int pieceId, bool elevated)
    {
        if (pieceId is 25 or 28 or 29 or 32)
        {
            return pieceId switch
            {
                25 => new Point(0, 4),
                28 => new Point(3, 4),
                29 => new Point(0, 5),
                32 => new Point(3, 5),
                _ => new Point(0, 4),
            };
        }

        if (!elevated)
        {
            if (pieceId is >= 13 and <= 16)
                return new Point(3, pieceId - 13);
            pieceId--;
            return new Point(pieceId % 3, pieceId / 3);
        }

        if (pieceId is >= 21 and <= 24)
            return new Point(pieceId - 21 + 5, 5);
        if (pieceId is >= 17 and <= 20)
            return new Point(pieceId - 17 + 5, 4);
        if (pieceId is >= 10 and <= 12)
            return new Point(pieceId - 10 + 5, 3);
        if (pieceId is >= 13 and <= 16)
            return new Point(8, pieceId - 13);

        pieceId--;
        return new Point(5 + pieceId % 3, pieceId / 3);
    }

    private static Rectangle ExpandDest(Rectangle dest, float tileSpan)
    {
        var scale = DungeonFloorTiles.VisualScale;
        var cx = dest.X + dest.Width * 0.5f;
        var cy = dest.Y + dest.Height * 0.5f;
        var w = Math.Max(1, (int)MathF.Ceiling(dest.Width * tileSpan * scale));
        var h = Math.Max(1, (int)MathF.Ceiling(dest.Height * tileSpan * scale));
        return new Rectangle((int)(cx - w * 0.5f), (int)(cy - h * 0.5f), w, h);
    }

    private static Rectangle ScaleDest(Rectangle dest)
    {
        var scale = DungeonFloorTiles.VisualScale;
        if (MathF.Abs(scale - 1f) < 0.001f) return dest;
        var cx = dest.X + dest.Width * 0.5f;
        var cy = dest.Y + dest.Height * 0.5f;
        var w = Math.Max(1, (int)MathF.Ceiling(dest.Width * scale));
        var h = Math.Max(1, (int)MathF.Ceiling(dest.Height * scale));
        return new Rectangle((int)(cx - w * 0.5f), (int)(cy - h * 0.5f), w, h);
    }
}
