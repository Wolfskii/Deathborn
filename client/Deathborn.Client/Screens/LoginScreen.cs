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
    private static readonly Color Status = new(220, 180, 120);
    private static readonly Color StatusError = new(255, 120, 100);
    private const int TopMargin = 20;
    private const int BottomReserve = 140;
    private const int MaxLogoWidth = 480;
    private const int PanelWidth = 420;
    private const int PanelHeight = 248;
    private const int PanelGapBelowLogo = 24;
    private const int MinPanelWidth = 380;
    private const float MinFormScale = 0.9f;

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

    private readonly AnimatedBannerSprite _banner = new();
    private Song? _theme;
    private Rectangle _panel;
    private Rectangle _logoBounds;
    private readonly PixelBrazier _leftBrazier = new();
    private readonly PixelBrazier _rightBrazier = new();
    private int _leftBrazierX;
    private int _rightBrazierX;
    private int _brazierBottomY;
    private int _viewportW;
    private int _viewportH;
    private readonly ReapersCallLyrics _lyrics = new();
    private readonly ClientUpdateOverlay _clientUpdate = new();

    public LoginScreen(ScreenManager screens) => _screens = screens;

    public void OnEnter()
    {
        var game = DeathbornGame.Instance;
        _banner.SetTexture(game.Content.Load<Texture2D>("Images/Logos/Banners/Banner V2 - animated"));
        _theme ??= game.Content.Load<Song>("Audio/Songs/The Reaper\u2019s Call");
        MusicPlayer.Play(_theme);
        _lyrics.Reset(_theme);

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

        _ = _clientUpdate.CheckOnLoginAsync();
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
        if (_viewportW != GameViewport.Width || _viewportH != GameViewport.Height)
            Layout();

        _banner.Update(gameTime);
        _leftBrazier.Update(gameTime);
        _rightBrazier.Update(gameTime);
        _lyrics.Update();

        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        _mouse = mouse.Position;

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            _clientUpdate.Update(_mouse, clicked: true);

        if (_clientUpdate.BlocksInput)
        {
            _prevKb = kb;
            _prevMouse = mouse;
            return;
        }

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
        var cx = GameViewport.Width / 2;

        sb.Begin(samplerState: SamplerState.PointClamp);

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), BgBlack);

        _banner.Draw(sb, _logoBounds);

        _leftBrazier.Draw(sb, _leftBrazierX, _brazierBottomY);
        _rightBrazier.Draw(sb, _rightBrazierX, _brazierBottomY);

        _lyrics.Draw(sb, font, GameViewport.Width, GameViewport.Height);

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
        {
            var statusColor = _status.StartsWith("Error:", StringComparison.Ordinal) ? StatusError : Status;
            var statusY = Math.Min(_panel.Bottom + 12, GameViewport.Height - BottomReserve + 10);
            sb.DrawString(font, _status, new Vector2(cx - _panel.Width / 2f, statusY), statusColor);
        }

        var versionLine =
            $"Client {ProtocolCompat.ClientRelease} (protocol {ProtocolCompat.Protocol})";
        var versionSize = font.MeasureString(versionLine) * 0.72f;
        sb.DrawString(font, versionLine,
            new Vector2(cx - versionSize.X / 2f, GameViewport.Height - versionSize.Y - 10),
            new Color(120, 110, 100), 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);

        _clientUpdate.Draw(sb, font);

        sb.End();
    }

    private void Layout()
    {
        _viewportW = GameViewport.Width;
        _viewportH = GameViewport.Height;
        var viewportWidth = GameViewport.Width;
        var viewportHeight = GameViewport.Height;
        var cx = viewportWidth / 2;

        // Keep the form legible, but allow it to shrink a little on narrow windows.
        var formScale = Math.Clamp(viewportWidth / 1280f, MinFormScale, 1f);
        var panelWidth = Math.Clamp((int)MathF.Round(PanelWidth * formScale), MinPanelWidth, PanelWidth);
        var panelHeight = (int)MathF.Round(PanelHeight * formScale);
        var panelGap = (int)MathF.Round(PanelGapBelowLogo * formScale);
        var stackBottom = viewportHeight - BottomReserve;
        var maxLogoHeight = Math.Max(96, stackBottom - TopMargin - panelGap - panelHeight);
        var maxLogoWidth = Math.Min(MaxLogoWidth, Math.Max(96, viewportWidth - 48));

        var logoAspect = _banner.Aspect;
        var logoWidth = Math.Min(maxLogoWidth, (int)MathF.Round(maxLogoHeight * logoAspect));
        var logoHeight = Math.Max(1, (int)MathF.Round(logoWidth / logoAspect));
        _logoBounds = new Rectangle(cx - logoWidth / 2, TopMargin, logoWidth, logoHeight);

        _panel = new Rectangle(cx - panelWidth / 2, _logoBounds.Bottom + panelGap, panelWidth, panelHeight);

        var inset = (int)MathF.Round(30 * formScale);
        var fieldHeight = Math.Max(32, (int)MathF.Round(36 * formScale));
        var checkboxSize = Math.Max(18, (int)MathF.Round(20 * formScale));
        var fieldWidth = _panel.Width - inset * 2;
        var buttonGap = (int)MathF.Round(20 * formScale);
        var buttonWidth = (fieldWidth - buttonGap) / 2;
        var emailY = _panel.Y + (int)MathF.Round(52 * formScale);
        var passwordY = _panel.Y + (int)MathF.Round(102 * formScale);
        var rememberY = _panel.Y + (int)MathF.Round(150 * formScale);
        var buttonY = _panel.Y + (int)MathF.Round(192 * formScale);

        _email.Bounds = new Rectangle(_panel.X + inset, emailY, fieldWidth, fieldHeight);
        _password.Bounds = new Rectangle(_panel.X + inset, passwordY, fieldWidth, fieldHeight);
        _remember.BoxBounds = new Rectangle(_panel.X + inset, rememberY, checkboxSize, checkboxSize);
        _loginBtn.Bounds = new Rectangle(_panel.X + inset, buttonY, buttonWidth, fieldHeight);
        _registerBtn.Bounds = new Rectangle(_panel.X + inset + buttonWidth + buttonGap, buttonY, buttonWidth, fieldHeight);

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
        _status = msg.StartsWith("Outdated", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("Incompatible", StringComparison.OrdinalIgnoreCase)
            || msg.StartsWith("Could not", StringComparison.OrdinalIgnoreCase)
            ? msg
            : "Error: " + msg;
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
