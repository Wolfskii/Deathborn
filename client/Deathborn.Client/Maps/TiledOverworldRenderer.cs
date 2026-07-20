using Deathborn.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tilemaps;

namespace Deathborn.Client.Maps;

/// <summary>
/// Draws the authored Swarovia mainland Tiled map (WYSIWYG tiles, no elevation autotile).
/// Farm RPG art is 16 px; world cells are 32 px — stretch each tile to the screen cell rect.
/// </summary>
public static class TiledOverworldRenderer
{
    public const string MapId = "swarovia_mainland";

    private static readonly string[] LayerDrawOrder = ["Water", "Ground"];

    public static bool IsActive => TiledMapCatalog.TryGet(MapId) != null;

    public static void Draw(
        SpriteBatch spriteBatch,
        TiledMapInstance map,
        GameTime gameTime,
        VisibleTileRegion region)
    {
        map.Update(gameTime);

        var tilemap = map.Map;
        var tileRegion = new Rectangle(
            region.MinTx,
            region.MinTy,
            region.MaxTx - region.MinTx + 1,
            region.MaxTy - region.MinTy + 1);

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

                spriteBatch.Draw(texture, region.Rect(entry.X, entry.Y), sourceRect, tint);
            }
        }
    }
}
