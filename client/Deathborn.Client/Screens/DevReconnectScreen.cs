using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

/// <summary>
/// Dev-only boot screen: log in with saved credentials and enter the world without the login UI.
/// </summary>
public sealed class DevReconnectScreen : IScreen
{
    private static readonly Color BgBlack = new(0, 0, 0);
    private static readonly Color Status = new(220, 180, 120);
    private static readonly Color StatusError = new(255, 120, 100);

    private readonly ScreenManager _screens;
    private string _email = "";
    private string _password = "";
    private string _status = "Reconnecting...";
    private bool _failed;

    public DevReconnectScreen(ScreenManager screens) => _screens = screens;

    public void OnEnter()
    {
        _status = "Reconnecting...";
        _failed = false;

        if (SavedLogin.LoadIfRemembered() is not { } saved)
        {
            _screens.Change(new LoginScreen(_screens));
            return;
        }

        _email = saved.Email;
        _password = saved.Password;

        var net = _screens.Net;
        net.AuthSucceeded -= OnAuthOk;
        net.AuthFailed -= OnAuthFail;
        net.Welcome -= OnWelcome;
        net.NeedCharacter -= OnNeedCharacter;
        net.Disconnected -= OnDisconnected;
        net.AuthSucceeded += OnAuthOk;
        net.AuthFailed += OnAuthFail;
        net.Welcome += OnWelcome;
        net.NeedCharacter += OnNeedCharacter;
        net.Disconnected += OnDisconnected;

        _ = ReconnectAsync();
    }

    public void OnExit()
    {
        var net = _screens.Net;
        net.AuthSucceeded -= OnAuthOk;
        net.AuthFailed -= OnAuthFail;
        net.Welcome -= OnWelcome;
        net.NeedCharacter -= OnNeedCharacter;
        net.Disconnected -= OnDisconnected;
    }

    public void Update(GameTime gameTime)
    {
    }

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;
        var cx = GameViewport.Width / 2f;

        sb.Begin();
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), BgBlack);

        var color = _failed ? StatusError : Status;
        var size = font.MeasureString(_status);
        sb.DrawString(font, _status, new Vector2(cx - size.X / 2f, GameViewport.Height / 2f - size.Y / 2f), color);
        sb.End();
    }

    private async Task ReconnectAsync()
    {
        _status = "Logging in...";
        await _screens.Net.LoginAsync(_email, _password);
    }

    private void OnAuthOk()
    {
        SavedLogin.Save(true, _email, _password);
        _status = "Entering world...";
        _ = _screens.Net.ConnectWorldAsync();
    }

    private void OnAuthFail(string msg)
    {
        _failed = true;
        _status = msg.StartsWith("Outdated", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("Incompatible", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("Could not", StringComparison.OrdinalIgnoreCase)
            ? msg
            : "Reconnect failed: " + msg;
        _screens.Change(new LoginScreen(_screens));
    }

    private void OnWelcome(WelcomeData _) => _screens.Change(new WorldScreen(_screens));
    private void OnNeedCharacter() => _screens.Change(new CharacterCreateScreen(_screens));

    private void OnDisconnected()
    {
        _failed = true;
        _status = "Reconnect failed: could not connect to world server";
        _screens.Change(new LoginScreen(_screens));
    }
}
