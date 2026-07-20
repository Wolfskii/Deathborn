using System.Text.Json;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tilemaps;
using MonoGame.Extended.Tilemaps.Tiled;

namespace Deathborn.Client.Maps;

/// <summary>Loads authored Tiled maps listed in Content/Maps/manifest.json.</summary>
public static class TiledMapCatalog
{
    private static readonly Dictionary<string, TiledMapInstance> _maps = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static IReadOnlyDictionary<string, TiledMapInstance> Maps => _maps;

    /// <summary>
    /// Loads .tmx files from disk at runtime (edit in Tiled, restart client — no MGCB rebuild).
    /// </summary>
    public static void Load(ContentManager content, GraphicsDevice graphicsDevice)
    {
        if (_loaded)
            return;

        _loaded = true;
        var contentRoot = Path.GetFullPath(content.RootDirectory);
        var manifestPath = Path.Combine(contentRoot, "Maps", "manifest.json");
        if (!File.Exists(manifestPath))
            return;

        using var stream = File.OpenRead(manifestPath);
        var manifest = JsonSerializer.Deserialize<MapManifest>(stream, JsonOptions);
        if (manifest?.Maps == null)
            return;

        var parser = new TiledTmxParser(contentRoot);

        foreach (var entry in manifest.Maps)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.ContentPath))
                continue;

            var tmxRelative = entry.ContentPath.Replace('/', Path.DirectorySeparatorChar) + ".tmx";
            var tmxFull = Path.Combine(contentRoot, tmxRelative);
            if (!File.Exists(tmxFull))
            {
                Console.WriteLine($"[TiledMapCatalog] Missing map file: {tmxFull}");
                continue;
            }

            try
            {
                var map = parser.ParseFromFile(tmxRelative, graphicsDevice);
                var metadata = TiledMapMetadata.FromTilemap(map);
                var title = entry.Title ?? map.Name;
                _maps[entry.Id] = new TiledMapInstance(entry.Id, title, map, metadata);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TiledMapCatalog] Failed to load '{entry.Id}' ({tmxRelative}): {ex.Message}");
            }
        }
    }

    public static TiledMapInstance? TryGet(string mapId) =>
        _maps.TryGetValue(mapId, out var map) ? map : null;

    public static void Clear()
    {
        foreach (var map in _maps.Values)
            map.Dispose();
        _maps.Clear();
        _loaded = false;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class MapManifest
    {
        public List<MapEntry>? Maps { get; set; }
    }

    private sealed class MapEntry
    {
        public string? Id { get; set; }
        public string? ContentPath { get; set; }
        public string? Title { get; set; }
        public List<string>? Tags { get; set; }
    }
}
