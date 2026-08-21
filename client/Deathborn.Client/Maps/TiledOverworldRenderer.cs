using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tilemaps;
using System.Text.RegularExpressions;

namespace Deathborn.Client.Maps;

/// <summary>
/// Draws the authored Swarovia mainland Tiled map (WYSIWYG tiles, no elevation autotile).
/// Farm RPG art and world cells are both 16 px; draw each tile at the screen cell rect.
/// Draws every layer live so pixel-snapped ground and animated shoreline tiles stay aligned while moving.
/// Layer names are free; gameplay/render groups use Tiled class ground_N / water_N.
/// </summary>
public static class TiledOverworldRenderer
{
    public const string MapId = "swarovia_mainland";

    private static readonly Regex WaterClass = new(@"^water_(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GroundClass = new(@"^ground_(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Retained for dev-performance logging compatibility; live rendering does not rebuild a cache.</summary>
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

        // Draw each layer with the same snapped per-tile rectangles. Mixing a scaled
        // render-target cache with live shoreline animation exposed moving blue seams.
        DrawMatchingLayersScreen(spriteBatch, map, region, water: true);
        DrawMatchingLayersScreen(spriteBatch, map, region, water: false);
    }

    private static bool IsWaterLayer(TilemapLayer layer)
    {
        if (!string.IsNullOrEmpty(layer.Class) && WaterClass.IsMatch(layer.Class))
            return true;
        // Legacy name fallback when classes are missing.
        return string.IsNullOrEmpty(layer.Class)
            && (layer.Name is "Water" or "Sea");
    }

    private static bool IsGroundLayer(TilemapLayer layer)
    {
        if (!string.IsNullOrEmpty(layer.Class) && GroundClass.IsMatch(layer.Class))
            return true;
        return string.IsNullOrEmpty(layer.Class)
            && (layer.Name is "Ground" or "Land");
    }

    private static void DrawMatchingLayersScreen(
        SpriteBatch spriteBatch,
        TiledMapInstance map,
        VisibleTileRegion region,
        bool water)
    {
        foreach (var layer in map.Map.Layers)
        {
            if (layer is not TilemapTileLayer)
                continue;
            if (water ? !IsWaterLayer(layer) : !IsGroundLayer(layer))
                continue;
            DrawLayerScreen(spriteBatch, map, layer.Name, region);
        }
    }

    private static void DrawLayerScreen(
        SpriteBatch spriteBatch,
        TiledMapInstance map,
        string layerName,
        VisibleTileRegion region)
    {
        var tilemap = map.Map;
        if (!tilemap.Layers.TryGetValue(layerName, out var layer) || layer is not TilemapTileLayer tileLayer)
            return;
        if (!layer.IsVisible)
            return;

        var tint = layer.TintColor.HasValue
            ? layer.TintColor.Value * layer.Opacity
            : Color.White * layer.Opacity;

        var tileRegion = new Rectangle(
            region.MinTx,
            region.MinTy,
            region.MaxTx - region.MinTx + 1,
            region.MaxTy - region.MinTy + 1);
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
