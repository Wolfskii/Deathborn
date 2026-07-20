using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tilemaps;

namespace Deathborn.Client.Maps;

/// <summary>
/// Draws the authored Swarovia mainland Tiled map (WYSIWYG tiles, no elevation autotile).
/// Farm RPG art is 16 px; world cells are 32 px — stretch each tile to the screen cell rect.
/// Visible tiles are baked into a padded render-target so scrolling is mostly one blit.
/// </summary>
public static class TiledOverworldRenderer
{
    public const string MapId = "swarovia_mainland";

    private const int CachePadTiles = 8;
    private const float AnimRebuildSeconds = 0.2f;

    private static readonly string[] LayerDrawOrder = ["Water", "Ground"];

    private static RenderTarget2D? _cache;
    private static int _cacheMinTx, _cacheMaxTx, _cacheMinTy, _cacheMaxTy;
    private static float _animAge;
    /// <summary>Set when the tile cache was rebuilt this frame (cleared by DevPerfLog consumers).</summary>
    public static string? LastRebuildReason { get; private set; }

    public static bool IsActive => TiledMapCatalog.TryGet(MapId) != null;

    public static void Draw(
        SpriteBatch spriteBatch,
        GraphicsDevice graphicsDevice,
        TiledMapInstance map,
        GameTime gameTime,
        VisibleTileRegion region)
    {
        LastRebuildReason = null;
        map.Update(gameTime);
        _animAge += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (NeedsRebuild(region, out var reason))
        {
            LastRebuildReason = reason;
            RebuildCache(spriteBatch, graphicsDevice, map, region);
        }

        if (_cache == null)
            return;

        var tl = region.Rect(_cacheMinTx, _cacheMinTy);
        var br = region.Rect(_cacheMaxTx, _cacheMaxTy);
        var dest = new Rectangle(tl.X, tl.Y, br.Right - tl.X, br.Bottom - tl.Y);
        spriteBatch.Draw(_cache, dest, Color.White);
    }

    private static bool NeedsRebuild(VisibleTileRegion region, out string reason)
    {
        if (_cache == null)
        {
            reason = "cold";
            return true;
        }
        if (_animAge >= AnimRebuildSeconds)
        {
            reason = "anim";
            return true;
        }
        if (region.MinTx < _cacheMinTx || region.MaxTx > _cacheMaxTx ||
            region.MinTy < _cacheMinTy || region.MaxTy > _cacheMaxTy)
        {
            reason = "scroll";
            return true;
        }

        reason = "";
        return false;
    }

    private static void RebuildCache(
        SpriteBatch spriteBatch,
        GraphicsDevice graphicsDevice,
        TiledMapInstance map,
        VisibleTileRegion region)
    {
        var world = WorldMap.SwaroviaMainland;
        var tileSize = (int)Math.Max(1, MathF.Round(world.TileSize));

        var minTx = Math.Max(0, region.MinTx - CachePadTiles);
        var maxTx = Math.Min(world.TileWidth - 1, region.MaxTx + CachePadTiles);
        var minTy = Math.Max(0, region.MinTy - CachePadTiles);
        var maxTy = Math.Min(world.TileHeight - 1, region.MaxTy + CachePadTiles);

        var tileW = maxTx - minTx + 1;
        var tileH = maxTy - minTy + 1;
        var pxW = tileW * tileSize;
        var pxH = tileH * tileSize;

        if (_cache == null || _cache.Width != pxW || _cache.Height != pxH)
        {
            _cache?.Dispose();
            _cache = new RenderTarget2D(
                graphicsDevice,
                pxW,
                pxH,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
        }

        spriteBatch.End();
        var prevTargets = graphicsDevice.GetRenderTargets();
        graphicsDevice.SetRenderTarget(_cache);
        graphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var tilemap = map.Map;
        var tileRegion = new Rectangle(minTx, minTy, tileW, tileH);

        foreach (var layerName in LayerDrawOrder)
        {
            if (!tilemap.Layers.TryGetValue(layerName, out var layer) || layer is not TilemapTileLayer tileLayer)
                continue;
            if (!layer.IsVisible)
                continue;

            var tint = layer.TintColor.HasValue
                ? layer.TintColor.Value * layer.Opacity
                : Color.White * layer.Opacity;

            foreach (var entry in tileLayer.GetTilesInRegion(tileRegion))
            {
                var localId = entry.Tile.GetLocalId(tilemap.Tilesets, out var tileset);
                if (tileset == null)
                    continue;

                tileset.GetRenderSource(localId, out var texture, out var sourceRect);
                if (texture == null)
                    continue;

                var dest = new Rectangle(
                    (entry.X - minTx) * tileSize,
                    (entry.Y - minTy) * tileSize,
                    tileSize,
                    tileSize);
                spriteBatch.Draw(texture, dest, sourceRect, tint);
            }
        }

        spriteBatch.End();
        graphicsDevice.SetRenderTargets(prevTargets);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _cacheMinTx = minTx;
        _cacheMaxTx = maxTx;
        _cacheMinTy = minTy;
        _cacheMaxTy = maxTy;
        _animAge = 0f;
    }
}
