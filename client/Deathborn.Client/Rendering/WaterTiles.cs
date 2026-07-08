using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Tiny Swords water fill (Water Background color) plus animated shoreline foam.
/// Foam frames are 192×192 (3×3 tile footprint) placed under flat land tiles.
/// </summary>
public static class WaterTiles
{
    public const int TilePixelSize = 64;
    public const int FoamFrameSize = 192;
    public const int FoamFrameCount = 16;
    public const float FoamFrameDuration = 0.1f;

    private static Texture2D? _bgSheet;
    private static Texture2D? _foamSheet;
    private static float _animTime;

    public static bool IsLoaded => _bgSheet != null;

    public static void Load(ContentManager content)
    {
        _bgSheet = content.Load<Texture2D>("Tiles/tiny_swords_water_bg");
        _foamSheet = content.Load<Texture2D>("Tiles/tiny_swords_water_foam");
    }

    public static void Update(float dt) => _animTime += dt;

    public static bool TryDraw(SpriteBatch sb, int worldTx, int worldTy, Rectangle dest)
    {
        if (_bgSheet == null) return false;

        var src = new Rectangle(0, 0, TilePixelSize, TilePixelSize);
        sb.Draw(_bgSheet, ScaleDest(dest, DungeonFloorTiles.VisualScale), src, Color.White);
        return true;
    }

    /// <summary>Animated foam under land tiles that border water (drawn before land).</summary>
    public static bool TryDrawFoam(SpriteBatch sb, WorldMap map, int tx, int ty, Rectangle dest)
    {
        if (_foamSheet == null || !map.IsLand(tx, ty) || !BordersWater(map, tx, ty))
            return false;

        var frame = FoamFrame(tx, ty);
        var src = new Rectangle(frame * FoamFrameSize, 0, FoamFrameSize, FoamFrameSize);
        sb.Draw(_foamSheet, ExpandDest(dest, 3f), src, Color.White);
        return true;
    }

    private static bool BordersWater(WorldMap map, int tx, int ty)
    {
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            if (!map.IsLand(tx + dx, ty + dy)) return true;
        }
        return false;
    }

    private static int FoamFrame(int tx, int ty)
    {
        var phase = (tx * 5 + ty * 11) % FoamFrameCount;
        return ((int)(_animTime / FoamFrameDuration) + phase) % FoamFrameCount;
    }

    private static Rectangle ExpandDest(Rectangle dest, float tileSpan)
    {
        var scale = DungeonFloorTiles.VisualScale;
        var cx = dest.X + dest.Width * 0.5f;
        var cy = dest.Y + dest.Height * 0.5f;
        var w = Math.Max(1, (int)MathF.Ceiling(dest.Width * tileSpan * scale));
        var h = Math.Max(1, (int)MathF.Ceiling(dest.Height * tileSpan * scale));
        return new Rectangle((int)(cx - w * 0.5f), (int)(cy - h * 0.5f), w, h);
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
