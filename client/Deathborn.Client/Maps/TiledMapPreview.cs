using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Deathborn.Client.Maps;

/// <summary>Dev overlay: draw a Tiled map in world space using the game's camera/zoom.</summary>
public static class TiledMapPreview
{
    private static OrthographicCamera? _camera;

    public static bool IsActive { get; private set; }
    public static string ActiveMapId { get; private set; } = "starter_room";

    public static void Toggle(string mapId = "starter_room")
    {
        IsActive = !IsActive;
        ActiveMapId = mapId;
    }

    public static void EnsureCamera(GraphicsDevice graphicsDevice)
    {
        _camera ??= new OrthographicCamera(graphicsDevice);
    }

    public static void Draw(
        SpriteBatch spriteBatch,
        GraphicsDevice graphicsDevice,
        TiledMapInstance map,
        Vector2 worldCamera,
        float worldZoom)
    {
        EnsureCamera(graphicsDevice);
        if (_camera == null)
            return;

        _camera.LookAt(worldCamera);
        _camera.Zoom = worldZoom;
        map.Draw(spriteBatch, _camera);
    }
}
