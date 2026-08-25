using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Draggable OS-style UI window with title bar, close button, and optional shortcut.</summary>
public abstract class UiWindow
{
    protected static readonly Color PanelFill = new(250, 181, 140);
    protected static readonly Color PanelBorder = FarmRpgUi.Ink;
    protected static readonly Color TitleFill = new(231, 140, 96);
    protected static readonly Color GoldDim = FarmRpgUi.InkMuted;

    private const int TitleBarHeight = 44;
    private const int CloseSize = 24;
    private const int Border = 8;
    private const int ViewportPad = 8;

    private bool _dragging;
    private Point _dragMouseOffset;
    private readonly Point _defaultPosition;
    private readonly Point _defaultSize;

    public string Title { get; }
    public Keys? ShortcutKey { get; }
    public bool IsOpen { get; private set; }
    public Rectangle Bounds { get; private set; }
    public Action? OnBringToFront;

    private Rectangle TitleBar => new(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
    private Rectangle CloseButton => new(
        Bounds.Right - CloseSize - 6,
        Bounds.Y + (TitleBarHeight - CloseSize) / 2,
        CloseSize,
        CloseSize);
    protected Rectangle ContentBounds => new(
        Bounds.X + Border,
        Bounds.Y + TitleBarHeight + 4,
        Math.Max(1, Bounds.Width - Border * 2),
        Math.Max(1, Bounds.Height - TitleBarHeight - Border - 4));

    protected UiWindow(string title, int width, int height, Keys? shortcutKey, Point defaultPosition)
    {
        Title = title;
        ShortcutKey = shortcutKey;
        _defaultPosition = defaultPosition;
        _defaultSize = new Point(width, height);
        Bounds = new Rectangle(defaultPosition.X, defaultPosition.Y, width, height);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        var width = Math.Min(_defaultSize.X, Math.Max(1, GameViewport.Width - ViewportPad * 2));
        var height = Math.Min(_defaultSize.Y, Math.Max(1, GameViewport.Height - ViewportPad * 2));
        Bounds = new Rectangle(_defaultPosition.X, _defaultPosition.Y, width, height);
        ClampToViewport();
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        _dragging = false;
    }

    /// <returns>True when this window wants mouse input (open and cursor over bounds).</returns>
    public bool Update(MouseState mouse, MouseState prevMouse, KeyboardState kb, KeyboardState prevKb, bool allowShortcut)
    {
        if (allowShortcut && ShortcutKey is Keys key
            && kb.IsKeyDown(key) && !prevKb.IsKeyDown(key))
        {
            Toggle();
        }

        if (!IsOpen)
            return false;

        var pos = mouse.Position;

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (CloseButton.Contains(pos))
            {
                Close();
                return true;
            }

            if (TitleBar.Contains(pos) && !CloseButton.Contains(pos))
            {
                _dragging = true;
                _dragMouseOffset = new Point(pos.X - Bounds.X, pos.Y - Bounds.Y);
                OnBringToFront?.Invoke();
            }
        }

        if (_dragging)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                Bounds = new Rectangle(pos.X - _dragMouseOffset.X, pos.Y - _dragMouseOffset.Y, Bounds.Width, Bounds.Height);
                ClampToViewport();
            }
            else
            {
                _dragging = false;
            }
        }

        UpdateContent(mouse, prevMouse);

        return Bounds.Contains(pos);
    }

    protected virtual void UpdateContent(MouseState mouse, MouseState prevMouse) { }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!IsOpen) return;

        if (FarmRpgUi.IsLoaded)
        {
            FarmRpgUi.DrawWindowPanel(sb, Bounds);
            var close = CloseButton;
            var ribbonW = Math.Max(80, close.X - Bounds.X - 20);
            var ribbon = new Rectangle(Bounds.X + 10, Bounds.Y + 8, ribbonW, 32);
            FarmRpgUi.DrawTitle(sb, ribbon);
            var titleSize = font.MeasureString(Title);
            var titlePos = new Vector2(
                ribbon.X + (ribbon.Width - titleSize.X) * 0.5f,
                ribbon.Y + (ribbon.Height - titleSize.Y) * 0.5f);
            sb.DrawString(font, Title, titlePos, FarmRpgUi.Ink);

            var hover = close.Contains(Mouse.GetState().Position);
            FarmRpgUi.DrawCloseButton(sb, close, hover);
            DrawContent(sb, font, ContentBounds);
            return;
        }

        DrawPrimitives.FillRect(sb, Bounds, PanelFill);
        DrawBorder(sb, Bounds, PanelBorder, Border);

        DrawPrimitives.FillRect(sb, TitleBar, TitleFill);
        DrawBorder(sb, TitleBar, GoldDim, 1);

        var legacyTitlePos = new Vector2(Bounds.X + 10, Bounds.Y + 6);
        sb.DrawString(font, Title, legacyTitlePos, PanelBorder);

        DrawCloseButton(sb, font, CloseButton, CloseButton.Contains(Mouse.GetState().Position));

        DrawContent(sb, font, ContentBounds);
    }

    private static void DrawCloseGlyph(SpriteBatch sb, Rectangle rect, bool hover)
    {
        var pad = Math.Max(5, rect.Width / 5);
        var x = rect.X + pad;
        var y = rect.Y + pad;
        var col = hover ? Color.White : new Color(255, 230, 220);
        var thick = Math.Max(1.5f, rect.Width / 14f);
        DrawPrimitives.DrawLine(sb, new Vector2(x, y), new Vector2(rect.Right - pad, rect.Bottom - pad), col, thick);
        DrawPrimitives.DrawLine(sb, new Vector2(rect.Right - pad, y), new Vector2(x, rect.Bottom - pad), col, thick);
    }

    private void DrawCloseButton(SpriteBatch sb, SpriteFont font, Rectangle rect, bool hover)
    {
        var bg = hover ? new Color(120, 45, 40) : new Color(55, 40, 38);
        DrawPrimitives.FillRect(sb, rect, bg);
        DrawBorder(sb, rect, hover ? new Color(240, 180, 120) : GoldDim, 1);

        var x = rect.X + 4;
        var y = rect.Y + 4;
        var col = Color.White;
        DrawPrimitives.DrawLine(sb, new Vector2(x, y), new Vector2(rect.Right - 4, rect.Bottom - 4), col, 2f);
        DrawPrimitives.DrawLine(sb, new Vector2(rect.Right - 4, y), new Vector2(x, rect.Bottom - 4), col, 2f);
    }

    protected static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    protected static void DrawStatBar(
        SpriteBatch sb, SpriteFont font, Rectangle area, string label, float current, float max, Color fill,
        string? valueText = null, int barHeight = 14)
    {
        sb.DrawString(font, label, new Vector2(area.X, area.Y), FarmRpgUi.Ink);

        var text = valueText ?? $"{(int)current}/{(int)max}";
        var size = font.MeasureString(text);
        sb.DrawString(font, text, new Vector2(area.Right - size.X, area.Y), FarmRpgUi.InkMuted);

        var barY = area.Y + font.LineSpacing + 2;
        var bar = new Rectangle(area.X, (int)barY, area.Width, barHeight);
        if (FarmRpgUi.IsLoaded)
        {
            FarmRpgUi.DrawBar(sb, bar, max > 0f ? current / max : 0f, fill);
            return;
        }

        DrawPrimitives.FillRect(sb, bar, new Color(18, 16, 14));
        DrawBorder(sb, bar, GoldDim, 1);

        var pct = max > 0f ? MathHelper.Clamp(current / max, 0f, 1f) : 0f;
        var fillW = (int)((bar.Width - 2) * pct);
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(bar.X + 1, bar.Y + 1, fillW, bar.Height - 2), fill);
    }

    protected abstract void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle contentArea);

    protected static void DrawThemedButton(
        SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover, bool enabled = true, bool danger = false)
    {
        if (FarmRpgUi.IsLoaded)
            FarmRpgUi.DrawButton(sb, rect, pressed: hover && enabled, disabled: !enabled, danger: danger);
        else
        {
            DrawPrimitives.FillRect(sb, rect,
                !enabled ? new Color(50, 45, 40) : hover ? new Color(90, 72, 38) : new Color(55, 45, 28));
            DrawBorder(sb, rect, GoldDim, 1);
        }

        var size = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - size.X) * 0.5f, rect.Y + (rect.Height - size.Y) * 0.5f),
            enabled ? FarmRpgUi.Ink : new Color(130, 105, 95));
    }

    private void ClampToViewport()
    {
        var x = Math.Clamp(Bounds.X, 0, Math.Max(0, GameViewport.Width - Bounds.Width));
        var y = Math.Clamp(Bounds.Y, 0, Math.Max(0, GameViewport.Height - Bounds.Height));
        Bounds = new Rectangle(x, y, Bounds.Width, Bounds.Height);
    }
}
