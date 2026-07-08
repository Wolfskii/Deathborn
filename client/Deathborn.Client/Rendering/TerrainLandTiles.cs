using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Tiny Swords flat ground (Tilemap_color1) with 4-neighbour autotiling against water.
/// Mask bits: N=8, E=4, S=2, W=1 (1 = adjacent land).
/// </summary>
public static class TerrainLandTiles
{
    public const int TilePixelSize = 64;

    private static Texture2D? _sheet;

    // Mask index 0–15 → sheet cell (col, row) per Tiny Swords flat-ground autotile spec.
    private static readonly Point[] MaskToCell =
    [
        new(3, 3), // 0000 isolated island
        new(2, 3), // 0001
        new(3, 0), // 0010
        new(2, 0), // 0011
        new(0, 3), // 0100
        new(1, 3), // 0101
        new(0, 0), // 0110
        new(1, 0), // 0111
        new(3, 2), // 1000
        new(2, 2), // 1001
        new(3, 1), // 1010
        new(2, 1), // 1011
        new(0, 2), // 1100
        new(1, 2), // 1101
        new(0, 1), // 1110
        new(1, 1), // 1111 center fill
    ];

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content) =>
        _sheet = content.Load<Texture2D>("Tiles/tiny_swords_land_tileset");

    public static bool TryDrawLand(SpriteBatch sb, WorldMap map, int tx, int ty, Rectangle dest)
    {
        if (_sheet == null) return false;

        var mask = 0;
        if (map.IsLand(tx, ty - 1)) mask |= 8;
        if (map.IsLand(tx + 1, ty)) mask |= 4;
        if (map.IsLand(tx, ty + 1)) mask |= 2;
        if (map.IsLand(tx - 1, ty)) mask |= 1;

        var cell = MaskToCell[mask];
        var src = new Rectangle(
            cell.X * TilePixelSize,
            cell.Y * TilePixelSize,
            TilePixelSize,
            TilePixelSize);
        sb.Draw(_sheet, ScaleDest(dest, DungeonFloorTiles.VisualScale), src, Color.White);
        return true;
    }

    private static Rectangle ScaleDest(Rectangle dest, float scale)
    {
        if (MathF.Abs(scale - 1f) < 0.001f) return dest;
        var cx = dest.X + dest.Width * 0.5f;
        var cy = dest.Y + dest.Height * 0.5f;
        var w = Math.Max(1, (int)MathF.Ceiling(dest.Width * scale));
        var h = Math.Max(1, (int)MathF.Ceiling(dest.Height * scale));
        return new Rectangle((int)(cx - w * 0.5f), (int)(cy - h * 0.5f), w, h);
    }
}
