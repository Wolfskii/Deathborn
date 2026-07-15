using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Farm RPG multi-elevation terrain (16 px modular grass, cliffs, shadows).
/// </summary>
public static class FarmRpgTerrain
{
    public const int TilePixelSize = FarmRpgTileCells.Size;

    private const int CliffLipRow = 17;
    private const int CliffBaseRow = 18;

    private static Texture2D?[] _grassSheets = new Texture2D[5];
    private static Texture2D? _shadowSheet;

    public static bool IsLoaded => _grassSheets[0] != null;

    public static void Load(ContentManager content)
    {
        for (var i = 0; i < 5; i++)
            _grassSheets[i] = content.Load<Texture2D>($"Tiles/FarmRpg/grass_elev{i}");

        _shadowSheet = content.Load<Texture2D>("Tiles/FarmRpg/shadow");
    }

    public static void Draw(SpriteBatch sb, WorldMap map, VisibleTileRegion region)
    {
        if (!IsLoaded || !map.HasElevation) return;

        var tilePx = map.TileSize * region.Zoom;
        if (tilePx < 0.5f) return;

        TerrainTileBuckets.Clear();
        region.ForEachTile((tx, ty) =>
        {
            var elev = map.GetElevation(tx, ty);
            if (elev < 0 || elev > map.MaxElevation) return;
            TerrainTileBuckets.Add(elev, tx, ty);
        });

        var maxElev = map.MaxElevation;
        for (var elev = 0; elev <= maxElev; elev++)
        {
            var tiles = TerrainTileBuckets.ForElevation(elev);
            if (tiles.Count == 0) continue;

            if (elev > 0)
                DrawShadowPass(sb, map, region, elev, tiles);

            for (var i = 0; i < tiles.Count; i++)
            {
                TerrainTileBuckets.Unpack(tiles[i], out var tx, out var ty);
                if (map.IsRampTop(tx, ty) || map.IsRampLanding(tx, ty)) continue;
                DrawGroundTop(sb, map, tx, ty, region.Rect(tx, ty), elev);
            }

            for (var i = 0; i < tiles.Count; i++)
            {
                TerrainTileBuckets.Unpack(tiles[i], out var tx, out var ty);
                DrawCliffBaseBelow(sb, map, tx, ty, elev, region);
            }
        }

        DrawRamps(sb, map, region);
    }

    private static void DrawRamps(SpriteBatch sb, WorldMap map, VisibleTileRegion region)
    {
        for (var ty = region.MinTy; ty <= region.MaxTy; ty++)
        for (var tx = region.MinTx; tx <= region.MaxTx; tx++)
        {
            var ramp = map.GetRamp(tx, ty);
            if (ramp == 0) continue;

            var landingElev = map.GetElevation(tx, ty);
            if (landingElev < 0) continue;

            var topTy = ty - 1;
            if (topTy >= 0)
            {
                DrawRampTopBackdrop(sb, map, tx, topTy, region);
                DrawGroundTop(sb, map, tx, topTy, region.Rect(tx, topTy), landingElev);
            }

            DrawGroundTop(sb, map, tx, ty, region.Rect(tx, ty), landingElev);
        }
    }

    private static void DrawRampTopBackdrop(
        SpriteBatch sb, WorldMap map, int tx, int topTy, VisibleTileRegion region)
    {
        var northTy = topTy - 1;
        if (northTy >= 0 && map.GetElevation(tx, northTy) < 0)
            WaterTiles.TryDraw(sb, map, tx, topTy, region.Rect(tx, topTy));
    }

    private static void DrawShadowPass(
        SpriteBatch sb, WorldMap map, VisibleTileRegion region, int elev, List<int> tiles)
    {
        if (_shadowSheet == null) return;

        for (var i = 0; i < tiles.Count; i++)
        {
            TerrainTileBuckets.Unpack(tiles[i], out var tx, out var ty);

            var south = map.GetElevation(tx, ty + 1);
            var south2 = map.GetElevation(tx, ty + 2);
            if (south < 0 || south >= elev || south2 < 0)
                continue;

            var shadowTy = ty + 1;
            if (shadowTy >= map.TileHeight) continue;

            var dest = ExpandDest(region.Rect(tx, shadowTy), 1.4f);
            var src = FarmRpgTileCells.CellRect(new Point(0, 0));
            sb.Draw(_shadowSheet, dest, src, Color.White * 0.65f);
        }
    }

    private static void DrawGroundTop(SpriteBatch sb, WorldMap map, int tx, int ty, Rectangle dest, int elev)
    {
        var grass = _grassSheets[Math.Clamp(elev, 0, 4)];
        if (grass == null) return;

        var south = map.GetElevation(tx, ty + 1);
        var north = ty > 0 ? map.GetElevation(tx, ty - 1) : elev;

        // Row below a higher lip — cliff-base pass already drew the face.
        if (north > elev)
            return;

        if (elev > 0 && south < elev && south >= 0)
        {
            DrawCliffLip(sb, grass, map, tx, ty, dest);
            return;
        }

        var src = FarmRpgTileCells.GrassSrc(15);
        sb.Draw(grass, ScaleDest(dest), src, Color.White);
    }

    private static void DrawCliffLip(SpriteBatch sb, Texture2D grass, WorldMap map, int tx, int ty, Rectangle dest)
    {
        var col = CliffColumn(map, tx, ty);
        var src = new Rectangle(col * TilePixelSize, CliffLipRow * TilePixelSize, TilePixelSize, TilePixelSize);
        sb.Draw(grass, ScaleDest(dest), src, Color.White);
    }

    private static void DrawCliffBaseBelow(
        SpriteBatch sb, WorldMap map, int tx, int ty, int lipElev, VisibleTileRegion region)
    {
        if (IsCliffBaseRow(map, tx, ty, lipElev))
            return;

        var south = map.GetElevation(tx, ty + 1);
        if (south >= lipElev) return;

        var baseTy = ty + 1;
        if (baseTy >= map.TileHeight) return;
        if (map.IsRampLanding(tx, baseTy) || map.IsRampTop(tx, baseTy)) return;

        var grass = _grassSheets[Math.Clamp(lipElev, 0, 4)];
        if (grass == null) return;

        var col = CliffColumn(map, tx, ty);
        var row = south < 0 ? CliffBaseRow + 1 : CliffBaseRow;
        var src = new Rectangle(col * TilePixelSize, row * TilePixelSize, TilePixelSize, TilePixelSize);
        sb.Draw(grass, ScaleDest(region.Rect(tx, baseTy)), src, Color.White);
    }

    private static int CliffColumn(WorldMap map, int tx, int ty)
    {
        var westDrop = HasSouthDrop(map, tx - 1, ty);
        var eastDrop = HasSouthDrop(map, tx + 1, ty);

        if (!westDrop && !eastDrop) return 17;
        if (!westDrop) return 16;
        if (!eastDrop) return 19;
        return 18;
    }

    private static bool HasSouthDrop(WorldMap map, int tx, int ty)
    {
        if (tx < 0 || tx >= map.TileWidth || ty < 0 || ty >= map.TileHeight)
            return false;
        var e = map.GetElevation(tx, ty);
        return e > 0 && map.GetElevation(tx, ty + 1) < e;
    }

    private static bool IsCliffBaseRow(WorldMap map, int tx, int ty, int elev)
    {
        if (ty <= 0) return false;
        return map.GetElevation(tx, ty - 1) > elev;
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
