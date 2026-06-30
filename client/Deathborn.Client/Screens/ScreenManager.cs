using Microsoft.Xna.Framework;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class ScreenManager
{
    private readonly DeathbornGame _game;
    private IScreen? _current;
    private readonly EscMenuOverlay _escMenu = new();

    public ScreenManager(DeathbornGame game) => _game = game;

    public GameClient Net => _game.Client;

    public bool EscMenuOpen => _escMenu.IsOpen;

    public void SetOpenCharacterHandler(Action? handler) => _escMenu.OnOpenCharacter = handler;

    public void SetOpenSpellBookHandler(Action? handler) => _escMenu.OnOpenSpellBook = handler;

    public void SetOpenInventoryHandler(Action? handler) => _escMenu.OnOpenInventory = handler;

    public void SetOpenSkillsHandler(Action? handler) => _escMenu.OnOpenSkills = handler;

    public void SetBuildHouseHandler(Action? handler) => _escMenu.OnBuildHouse = handler;

    public void SetBuildHouseEnabled(bool enabled) => _escMenu.SetBuildHouseEnabled(enabled);

    public void Change(IScreen screen)
    {
        _current?.OnExit();
        TextField.ReleaseFocus();
        _escMenu.Close();
        _current = screen;
        _current.OnEnter();
    }

    public bool TryHandleEscape()
    {
        if (_escMenu.IsOpen)
        {
            _escMenu.Close();
            return true;
        }

        if (_current?.HandleEscape() == true)
            return true;

        _escMenu.Open();
        return true;
    }

    public void Update(GameTime gameTime)
    {
        Net.Poll();
        _escMenu.Update(gameTime);
        _current?.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        _current?.Draw(gameTime);

        if (!_escMenu.IsOpen) return;

        var sb = _game.SpriteBatch;
        IReadOnlyList<string>? debugLines = _current is IDebugInfoScreen info ? info.DebugInfoLines : null;
        sb.Begin();
        _escMenu.Draw(sb, _game.Font, debugLines);
        sb.End();
    }
}
