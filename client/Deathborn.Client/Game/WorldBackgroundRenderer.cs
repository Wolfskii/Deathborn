using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Gameplay;

public sealed class WorldBackgroundRenderer
{
    public void Draw(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom) =>
        WorldMap.Realik.Draw(sb, camera, screenCenter, zoom);
}
