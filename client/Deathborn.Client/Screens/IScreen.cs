using Microsoft.Xna.Framework;

namespace Deathborn.Client.Screens;

public interface IScreen
{
    void OnEnter();
    void OnExit();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}
