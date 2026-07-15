using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Farm RPG flat ground with plain grass fill and shoreline autotiles.
/// </summary>
public static class TerrainLandTiles
{
    public const int TilePixelSize = FarmRpgTileCells.Size;

    private static Texture2D? _grassSheet;

    public static bool IsLoaded => _grassSheet != null;

    public static void Load(ContentManager content) =>
        _grassSheet = content.Load<Texture2D>("Tiles/FarmRpg/grass_elev1");

    public static bool TryDrawLand(SpriteBatch sb, WorldMap map, int tx, int ty, Rectangle dest)
    {
        if (_grassSheet == null) return false;

        var src = FarmRpgTileCells.GrassSrc(15);
        sb.Draw(_grassSheet, ScaleDest(dest, DungeonFloorTiles.VisualScale), src, Color.White);
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
