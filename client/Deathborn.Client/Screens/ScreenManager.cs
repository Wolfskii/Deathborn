using Microsoft.Xna.Framework;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class ScreenManager
{
    private readonly DeathbornGame _game;
    private IScreen? _current;

    private readonly MusicMuteButton _musicMute = new();

    public ScreenManager(DeathbornGame game) => _game = game;

    public GameClient Net => _game.Client;

    public void Change(IScreen screen)
    {
        _current?.OnExit();
        TextField.ReleaseFocus();
        _current = screen;
        _current.OnEnter();
    }

    public bool TryHandleEscape() => _current?.HandleEscape() ?? false;

    public void Update(GameTime gameTime)
    {
        Net.Poll();
        _current?.Update(gameTime);
        _musicMute.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        _current?.Draw(gameTime);

        var sb = _game.SpriteBatch;
        sb.Begin();
        _musicMute.Draw(sb);
        sb.End();
    }
}
