using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Maps;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class WorldBackgroundRenderer
{
    private readonly VisibleTileRegion _tiles = new();

    public void Draw(
        SpriteBatch sb,
        GraphicsDevice graphicsDevice,
        GameTime gameTime,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        var map = WorldMap.SwaroviaMainland;
        _tiles.Begin(map, camera, screenCenter, zoom, marginTiles: 3f);

        if (TiledMapCatalog.TryGet(TiledOverworldRenderer.MapId) is { } painted)
        {
            TiledOverworldRenderer.Draw(sb, graphicsDevice, painted, gameTime, _tiles);
            return;
        }

        map.Draw(sb, _tiles);
    }
}
