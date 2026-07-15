using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Farm RPG water — solid fill with optional shore fringe on cells bordering land.
/// </summary>
public static class WaterTiles
{
    public const int TilePixelSize = FarmRpgTileCells.Size;

    private static Texture2D? _fillSheet;
    private static Texture2D? _shoreSheet;

    public static bool IsLoaded => _fillSheet != null;

    public static void Load(ContentManager content)
    {
        _fillSheet = content.Load<Texture2D>("Tiles/FarmRpg/water_fill");
        _shoreSheet = content.Load<Texture2D>("Tiles/FarmRpg/grass_water");
    }

    public static void Update(float dt)
    {
    }

    public static bool TryDraw(SpriteBatch sb, WorldMap map, int worldTx, int worldTy, Rectangle dest)
    {
        if (_fillSheet == null) return false;

        var scale = DungeonFloorTiles.VisualScale;
        var fillSrc = new Rectangle(0, 0, TilePixelSize, TilePixelSize);
        sb.Draw(_fillSheet, ScaleDest(dest, scale), fillSrc, Color.White);

        var landMask = LandMask(map, worldTx, worldTy);
        if (landMask != 0 && _shoreSheet != null)
        {
            var shoreSrc = FarmRpgTileCells.WaterShoreSrc(landMask);
            sb.Draw(_shoreSheet, ScaleDest(dest, scale), shoreSrc, Color.White);
        }

        return true;
    }

    private static int LandMask(WorldMap map, int tx, int ty)
    {
        var mask = 0;
        if (map.IsLand(tx, ty - 1)) mask |= 8;
        if (map.IsLand(tx + 1, ty)) mask |= 4;
        if (map.IsLand(tx, ty + 1)) mask |= 2;
        if (map.IsLand(tx - 1, ty)) mask |= 1;
        return mask;
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
