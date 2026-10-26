using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

/// <summary>
/// Shared frame for the read-only record screens (past lives, leaderboard): a
/// titled panel, a scrollable content area clipped to its bounds, and a Back
/// button. Subclasses load data and draw the rows.
/// </summary>
public abstract class RecordsScreen : IScreen
{
    protected static readonly Color Gold = new(210, 170, 80);
    protected static readonly Color Muted = new(170, 165, 175);
    protected static readonly Color Dim = new(120, 115, 125);

    protected enum LoadState { Loading, Ready, Failed }

    private const int ScrollStep = 24;

    private static readonly RasterizerState ScissorState = new() { ScissorTestEnable = true };

    private readonly Func<IScreen> _back;
    private readonly Button _backBtn = new() { Label = "Back" };
    private MouseState _prevMouse;
    private int _scrollY;
    private Rectangle _panel;
    private Rectangle _content;

    protected RecordsScreen(ScreenManager screens, Func<IScreen> back)
    {
        Screens = screens;
        _back = back;
    }

    protected ScreenManager Screens { get; }
    protected Point MousePosition { get; private set; }

    protected abstract string Title { get; }
    protected virtual int HeaderHeight => 48;

    /// <summary>Total height of the scrollable rows for the current data.</summary>
    protected abstract int ContentHeight { get; }

    /// <summary>Loads the data. Called on enter and when a tab changes. Must not throw.</summary>
    protected abstract Task LoadAsync();

    /// <summary>Draws the rows. Only rows that pass <see cref="IsRowVisible"/> need drawing.</summary>
    protected abstract void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area, int scrollY);

    protected virtual void DrawHeader(SpriteBatch sb, SpriteFont font) { }

    /// <summary>Handles a click outside the Back button. Returns true when consumed.</summary>
    protected virtual bool HandleClick(Point p) => false;

    protected virtual void OnLayout(Rectangle panel) { }

    protected Rectangle Content => _content;

    protected void ResetScroll() => _scrollY = 0;

    protected static bool IsRowVisible(int y, int height, Rectangle area) =>
        y + height >= area.Top && y <= area.Bottom;

    protected static void DrawCentered(SpriteBatch sb, SpriteFont font, Rectangle area, string text)
    {
        var size = font.MeasureString(text);
        sb.DrawString(font, text, new Vector2(area.Center.X - size.X / 2f, area.Center.Y - size.Y / 2f), Muted);
    }

    /// <summary>Cuts text with "..." so it fits in maxWidth. Server text is filtered to the font's range.</summary>
    protected static string Truncate(SpriteFont font, string? text, float maxWidth)
    {
        var s = SpriteFontSafe.Filter(text);
        if (font.MeasureString(s).X <= maxWidth)
            return s;
        const string ellipsis = "...";
        while (s.Length > 0 && font.MeasureString(s + ellipsis).X > maxWidth)
            s = s[..^1];
        return s + ellipsis;
    }

    /// <summary>Renders seconds as "12 min" or "2h 5m".</summary>
    protected static string FormatSurvival(long seconds)
    {
        var minutes = seconds / 60;
        return minutes < 60 ? $"{minutes} min" : $"{minutes / 60}h {minutes % 60}m";
    }

    public void OnEnter()
    {
        ResetScroll();
        Layout();
        _ = LoadAsync();
    }

    public void OnExit() { }

    public void Update(GameTime gameTime)
    {
        Layout();
        var mouse = Mouse.GetState();
        MousePosition = mouse.Position;

        var wheel = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (wheel != 0 && _content.Contains(MousePosition))
            _scrollY = Math.Clamp(_scrollY - Math.Sign(wheel) * ScrollStep, 0, MaxScroll);

        var clicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        if (clicked)
        {
            if (_backBtn.Contains(MousePosition))
                GoBack();
            else
                HandleClick(MousePosition);
        }
        _prevMouse = mouse;
    }

    public bool HandleEscape()
    {
        GoBack();
        return true;
    }

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;

        sb.Begin();
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), Color.Black);
        if (FarmRpgUi.IsLoaded)
            FarmRpgUi.DrawWindowPanel(sb, _panel);
        else
            DrawPrimitives.FillRect(sb, _panel, new Color(28, 24, 18));

        var titleSize = font.MeasureString(Title);
        sb.DrawString(font, Title, new Vector2(_panel.Center.X - titleSize.X / 2f, _panel.Y + 14), Gold);
        DrawHeader(sb, font);
        DrawScrollbar(sb);
        _backBtn.Draw(sb, font, _backBtn.Contains(MousePosition));
        sb.End();

        // Rows are clipped to the content area so scrolled-off text never draws over the header or Back.
        var device = game.GraphicsDevice;
        var previousScissor = device.ScissorRectangle;
        device.ScissorRectangle = _content;
        sb.Begin(rasterizerState: ScissorState);
        DrawContent(sb, font, _content, _scrollY);
        sb.End();
        device.ScissorRectangle = previousScissor;
    }

    private int MaxScroll => Math.Max(0, ContentHeight - _content.Height);

    private void Layout()
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        var width = Math.Min(560, vw - 32);
        _panel = new Rectangle(vw / 2 - width / 2, 40, width, Math.Max(200, vh - 80));
        _content = new Rectangle(_panel.X + 16, _panel.Y + HeaderHeight, _panel.Width - 32,
            _panel.Height - HeaderHeight - 72);
        _backBtn.Bounds = new Rectangle(vw / 2 - 80, _panel.Bottom - 56, 160, 36);
        _scrollY = Math.Clamp(_scrollY, 0, MaxScroll);
        OnLayout(_panel);
    }

    private void DrawScrollbar(SpriteBatch sb)
    {
        if (MaxScroll == 0)
            return;
        var track = new Rectangle(_content.Right + 4, _content.Y, 6, _content.Height);
        var thumbHeight = Math.Max(24, track.Height * _content.Height / ContentHeight);
        var thumbY = track.Y + (int)((track.Height - thumbHeight) * (_scrollY / (float)MaxScroll));
        var thumb = new Rectangle(track.X, thumbY, track.Width, thumbHeight);
        if (FarmRpgUi.IsLoaded)
            FarmRpgUi.DrawScrollbar(sb, track, thumb);
        else
        {
            DrawPrimitives.FillRect(sb, track, new Color(40, 36, 30));
            DrawPrimitives.FillRect(sb, thumb, Gold);
        }
    }

    private void GoBack() => Screens.Change(_back());
}
