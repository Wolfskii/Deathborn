using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Occasional 16×16 grass/flower props from ALL props seasons.png on flat land tiles.
/// </summary>
public static class FarmRpgGrassProps
{
    private const uint Seed = 0x06A5500;
    private const int CellSize = 16;
    private const int SpawnRollMax = 1000;
    private const int SpawnChance = 130;

    private static Texture2D? _sheet;
    private static Point[][] _catalogByElev = [];

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        _sheet = content.Load<Texture2D>("Tiles/FarmRpg/props_seasons");
        _catalogByElev =
        [
            BuildRange(0, 1, 0, 14),
            BuildRange(0, 1, 0, 14),
            BuildRange(6, 7, 0, 11),
            BuildRange(2, 3, 0, 12),
            BuildRange(4, 5, 0, 11),
        ];
    }

    public static void Draw(SpriteBatch sb, WorldMap map, VisibleTileRegion region)
    {
        if (_sheet == null || !map.HasElevation || _catalogByElev.Length == 0) return;

        region.ForEachTile((tx, ty) =>
        {
            if (!ShouldPlace(map, tx, ty, out var elev)) return;
            if (Hash(tx, ty, 1) % SpawnRollMax >= SpawnChance) return;

            var catalog = _catalogByElev[Math.Clamp(elev, 0, _catalogByElev.Length - 1)];
            if (catalog.Length == 0) return;

            var pick = catalog[(int)(Hash(tx, ty, 2) % (uint)catalog.Length)];
            var src = new Rectangle(pick.X * CellSize, pick.Y * CellSize, CellSize, CellSize);
            sb.Draw(_sheet, ScaleDest(region.Rect(tx, ty)), src, Color.White);
        });
    }

    private static bool ShouldPlace(WorldMap map, int tx, int ty, out int elev)
    {
        elev = map.GetElevation(tx, ty);
        if (elev < 0 || !map.IsLand(tx, ty)) return false;
        if (map.IsRampTop(tx, ty) || map.IsRampLanding(tx, ty)) return false;

        var north = ty > 0 ? map.GetElevation(tx, ty - 1) : elev;
        if (north > elev) return false;

        var south = ty + 1 < map.TileHeight ? map.GetElevation(tx, ty + 1) : elev;
        if (elev > 0 && south < elev && south >= 0) return false;

        if (ty > 0 && map.GetElevation(tx, ty - 1) > elev) return false;

        var world = new Vector2((tx + 0.5f) * map.TileSize, (ty + 0.5f) * map.TileSize);
        if (NearSpawn(map, world) || InTown(world)) return false;

        return true;
    }

    private static Point[] BuildRange(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        var count = (rowEnd - rowStart + 1) * (colEnd - colStart + 1);
        var props = new Point[count];
        var i = 0;
        for (var row = rowStart; row <= rowEnd; row++)
        for (var col = colStart; col <= colEnd; col++)
            props[i++] = new Point(col, row);
        return props;
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

    private static bool NearSpawn(WorldMap map, Vector2 pos)
    {
        const float radius = 160f;
        return Vector2.DistanceSquared(pos, map.DefaultSpawn) < radius * radius;
    }

    private static bool InTown(Vector2 world)
    {
        const float pad = 56f;
        foreach (var zone in WorldZones.Towns)
        {
            if (world.X >= zone.Center.X - zone.HalfWidth - pad
                && world.X <= zone.Center.X + zone.HalfWidth + pad
                && world.Y >= zone.Center.Y - zone.HalfHeight - pad
                && world.Y <= zone.Center.Y + zone.HalfHeight + pad)
                return true;
        }
        return false;
    }

    private static uint Hash(int tx, int ty, int salt)
    {
        var h = Seed ^ (uint)(tx * 73856093) ^ (uint)(ty * 19349663) ^ (uint)(salt * 83492791);
        h ^= h >> 16;
        h *= 0x85EBCA6B;
        h ^= h >> 13;
        h *= 0xC2B2AE35;
        h ^= h >> 16;
        return h;
    }
}
