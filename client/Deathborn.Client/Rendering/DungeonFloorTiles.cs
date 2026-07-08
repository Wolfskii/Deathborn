using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Bitcrawl dungeon floor tiles (land-only 2×3 block on the left of Tileset.png).</summary>
public static class DungeonFloorTiles
{
    public const int TilePixelSize = 16;
    /// <summary>Draw tiles slightly larger than the world grid to reduce seams.</summary>
    public const float VisualScale = 1.125f;

    private static Texture2D? _sheet;

    // Top-left 2×3 floor-only block.
    private static readonly Point[] FloorVariants =
    [
        new(0, 0), new(1, 0),
        new(0, 1), new(1, 1),
        new(0, 2), new(1, 2),
    ];

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content) =>
        _sheet = content.Load<Texture2D>("Tiles/dungeon_floor_tileset");

    public static bool TryDrawFloor(SpriteBatch sb, int worldTx, int worldTy, Rectangle dest) =>
        TryDraw(sb, FloorVariant(worldTx, worldTy), dest);

    private static bool TryDraw(SpriteBatch sb, Point gridCell, Rectangle dest)
    {
        if (_sheet == null) return false;

        var src = new Rectangle(
            gridCell.X * TilePixelSize,
            gridCell.Y * TilePixelSize,
            TilePixelSize,
            TilePixelSize);
        sb.Draw(_sheet, ScaleDest(dest, VisualScale), src, Color.White);
        return true;
    }

    /// <summary>Mostly one neutral floor tile; rare accents for subtle variation.</summary>
    private static Point FloorVariant(int tx, int ty)
    {
        var hash = Math.Abs(tx * 7 + ty * 13 + tx * ty * 3);
        if (hash % 6 != 0)
            return FloorVariants[0];

        return FloorVariants[1 + hash % (FloorVariants.Length - 1)];
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
