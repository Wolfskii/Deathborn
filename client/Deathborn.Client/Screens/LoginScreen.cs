using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class LoginScreen : IScreen
{
    private readonly ScreenManager _screens;
    private readonly TextField _email = new() { Placeholder = "email" };
    private readonly TextField _password = new() { Placeholder = "password", IsPassword = true };
    private readonly Checkbox _remember = new() { Label = "Remember email and password", Checked = true };
    private readonly Button _loginBtn = new() { Label = "Login" };
    private readonly Button _registerBtn = new() { Label = "Register" };
    private string _status = "";
    private bool _busy;
    private KeyboardState _prevKb;
    private Point _mouse;
    private bool _waitingWorld;

    public LoginScreen(ScreenManager screens) => _screens = screens;

    public void OnEnter()
    {
        Layout();
        _status = "";
        _busy = false;
        _waitingWorld = false;
        _email.Focused = true;

        if (SavedLogin.LoadIfRemembered() is { } saved)
        {
            _email.Text = saved.Email;
            _password.Text = saved.Password;
            _remember.Checked = true;
        }

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
    }

    public void OnExit()
    {
        _email.Focused = false;
        _password.Focused = false;

        var net = _screens.Net;
        net.AuthSucceeded -= OnAuthOk;
        net.AuthFailed -= OnAuthFail;
        net.Welcome -= OnWelcome;
        net.NeedCharacter -= OnNeedCharacter;
        net.Disconnected -= OnDisconnected;
    }

    public void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        _mouse = mouse.Position;

        if (_busy && !_waitingWorld)
        {
            _prevKb = kb;
            _prevMouse = mouse;
            return;
        }

        _email.Update(gameTime, kb, _prevKb);
        _password.Update(gameTime, kb, _prevKb);

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            _email.Focused = _email.Bounds.Contains(_mouse);
            _password.Focused = _password.Bounds.Contains(_mouse);

            if (_remember.ContainsBox(_mouse))
                _remember.Checked = !_remember.Checked;

            if (_loginBtn.Contains(_mouse)) _ = LoginAsync();
            if (_registerBtn.Contains(_mouse)) _ = RegisterAsync();
        }

        if (kb.IsKeyDown(Keys.Tab) && !_prevKb.IsKeyDown(Keys.Tab))
        {
            if (_email.Focused) { _email.Focused = false; _password.Focused = true; }
            else { _password.Focused = false; _email.Focused = true; }
        }

        if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter) && !_busy)
            _ = LoginAsync();

        _prevKb = kb;
        _prevMouse = mouse;
    }

    private MouseState _prevMouse;

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;

        sb.Begin();
        var cx = Config.Width / 2;
        sb.DrawString(font, "DEATHBORN", new Vector2(cx - font.MeasureString("DEATHBORN").X / 2, 120), Color.White);
        sb.DrawString(font, "You are born to die. Only skill decides when.",
            new Vector2(cx - font.MeasureString("You are born to die. Only skill decides when.").X / 2, 155),
            new Color(180, 180, 190));

        _email.Draw(sb, font);
        _password.Draw(sb, font);
        _remember.Draw(sb, font, _remember.ContainsBox(_mouse));
        _loginBtn.Draw(sb, font, _loginBtn.Contains(_mouse));
        _registerBtn.Draw(sb, font, _registerBtn.Contains(_mouse));

        if (!string.IsNullOrEmpty(_status))
            sb.DrawString(font, _status, new Vector2(cx - 200, 450), new Color(220, 180, 120));

        sb.End();
    }

    private void Layout()
    {
        var cx = Config.Width / 2;
        _email.Bounds = new Rectangle(cx - 180, 220, 360, 36);
        _password.Bounds = new Rectangle(cx - 180, 270, 360, 36);
        _remember.BoxBounds = new Rectangle(cx - 180, 318, 20, 20);
        _loginBtn.Bounds = new Rectangle(cx - 180, 360, 170, 36);
        _registerBtn.Bounds = new Rectangle(cx + 10, 360, 170, 36);
    }

    private async Task RegisterAsync()
    {
        _busy = true;
        _status = "Registering...";
        await _screens.Net.RegisterAsync(_email.Text, _password.Text);
    }

    private async Task LoginAsync()
    {
        _busy = true;
        _status = "Logging in...";
        await _screens.Net.LoginAsync(_email.Text, _password.Text);
    }

    private void OnAuthOk()
    {
        SavedLogin.Save(_remember.Checked, _email.Text, _password.Text);

        _status = "Entering world...";
        _waitingWorld = true;
        _ = _screens.Net.ConnectWorldAsync();
    }

    private void OnAuthFail(string msg)
    {
        _status = "Error: " + msg;
        _busy = false;
        _waitingWorld = false;
    }

    private void OnWelcome(WelcomeData _) => _screens.Change(new WorldScreen(_screens));
    private void OnNeedCharacter() => _screens.Change(new CharacterCreateScreen(_screens));

    private void OnDisconnected()
    {
        if (!_waitingWorld) return;
        _status = "Error: could not connect to world server";
        _busy = false;
        _waitingWorld = false;
    }
}
