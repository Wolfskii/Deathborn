using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Flat fill colors for minimap / world-map overlays — one tone per elevation,
/// sampled from Farm RPG spring/summer/fall/deep-forest grass sheets.
/// </summary>
public static class TerrainMapColors
{
    public static readonly Color Water = new(72, 152, 216);

    private static readonly Color[] LandByElevation =
    [
        new(120, 184, 72),  // 0 — shoreline / flat coast (spring)
        new(104, 176, 64),  // 1 — base land (spring)
        new(96, 168, 56),   // 2 — plateau (summer)
        new(112, 152, 48),  // 3 — plateau (fall)
        new(72, 136, 64),   // 4 — deep forest
    ];

    public static Color ForTile(WorldMap map, int tx, int ty)
    {
        if (!map.IsLand(tx, ty))
            return Water;

        var elev = map.GetElevation(tx, ty);
        if (elev < 0)
            return Water;
        if (elev >= LandByElevation.Length)
            return LandByElevation[^1];
        return LandByElevation[elev];
    }
}
