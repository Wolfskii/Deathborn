using Microsoft.Xna.Framework;
using MonoGame.Extended.Tilemaps;

namespace Deathborn.Client.Maps;

/// <summary>Gameplay data extracted from Tiled object layers and map properties.</summary>
public sealed class TiledMapMetadata
{
    public const string CollisionLayerName = "Collision";
    public const string ObjectsLayerName = "Objects";

    public IReadOnlyList<Rectangle> CollisionRects { get; init; } = [];
    public Vector2? SpawnPosition { get; init; }
    public float CollisionRadius { get; init; } = 12f;

    public static TiledMapMetadata FromTilemap(Tilemap map)
    {
        var collision = new List<Rectangle>();
        Vector2? spawn = null;

        if (map.Layers.TryGetValue(CollisionLayerName, out var collisionLayer)
            && collisionLayer is TilemapObjectLayer collisionObjects)
        {
            foreach (var obj in collisionObjects.Objects)
            {
                if (obj is TilemapRectangleObject rect)
                {
                    collision.Add(new Rectangle(
                        (int)rect.Position.X,
                        (int)rect.Position.Y,
                        (int)rect.Size.X,
                        (int)rect.Size.Y));
                }
            }
        }

        if (map.Layers.TryGetValue(ObjectsLayerName, out var objectsLayer)
            && objectsLayer is TilemapObjectLayer objectLayer)
        {
            foreach (var obj in objectLayer.Objects)
            {
                if (!IsSpawnObject(obj))
                    continue;

                spawn = obj.Position + new Vector2(map.TileWidth * 0.5f, map.TileHeight * 0.5f);
                break;
            }
        }

        return new TiledMapMetadata
        {
            CollisionRects = collision,
            SpawnPosition = spawn,
            CollisionRadius = map.Properties.GetFloat("collision_radius", 12f),
        };
    }

    private static bool IsSpawnObject(TilemapObject obj)
    {
        if (string.Equals(obj.Name, "Spawn", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(obj.Class, "spawn", StringComparison.OrdinalIgnoreCase))
            return true;

        var type = obj.Properties.GetString("type", string.Empty);
        return string.Equals(type, "spawn", StringComparison.OrdinalIgnoreCase);
    }
}
