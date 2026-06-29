using Microsoft.Xna.Framework;
using Deathborn.Client.Net;

namespace Deathborn.Client.Screens;

public sealed class ScreenManager
{
    private readonly DeathbornGame _game;
    private IScreen? _current;

    public ScreenManager(DeathbornGame game) => _game = game;

    public GameClient Net => _game.Client;

    public void Change(IScreen screen)
    {
        _current?.OnExit();
        _current = screen;
        _current.OnEnter();
    }

    public void Update(GameTime gameTime)
    {
        Net.Poll();
        _current?.Update(gameTime);
    }
    public void Draw(GameTime gameTime) => _current?.Draw(gameTime);
}
