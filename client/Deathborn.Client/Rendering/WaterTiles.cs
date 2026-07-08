using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Tiny Swords water fill (Water Background color) plus animated shoreline foam.
/// Foam frames are 192×192 (3×3 footprint); only the portions over water cells are drawn.
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

    /// <summary>
    /// Shoreline foam centered on a land tile, clipped to cardinal-adjacent water cells only
    /// so the 3×3 sprite does not bleed onto interior land.
    /// </summary>
    public static void TryDrawShoreFoam(
        SpriteBatch sb, WorldMap map, int tx, int ty, Rectangle landRect,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (_foamSheet == null || !map.IsLand(tx, ty) || map.GetElevation(tx, ty) != 0
            || !CardinallyBordersWater(map, tx, ty))
            return;

        var foamDest = ExpandDest(landRect, 3f);
        var frame = FoamFrame(tx, ty);
        var src = new Rectangle(frame * FoamFrameSize, 0, FoamFrameSize, FoamFrameSize);
        var tileSize = map.TileSize;

        if (!map.IsLand(tx, ty - 1))
            TryClipDraw(sb, foamDest, WorldMap.GetTileScreenRect(tx, ty - 1, camera, screenCenter, zoom, tileSize), src);
        if (!map.IsLand(tx + 1, ty))
            TryClipDraw(sb, foamDest, WorldMap.GetTileScreenRect(tx + 1, ty, camera, screenCenter, zoom, tileSize), src);
        if (!map.IsLand(tx, ty + 1))
            TryClipDraw(sb, foamDest, WorldMap.GetTileScreenRect(tx, ty + 1, camera, screenCenter, zoom, tileSize), src);
        if (!map.IsLand(tx - 1, ty))
            TryClipDraw(sb, foamDest, WorldMap.GetTileScreenRect(tx - 1, ty, camera, screenCenter, zoom, tileSize), src);
    }

    private static bool CardinallyBordersWater(WorldMap map, int tx, int ty) =>
        !map.IsLand(tx, ty - 1) || !map.IsLand(tx + 1, ty)
        || !map.IsLand(tx, ty + 1) || !map.IsLand(tx - 1, ty);

    private static void TryClipDraw(SpriteBatch sb, Rectangle dest, Rectangle clip, Rectangle src)
    {
        var intersect = Rectangle.Intersect(dest, clip);
        if (intersect.Width <= 0 || intersect.Height <= 0 || _foamSheet == null)
            return;

        var srcX = src.X + (intersect.X - dest.X) * src.Width / dest.Width;
        var srcY = src.Y + (intersect.Y - dest.Y) * src.Height / dest.Height;
        var srcW = intersect.Width * src.Width / dest.Width;
        var srcH = intersect.Height * src.Height / dest.Height;
        sb.Draw(_foamSheet, intersect, new Rectangle(srcX, srcY, srcW, srcH), Color.White);
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
