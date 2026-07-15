using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Farm RPG 16 px tile coordinates discovered from the vendor sheets.
/// </summary>
internal static class FarmRpgTileCells
{
    public const int Size = 16;

    public static readonly Point PlainGrass = new(9, 2);

    public static Rectangle GrassSrc(int mask) => CellRect(PlainGrass);

    /// <summary>Shore land uses plain grass; water-side fringe is drawn on water cells.</summary>
    public static Rectangle GrassWaterLandSrc(int landMask) => CellRect(PlainGrass);

    /// <summary>Water cell with land to the north — foam band at top of tile.</summary>
    public static readonly Point WaterShoreNorth = new(25, 2);

    /// <summary>Water cell with land to the south.</summary>
    public static readonly Point WaterShoreSouth = new(3, 9);

    /// <summary>Water cell with land to the east.</summary>
    public static readonly Point WaterShoreEast = new(10, 8);

    /// <summary>Water cell with land to the west.</summary>
    public static readonly Point WaterShoreWest = new(8, 8);

    public static Rectangle WaterShoreSrc(int landMask)
    {
        // landMask bits = land neighbours (N=8,E=4,S=2,W=1); pick dominant edge tile.
        if ((landMask & 8) != 0) return CellRect(WaterShoreNorth);
        if ((landMask & 2) != 0) return CellRect(WaterShoreSouth);
        if ((landMask & 4) != 0) return CellRect(WaterShoreEast);
        if ((landMask & 1) != 0) return CellRect(WaterShoreWest);
        return CellRect(WaterShoreSouth);
    }

    public static Rectangle CellRect(Point cell) =>
        new(cell.X * Size, cell.Y * Size, Size, Size);
}
