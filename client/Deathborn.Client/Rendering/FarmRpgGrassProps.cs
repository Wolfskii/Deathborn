using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Occasional 16×16 grass tuft overlays from ALL props seasons.png on flat land tiles.
/// Bright grass props match spring/summer ground (elev 0–2). Dark/teal props are reserved for deep forest later.
/// </summary>
public static class FarmRpgGrassProps
{
    private const uint Seed = 0x06A5500;
    private const int CellSize = 16;
    private const int SpawnRollMax = 1000;
    private const int SoloSpawnChance = 55;
    private const int PatchSpawnChanceMin = 340;
    private const int PatchSpawnChanceMax = 820;
    private const int PatchMacroSize = 8;
    private const int PatchMacroChance = 300;

    private static Texture2D? _sheet;
    private static Point[][] _catalogByElev = [];

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        _sheet = content.Load<Texture2D>("Tiles/FarmRpg/props_seasons");
        _catalogByElev =
        [
            BrightGrassProps,
            BrightGrassProps,
            BrightGrassProps,
            [],
            DarkGrassProps,
        ];
    }

    // Bright spring/summer rows only — no teal tufts (row 0 cols 5–7) or sunflower tiles (row 1 col 6).
    private static readonly Point[] BrightGrassProps =
    [
        new(1, 0), new(1, 1), new(5, 1),
    ];

    // Reserved for deep-forest / dark grass terrain (teal rows) — wired up later.
    private static readonly Point[] DarkGrassProps = [];

    public static void Draw(SpriteBatch sb, WorldMap map, VisibleTileRegion region)
    {
        if (_sheet == null || !map.HasElevation || _catalogByElev.Length == 0) return;

        region.ForEachTile((tx, ty) =>
        {
            if (IsInsideAnyHousePlot(map, tx, ty)) return;
            if (!ShouldPlace(map, tx, ty, out var elev)) return;
            if (!ShouldSpawn(tx, ty)) return;

            var catalog = _catalogByElev[Math.Clamp(elev, 0, _catalogByElev.Length - 1)];
            if (catalog.Length == 0) return;

            var pick = catalog[(int)(Hash(tx, ty, 2) % (uint)catalog.Length)];
            var src = new Rectangle(pick.X * CellSize, pick.Y * CellSize, CellSize, CellSize);
            sb.Draw(_sheet, ScaleDest(region.Rect(tx, ty)), src, Color.White);
        });
    }

    /// <summary>True when the plot would overlap procedural grass tufts (no collider, but blocks homestead).</summary>
    public static bool PlotHasTufts(WorldMap map, Vector2 center, float halfW, float halfH)
    {
        if (_sheet == null || !map.HasElevation || _catalogByElev.Length == 0) return false;

        var tile = map.TileSize;
        var tx0 = Math.Max(0, (int)MathF.Floor((center.X - halfW) / tile));
        var ty0 = Math.Max(0, (int)MathF.Floor((center.Y - halfH) / tile));
        var tx1 = Math.Min(map.TileWidth - 1, (int)MathF.Floor((center.X + halfW) / tile));
        var ty1 = Math.Min(map.TileHeight - 1, (int)MathF.Floor((center.Y + halfH) / tile));

        for (var ty = ty0; ty <= ty1; ty++)
        for (var tx = tx0; tx <= tx1; tx++)
        {
            if (!ShouldPlace(map, tx, ty, out var elev)) continue;
            if (!ShouldSpawn(tx, ty)) continue;
            var catalog = _catalogByElev[Math.Clamp(elev, 0, _catalogByElev.Length - 1)];
            if (catalog.Length > 0) return true;
        }
        return false;
    }

    private static bool IsInsideAnyHousePlot(WorldMap map, int tx, int ty)
    {
        var world = new Vector2((tx + 0.5f) * map.TileSize, (ty + 0.5f) * map.TileSize);
        foreach (var house in WorldZones.Houses)
        {
            if (HousingConstants.InPlot(world, house.Center))
                return true;
        }
        return false;
    }

    private static bool ShouldSpawn(int tx, int ty)
    {
        var patch = PatchInfluence(tx, ty);
        var threshold = patch <= 0f
            ? SoloSpawnChance
            : (int)(PatchSpawnChanceMin + patch * (PatchSpawnChanceMax - PatchSpawnChanceMin));
        return Hash(tx, ty, 1) % SpawnRollMax < (uint)threshold;
    }

    /// <summary>
    /// 0 outside patches; approaches 1 near patch centers for clustered grass tufts.
    /// </summary>
    private static float PatchInfluence(int tx, int ty)
    {
        var mx = FloorDiv(tx, PatchMacroSize);
        var my = FloorDiv(ty, PatchMacroSize);
        if (Hash(mx, my, 60) % SpawnRollMax >= PatchMacroChance)
            return 0f;

        var patchCount = 1 + (int)(Hash(mx, my, 61) % 2);
        var best = 0f;
        for (var i = 0; i < patchCount; i++)
        {
            var cx = mx * PatchMacroSize + (int)(Hash(mx, my, 62 + i * 4) % (uint)PatchMacroSize);
            var cy = my * PatchMacroSize + (int)(Hash(mx, my, 63 + i * 4) % (uint)PatchMacroSize);
            var radius = 2.2f + (Hash(mx, my, 64 + i * 4) % 1000) / 1000f * 2.8f;

            var dx = tx + 0.5f - cx;
            var dy = ty + 0.5f - cy;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist >= radius) continue;

            var influence = 1f - dist / radius;
            best = MathF.Max(best, influence);
        }

        return best;
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (value >= 0) return value / divisor;
        return (value - divisor + 1) / divisor;
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

        var world = new Vector2((tx + 0.5f) * map.TileSize, (ty + 0.5f) * map.TileSize);
        if (NearSpawn(map, world) || InTown(world)) return false;

        return true;
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
