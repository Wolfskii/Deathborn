using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Calm open-water tiles from Water+.png (row 1–2, cols 2–5).</summary>
public static class WaterTiles
{
    public const int TilePixelSize = 16;

    private static Texture2D? _sheet;
    private static readonly Point[] WaterVariants =
    [
        new(3, 1),
        new(2, 1), new(4, 1), new(5, 1),
        new(3, 2),
    ];

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content) =>
        _sheet = content.Load<Texture2D>("Tiles/water_tileset");

    public static bool TryDraw(SpriteBatch sb, int worldTx, int worldTy, Rectangle dest)
    {
        if (_sheet == null) return false;

        var variant = WaterVariant(worldTx, worldTy);
        var src = new Rectangle(
            variant.X * TilePixelSize,
            variant.Y * TilePixelSize,
            TilePixelSize,
            TilePixelSize);
        sb.Draw(_sheet, ScaleDest(dest, DungeonFloorTiles.VisualScale), src, Color.White);
        return true;
    }

    private static Point WaterVariant(int tx, int ty)
    {
        var hash = Math.Abs(tx * 11 + ty * 17 + tx * ty * 5);
        if (hash % 5 != 0)
            return WaterVariants[0];

        return WaterVariants[1 + hash % (WaterVariants.Length - 1)];
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
