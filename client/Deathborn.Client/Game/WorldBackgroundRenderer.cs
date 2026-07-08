using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class WorldBackgroundRenderer
{
    private readonly VisibleTileRegion _tiles = new();

    public void Draw(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var map = WorldMap.Realik;
        _tiles.Begin(map, camera, screenCenter, zoom, marginTiles: 3f);
        map.Draw(sb, _tiles);
    }
}
