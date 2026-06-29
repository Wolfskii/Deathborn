using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Deathborn.Client.Audio;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class LoginScreen : IScreen
{
    private static readonly Color BgBlack = new(0, 0, 0);
    private static readonly Color PanelFill = new(28, 24, 18);
    private static readonly Color PanelInner = new(18, 15, 12);
    private static readonly Color Gold = new(210, 170, 80);
    private static readonly Color GoldDim = new(130, 105, 55);
    private static readonly Color Tagline = new(200, 185, 140);
    private static readonly Color Status = new(220, 180, 120);
    private const string TaglineText = "You are born to die. Only skill decides when.";
    private const int TaglineGapBelowLogo = 12;
    private const int PanelGapBelowTagline = 48;

    private readonly ScreenManager _screens;
    private readonly TextField _email = new() { Placeholder = "Email" };
    private readonly TextField _password = new() { Placeholder = "Password", IsPassword = true };
    private readonly Checkbox _remember = new() { Label = "Remember email and password", Checked = true };
    private readonly Button _loginBtn = new() { Label = "Login" };
    private readonly Button _registerBtn = new() { Label = "Create account" };
    private string _status = "";
    private bool _busy;
    private KeyboardState _prevKb;
    private Point _mouse;
    private bool _waitingWorld;
    private MouseState _prevMouse;

    private Texture2D? _logo;
    private Song? _theme;
    private Rectangle _panel;
    private Rectangle _logoBounds;
    private int _taglineY;
    private readonly PixelBrazier _leftBrazier = new();
    private readonly PixelBrazier _rightBrazier = new();
    private int _leftBrazierX;
    private int _rightBrazierX;
    private int _brazierBottomY;

    public LoginScreen(ScreenManager screens) => _screens = screens;

    public void OnEnter()
    {
        var game = DeathbornGame.Instance;
        _logo ??= game.Content.Load<Texture2D>("Images/Logos/logo_no_text");
        _theme ??= game.Content.Load<Song>("Audio/Songs/The Reaper\u2019s Call");
        MusicPlayer.Play(_theme);

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
        MusicPlayer.Stop();
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
        _leftBrazier.Update(gameTime);
        _rightBrazier.Update(gameTime);

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

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;
        var cx = Config.Width / 2;

        sb.Begin();

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, Config.Width, Config.Height), BgBlack);

        if (_logo is not null)
            sb.Draw(_logo, _logoBounds, Color.White);

        var tagline = TaglineText;
        sb.DrawString(font, tagline,
            new Vector2(cx - font.MeasureString(tagline).X / 2, _taglineY),
            Tagline);

        _leftBrazier.Draw(sb, _leftBrazierX, _brazierBottomY);
        _rightBrazier.Draw(sb, _rightBrazierX, _brazierBottomY);

        DrawPanel(sb, _panel);

        var header = "Log in to Deathborn";
        sb.DrawString(font, header,
            new Vector2(cx - font.MeasureString(header).X / 2, _panel.Y + 14),
            Gold);

        _email.Draw(sb, font);
        _password.Draw(sb, font);
        _remember.Draw(sb, font, _remember.ContainsBox(_mouse));
        _loginBtn.Draw(sb, font, _loginBtn.Contains(_mouse));
        _registerBtn.Draw(sb, font, _registerBtn.Contains(_mouse));

        if (!string.IsNullOrEmpty(_status))
            sb.DrawString(font, _status, new Vector2(cx - 220, _panel.Bottom + 16), Status);

        sb.End();
    }

    private void Layout()
    {
        var cx = Config.Width / 2;

        if (_logo is not null)
        {
            const int logoDisplaySize = 220;
            var scale = logoDisplaySize / (float)Math.Max(_logo.Width, _logo.Height);
            var w = (int)(_logo.Width * scale);
            var h = (int)(_logo.Height * scale);
            _logoBounds = new Rectangle(cx - w / 2, 36, w, h);
        }
        else
        {
            _logoBounds = new Rectangle(cx - 110, 36, 220, 220);
        }

        _taglineY = _logoBounds.Bottom + TaglineGapBelowLogo;
        var taglineBottom = _taglineY + (int)DeathbornGame.Instance.Font.MeasureString(TaglineText).Y;
        _panel = new Rectangle(cx - 210, taglineBottom + PanelGapBelowTagline, 420, 248);
        _email.Bounds = new Rectangle(_panel.X + 30, _panel.Y + 52, 360, 36);
        _password.Bounds = new Rectangle(_panel.X + 30, _panel.Y + 102, 360, 36);
        _remember.BoxBounds = new Rectangle(_panel.X + 30, _panel.Y + 150, 20, 20);
        _loginBtn.Bounds = new Rectangle(_panel.X + 30, _panel.Y + 192, 170, 36);
        _registerBtn.Bounds = new Rectangle(_panel.X + 220, _panel.Y + 192, 170, 36);

        _leftBrazierX = _panel.X - 72;
        _rightBrazierX = _panel.Right + 72;
        _brazierBottomY = _panel.Bottom + 4;
    }

    private static void DrawPanel(SpriteBatch sb, Rectangle panel)
    {
        DrawPrimitives.FillRect(sb, panel, PanelFill);
        DrawBorder(sb, panel, Gold, 3);
        DrawBorder(sb, new Rectangle(panel.X + 6, panel.Y + 6, panel.Width - 12, panel.Height - 12), GoldDim, 1);
        DrawPrimitives.FillRect(sb, new Rectangle(panel.X + 8, panel.Y + 8, panel.Width - 16, panel.Height - 16), PanelInner);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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
