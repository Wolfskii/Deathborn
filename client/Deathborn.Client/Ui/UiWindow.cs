using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Draggable OS-style UI window with title bar, close button, and optional shortcut.</summary>
public abstract class UiWindow
{
    protected static readonly Color PanelFill = new(28, 24, 18);
    protected static readonly Color PanelBorder = new(210, 170, 80);
    protected static readonly Color TitleFill = new(40, 34, 26);
    protected static readonly Color GoldDim = new(130, 105, 55);

    private const int TitleBarHeight = 28;
    private const int CloseSize = 20;
    private const int Border = 2;

    private bool _dragging;
    private Point _dragMouseOffset;

    public string Title { get; }
    public Keys? ShortcutKey { get; }
    public bool IsOpen { get; private set; }
    public Rectangle Bounds { get; private set; }
    public Action? OnBringToFront;

    private Rectangle TitleBar => new(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
    private Rectangle CloseButton => new(Bounds.Right - CloseSize - 4, Bounds.Y + 4, CloseSize, CloseSize);
    private Rectangle ContentArea => new(
        Bounds.X + Border,
        Bounds.Y + TitleBarHeight + Border,
        Bounds.Width - Border * 2,
        Bounds.Height - TitleBarHeight - Border * 2);

    protected UiWindow(string title, int width, int height, Keys? shortcutKey, Point defaultPosition)
    {
        Title = title;
        ShortcutKey = shortcutKey;
        Bounds = new Rectangle(defaultPosition.X, defaultPosition.Y, width, height);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open() => IsOpen = true;

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

        DrawPrimitives.FillRect(sb, Bounds, PanelFill);
        DrawBorder(sb, Bounds, PanelBorder, Border);

        DrawPrimitives.FillRect(sb, TitleBar, TitleFill);
        DrawBorder(sb, TitleBar, GoldDim, 1);

        var titlePos = new Vector2(Bounds.X + 10, Bounds.Y + 6);
        sb.DrawString(font, Title, titlePos, PanelBorder);

        DrawCloseButton(sb, font, CloseButton, CloseButton.Contains(Mouse.GetState().Position));

        DrawContent(sb, font, ContentArea);
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
        SpriteBatch sb, SpriteFont font, Rectangle area, string label, float current, float max, Color fill, string? valueText = null)
    {
        sb.DrawString(font, label, new Vector2(area.X, area.Y), new Color(210, 205, 195));

        var barY = area.Y + font.LineSpacing;
        var bar = new Rectangle(area.X, (int)barY, area.Width, 14);
        DrawPrimitives.FillRect(sb, bar, new Color(18, 16, 14));
        DrawBorder(sb, bar, GoldDim, 1);

        var pct = max > 0f ? MathHelper.Clamp(current / max, 0f, 1f) : 0f;
        var fillW = (int)((bar.Width - 2) * pct);
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(bar.X + 1, bar.Y + 1, fillW, bar.Height - 2), fill);

        var text = valueText ?? $"{(int)current}/{(int)max}";
        var size = font.MeasureString(text);
        sb.DrawString(font, text, new Vector2(bar.Right - size.X, area.Y), new Color(200, 200, 210));
    }

    protected abstract void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle contentArea);

    private void ClampToViewport()
    {
        var x = Math.Clamp(Bounds.X, 0, Math.Max(0, GameViewport.Width - Bounds.Width));
        var y = Math.Clamp(Bounds.Y, 0, Math.Max(0, GameViewport.Height - Bounds.Height));
        Bounds = new Rectangle(x, y, Bounds.Width, Bounds.Height);
    }
}
