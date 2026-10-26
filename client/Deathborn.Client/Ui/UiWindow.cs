using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Draggable window: Farm RPG paper frame, a title plate on the top edge, a close
/// button, and an optional shortcut key. Subclasses draw inside <see cref="ContentBounds"/>.
/// </summary>
public abstract class UiWindow
{
    protected static readonly Color PanelBorder = FarmRpgUi.Ink;
    protected static readonly Color GoldDim = FarmRpgUi.InkMuted;

    /// <summary>Height of the strip at the top that holds the title and drags the window.</summary>
    private const int HeaderHeight = 52;
    private const int SidePadding = 20;
    private const int BottomPadding = 20;
    private const int TitleTop = 11;
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

    private Rectangle HeaderBar => new(Bounds.X, Bounds.Y, Bounds.Width, HeaderHeight);

    private Rectangle CloseButton
    {
        get
        {
            var size = FarmRpgUi.IconButtonSize;
            return new Rectangle(Bounds.Right - size.X - 14, Bounds.Y + TitleTop, size.X, size.Y);
        }
    }

    /// <summary>Area for window content, inside the frame and below the title.</summary>
    protected Rectangle ContentBounds => new(
        Bounds.X + SidePadding,
        Bounds.Y + HeaderHeight,
        Math.Max(1, Bounds.Width - SidePadding * 2),
        Math.Max(1, Bounds.Height - HeaderHeight - BottomPadding));

    /// <summary>Window height needed for <paramref name="contentHeight"/> pixels of content.</summary>
    protected static int HeightForContent(int contentHeight) => HeaderHeight + contentHeight + BottomPadding;

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

    /// <summary>Moves the window's top-left corner, kept inside the viewport.</summary>
    public void MoveTo(Point topLeft)
    {
        Bounds = new Rectangle(topLeft.X, topLeft.Y, Bounds.Width, Bounds.Height);
        ClampToViewport();
    }

    /// <summary>Changes the window height (content that grows or shrinks), keeping it on screen.</summary>
    protected void SetHeight(int height)
    {
        if (height == Bounds.Height) return;
        Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Math.Min(height, GameViewport.Height - ViewportPad * 2));
        ClampToViewport();
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

            if (HeaderBar.Contains(pos))
            {
                _dragging = true;
                _dragMouseOffset = new Point(pos.X - Bounds.X, pos.Y - Bounds.Y);
                OnBringToFront?.Invoke();
            }
            else if (Bounds.Contains(pos))
            {
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

        if (MeasureContentHeight(font) is int contentHeight)
            SetHeight(HeightForContent(contentHeight));

        FarmRpgUi.DrawWindowPanel(sb, Bounds);
        FarmRpgUi.DrawTitlePlate(sb, font, Title, Bounds.Center.X, Bounds.Y + TitleTop);

        var close = CloseButton;
        FarmRpgUi.DrawCloseButton(sb, close, close.Contains(Mouse.GetState().Position));

        DrawContent(sb, font, ContentBounds);
        DrawOverlay(sb, font);
    }

    /// <summary>Content height for windows that size themselves to their content; null keeps the fixed height.</summary>
    protected virtual int? MeasureContentHeight(SpriteFont font) => null;

    protected abstract void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle contentArea);

    /// <summary>Drawn after the content, for hover tooltips that may extend past the window.</summary>
    protected virtual void DrawOverlay(SpriteBatch sb, SpriteFont font) { }

    protected static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    /// <summary>Height used by <see cref="DrawStatBar"/> for one labelled bar.</summary>
    protected static int StatBarHeight(SpriteFont font, int barHeight = 10) => font.LineSpacing + 4 + barHeight;

    /// <summary>Label on the left, value on the right, bar underneath.</summary>
    protected static void DrawStatBar(
        SpriteBatch sb, SpriteFont font, Rectangle area, string label, float current, float max, Color fill,
        string? valueText = null, int barHeight = 10)
    {
        UiText.Draw(sb, font, label, new Vector2(area.X, area.Y), FarmRpgUi.Ink);
        var text = valueText ?? $"{(int)current} / {(int)max}";
        UiText.DrawRight(sb, font, text, area.Right, area.Y, FarmRpgUi.InkMuted);

        var bar = new Rectangle(area.X, area.Y + font.LineSpacing + 4, area.Width, barHeight);
        FarmRpgUi.DrawBar(sb, bar, max > 0f ? current / max : 0f, fill);
    }

    protected static void DrawThemedButton(
        SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover, bool enabled = true, bool danger = false)
    {
        var pressed = hover && enabled;
        FarmRpgUi.DrawButton(sb, rect, pressed: pressed, disabled: !enabled, danger: danger);
        var textArea = pressed ? new Rectangle(rect.X, rect.Y + 2, rect.Width, rect.Height - 2) : rect;
        UiText.DrawCentered(sb, font, label, textArea, enabled ? FarmRpgUi.Ink : FarmRpgUi.InkFaint);
    }

    private void ClampToViewport()
    {
        var x = Math.Clamp(Bounds.X, 0, Math.Max(0, GameViewport.Width - Bounds.Width));
        var y = Math.Clamp(Bounds.Y, 0, Math.Max(0, GameViewport.Height - Bounds.Height));
        Bounds = new Rectangle(x, y, Bounds.Width, Bounds.Height);
    }
}
