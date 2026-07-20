using Microsoft.Xna.Framework;
using MonoGame.Extended.Tilemaps;
using MonoGame.Extended.Tilemaps.Rendering;

namespace Deathborn.Client.Maps;

/// <summary>Loaded Tiled map plus renderer and parsed gameplay metadata.</summary>
public sealed class TiledMapInstance : IDisposable
{
    public string MapId { get; }
    public string Title { get; }
    public Tilemap Map { get; }
    public TiledMapMetadata Metadata { get; }
    public TilemapSpriteBatchRenderer Renderer { get; }

    public int WidthInPixels => Map.Width * Map.TileWidth;
    public int HeightInPixels => Map.Height * Map.TileHeight;

    public TiledMapInstance(string mapId, string title, Tilemap map, TiledMapMetadata metadata)
    {
        MapId = mapId;
        Title = title;
        Map = map;
        Metadata = metadata;
        Renderer = new TilemapSpriteBatchRenderer();
        Renderer.LoadTilemap(map);
    }

    public void Update(GameTime gameTime) => Renderer.Update(gameTime);

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, MonoGame.Extended.OrthographicCamera camera) =>
        Renderer.Draw(spriteBatch, camera);

    public void SetWorldAnchor(Vector2 worldCenter)
    {
        Map.WorldPosition = worldCenter - new Vector2(WidthInPixels * 0.5f, HeightInPixels * 0.5f);
    }

    public bool BlocksCircle(Vector2 worldPos, float radius)
    {
        foreach (var rect in Metadata.CollisionRects)
        {
            var worldRect = OffsetRect(rect);
            if (CircleIntersectsRect(worldPos, radius, worldRect))
                return true;
        }

        return false;
    }

    public Vector2 ResolveMove(Vector2 feet, Vector2 delta)
    {
        var next = feet + delta;
        if (!BlocksCircle(next, Metadata.CollisionRadius))
            return next;

        var slideX = new Vector2(next.X, feet.Y);
        if (!BlocksCircle(slideX, Metadata.CollisionRadius))
            return slideX;

        var slideY = new Vector2(feet.X, next.Y);
        if (!BlocksCircle(slideY, Metadata.CollisionRadius))
            return slideY;

        return feet;
    }

    private Rectangle OffsetRect(Rectangle rect) =>
        new(
            (int)(Map.WorldPosition.X + rect.X),
            (int)(Map.WorldPosition.Y + rect.Y),
            rect.Width,
            rect.Height);

    private static bool CircleIntersectsRect(Vector2 center, float radius, Rectangle rect)
    {
        var closestX = Math.Clamp(center.X, rect.Left, rect.Right);
        var closestY = Math.Clamp(center.Y, rect.Top, rect.Bottom);
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }

    public void Dispose()
    {
        // TilemapSpriteBatchRenderer has no unmanaged resources.
    }
}
