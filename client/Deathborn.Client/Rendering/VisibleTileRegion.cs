using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>Visible tile bounds plus per-frame screen rectangles for the tile grid.</summary>
public sealed class VisibleTileRegion
{
    /// <summary>Extra tiles cached beyond the view so cliff/shadow/foam neighbors do not miss the rect cache.</summary>
    private const float NeighborTilePad = 2f;

    public int MinTx { get; private set; }
    public int MaxTx { get; private set; }
    public int MinTy { get; private set; }
    public int MaxTy { get; private set; }
    public float Zoom { get; private set; }
    public Vector2 Camera { get; private set; }
    public Vector2 ScreenCenter { get; private set; }
    public float TileSize { get; private set; }

    private Rectangle[] _rects = [];
    private int _width;

    public void Begin(WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom, float marginTiles)
    {
        Camera = camera;
        ScreenCenter = screenCenter;
        Zoom = zoom;
        TileSize = map.TileSize;

        var halfViewW = screenCenter.X / zoom + map.TileSize * (marginTiles + NeighborTilePad);
        var halfViewH = screenCenter.Y / zoom + map.TileSize * (marginTiles + NeighborTilePad);

        MinTx = Math.Clamp((int)((camera.X - halfViewW) / map.TileSize), 0, map.TileWidth - 1);
        MaxTx = Math.Clamp((int)((camera.X + halfViewW) / map.TileSize), 0, map.TileWidth - 1);
        MinTy = Math.Clamp((int)((camera.Y - halfViewH) / map.TileSize), 0, map.TileHeight - 1);
        MaxTy = Math.Clamp((int)((camera.Y + halfViewH) / map.TileSize), 0, map.TileHeight - 1);

        _width = MaxTx - MinTx + 1;
        var height = MaxTy - MinTy + 1;
        var count = _width * height;
        if (_rects.Length < count)
            _rects = new Rectangle[count];

        var i = 0;
        for (var ty = MinTy; ty <= MaxTy; ty++)
        for (var tx = MinTx; tx <= MaxTx; tx++, i++)
            _rects[i] = WorldMap.GetTileScreenRect(tx, ty, camera, screenCenter, zoom, map.TileSize);
    }

    public Rectangle Rect(int tx, int ty)
    {
        if ((uint)(tx - MinTx) >= (uint)_width || (uint)(ty - MinTy) >= (uint)(MaxTy - MinTy + 1))
        {
            return WorldMap.GetTileScreenRect(tx, ty, Camera, ScreenCenter, Zoom, TileSize);
        }

        return _rects[(ty - MinTy) * _width + (tx - MinTx)];
    }

    public void ForEachTile(Action<int, int> visit)
    {
        for (var ty = MinTy; ty <= MaxTy; ty++)
        for (var tx = MinTx; tx <= MaxTx; tx++)
            visit(tx, ty);
    }
}
