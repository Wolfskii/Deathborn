using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Homestead interior: 26 x 15 tiles of 16 world units, three rooms side by side
/// (bedroom | hall | kitchen). Mirrors server game/interior_layout.go.
/// Tile (0,0) is the top-left corner of the interior (centre - InteriorHalfW/H).
/// <code>
/// row 0        dark wall top along the north edge
/// rows 1-2     wall face (wallpaper); row 1 is solid, players may stand in front of row 2
/// rows 3-13    floor
/// row 14       south wall top, exit gap in columns 12-13
/// col 0 / 25   outer side walls
/// col 8 / 17   room dividers, open in rows 8-10 (doorways)
/// </code>
/// </summary>
public static class HouseInteriorLayout
{
    public const float Tile = 16f;
    public const int Cols = 26;
    public const int Rows = 15;
    public const int DividerL = 8;
    public const int DividerR = 17;
    public const int DoorwayTop = 8;
    public const int DoorwayBottom = 10;
    public const int ExitLeft = 12;
    public const int ExitRight = 13;
    public const int FirstFloorRow = 3;
    /// <summary>Wall top plus the upper wall-face row block movement (server interiorNorthSolidRows).</summary>
    public const int NorthSolidRows = 2;

    public const int Bedroom = 0;
    public const int Hall = 1;
    public const int Kitchen = 2;

    public static Vector2 Origin(Vector2 center) =>
        center - new Vector2(HousingConstants.InteriorHalfW, HousingConstants.InteriorHalfH);

    /// <summary>World position of the top-left corner of tile (col, row).</summary>
    public static Vector2 TileWorld(Vector2 center, int col, int row) =>
        Origin(center) + new Vector2(col * Tile, row * Tile);

    /// <summary>Dark wall top (outer walls, dividers, south wall except the exit gap).</summary>
    public static bool IsWallTop(int col, int row)
    {
        if (col < 0 || row < 0 || col >= Cols || row >= Rows) return true;
        if (row == 0 || col == 0 || col == Cols - 1) return true;
        if (row == Rows - 1) return col < ExitLeft || col > ExitRight;
        if (col is DividerL or DividerR) return row < DoorwayTop || row > DoorwayBottom;
        return false;
    }

    /// <summary>Wallpapered wall face below the north wall top.</summary>
    public static bool IsWallFace(int col, int row) =>
        row is 1 or 2 && !IsWallTop(col, row);

    public static bool IsFloor(int col, int row) => !IsWallTop(col, row) && !IsWallFace(col, row);

    /// <summary>Room for a column; divider doorways belong to the hall.</summary>
    public static int RoomAt(int col) =>
        col < DividerL ? Bedroom : col <= DividerR ? Hall : Kitchen;

    /// <summary>First and last floor column of a room.</summary>
    public static (int First, int Last) RoomColumns(int room) => room switch
    {
        Bedroom => (1, DividerL - 1),
        Hall => (DividerL + 1, DividerR - 1),
        _ => (DividerR + 1, Cols - 2),
    };

    /// <summary>Solid interior walls in world space (same rectangles as server interiorWallRects).</summary>
    public static void AppendWalls(Vector2 center, List<(float L, float R, float T, float B)> walls)
    {
        var o = Origin(center);
        float X(int c) => o.X + c * Tile;
        float Y(int r) => o.Y + r * Tile;

        walls.Add((X(0), X(Cols), Y(0), Y(NorthSolidRows)));
        walls.Add((X(0), X(1), Y(0), Y(Rows)));
        walls.Add((X(Cols - 1), X(Cols), Y(0), Y(Rows)));
        walls.Add((X(0), X(ExitLeft), Y(Rows - 1), Y(Rows)));
        walls.Add((X(ExitRight + 1), X(Cols), Y(Rows - 1), Y(Rows)));
        foreach (var div in new[] { DividerL, DividerR })
        {
            walls.Add((X(div), X(div + 1), Y(0), Y(DoorwayTop)));
            walls.Add((X(div), X(div + 1), Y(DoorwayBottom + 1), Y(Rows)));
        }
    }
}
