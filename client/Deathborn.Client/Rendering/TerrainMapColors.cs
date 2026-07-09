using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Flat fill colors for minimap / world-map overlays — one tone per elevation,
/// sampled from Tiny Swords water + Tilemap_color1…5 center grass.
/// </summary>
public static class TerrainMapColors
{
    /// <summary>Water Background color.png</summary>
    public static readonly Color Water = new(64, 168, 168);

    /// <summary>Tilemap_color{N} matches elevation N-1 (color1 = el 0, …).</summary>
    private static readonly Color[] LandByElevation =
    [
        new(152, 184, 72), // 0 — shoreline / flat coast
        new(128, 176, 80), // 1 — base land
        new(88, 168, 96),  // 2 — plateau
        new(128, 152, 88), // 3 — plateau
        new(80, 152, 136), // 4 — plateau
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
