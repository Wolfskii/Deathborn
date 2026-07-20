using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Draggable panel listing active buffs with countdown and descriptions.</summary>
public sealed class BuffBarOverlay
{
    private const int TitleHeight = 24;
    private const int RowHeight = 54;
    private const int RowGap = 6;
    private const int Pad = 8;
    private const int RowInnerPad = 8;
    private const int AccentWidth = 4;
    private const int BarHeight = 3;
    private const int BarGapAbove = 6;
    private const int Width = 248;

    private bool _dragging;
    private Point _dragOffset;
    private Rectangle _bounds;

    public BuffBarOverlay()
    {
        _bounds = new Rectangle((int)Config.BuffBarDefaultX, (int)Config.BuffBarDefaultY, Width, TitleHeight + Pad);
    }

    public bool Update(MouseState mouse, MouseState prevMouse, BuffTracker tracker)
    {
        var titleBar = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        var captures = false;

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
            && titleBar.Contains(mouse.Position))
        {
            _dragging = true;
            _dragOffset = new Point(mouse.X - _bounds.X, mouse.Y - _bounds.Y);
        }

        if (_dragging)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                _bounds = new Rectangle(mouse.X - _dragOffset.X, mouse.Y - _dragOffset.Y, _bounds.Width, _bounds.Height);
                ClampToViewport();
            }
            else _dragging = false;
            captures = true;
        }

        ResizeForBuffCount(tracker.Active.Count);
        return captures && _bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, BuffTracker tracker)
    {
        if (tracker.Active.Count == 0) return;

        ResizeForBuffCount(tracker.Active.Count);

        DrawPrimitives.FillRect(sb, _bounds, new Color(16, 18, 24, 210));
        UiWindowBorder(sb, _bounds, new Color(120, 130, 150));

        var titleBar = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        DrawPrimitives.FillRect(sb, titleBar, new Color(32, 36, 48, 230));
        sb.DrawString(font, "Buffs", new Vector2(_bounds.X + 8, _bounds.Y + 5), new Color(200, 205, 220));

        var y = _bounds.Y + TitleHeight + Pad;
        foreach (var buff in tracker.Active)
        {
            var row = new Rectangle(_bounds.X + Pad, y, _bounds.Width - Pad * 2, RowHeight);
            var (name, desc, tint) = BuffCatalog.Describe(buff.Id);

            DrawPrimitives.FillRect(sb, row, new Color(24, 26, 32, 200));
            DrawPrimitives.FillRect(sb, new Rectangle(row.X, row.Y, AccentWidth, row.Height), tint);

            var textLeft = row.X + AccentWidth + RowInnerPad;
            var textTop = row.Y + RowInnerPad;
            var time = FormatRemaining(buff.Remaining);
            var timeRight = row.Right - RowInnerPad;

            sb.DrawString(font, name, new Vector2(textLeft, textTop), Color.White);
            DrawFixedWidthTimer(sb, font, time, timeRight, textTop, new Color(240, 220, 140));

            var descY = textTop + font.LineSpacing + 2;
            sb.DrawString(font, desc, new Vector2(textLeft, descY), new Color(170, 175, 185));

            var barY = row.Bottom - RowInnerPad - BarHeight;
            // Keep the timer bar below description text (not through it).
            var minBarY = descY + font.LineSpacing + BarGapAbove;
            if (barY < minBarY)
                barY = minBarY;

            var bar = new Rectangle(textLeft, barY, row.Right - RowInnerPad - textLeft, BarHeight);
            DrawPrimitives.FillRect(sb, bar, new Color(40, 42, 50));
            var pct = buff.Duration > 0f ? buff.Remaining / buff.Duration : 0f;
            var fillW = (int)(bar.Width * MathHelper.Clamp(pct, 0f, 1f));
            if (fillW > 0)
                DrawPrimitives.FillRect(sb, new Rectangle(bar.X, bar.Y, fillW, bar.Height), tint * 0.85f);

            y += RowHeight + RowGap;
        }
    }

    /// <summary>Minutes:seconds for long buffs (e.g. 9:45); bare seconds under one minute.</summary>
    private static string FormatRemaining(float seconds)
    {
        seconds = MathF.Max(0f, seconds);
        if (seconds >= 60f)
        {
            var total = (int)MathF.Floor(seconds);
            var m = total / 60;
            var s = total % 60;
            return $"{m}:{s:00}";
        }

        return seconds >= 1f ? $"{seconds:0}s" : $"{seconds:0.0}s";
    }

    /// <summary>
    /// Draw timer glyphs in fixed-width cells anchored to the right so proportional
    /// digit widths (and 9:59 → 10:00) don't shift the label horizontally.
    /// </summary>
    private static void DrawFixedWidthTimer(
        SpriteBatch sb, SpriteFont font, string time, float rightX, float y, Color color)
    {
        EnsureDigitMetrics(font);
        var x = rightX;
        for (var i = time.Length - 1; i >= 0; i--)
        {
            var ch = time[i];
            var cell = ch is ':' or '.' or 's' ? _specialWidth : _digitWidth;
            x -= cell;
            var glyph = ch.ToString();
            var gw = font.MeasureString(glyph).X;
            sb.DrawString(font, glyph, new Vector2(x + (cell - gw) * 0.5f, y), color);
        }
    }

    private static float _digitWidth;
    private static float _specialWidth;
    private static SpriteFont? _metricsFont;

    private static void EnsureDigitMetrics(SpriteFont font)
    {
        if (ReferenceEquals(_metricsFont, font) && _digitWidth > 0f)
            return;

        _metricsFont = font;
        _digitWidth = 0f;
        for (var d = 0; d <= 9; d++)
            _digitWidth = MathF.Max(_digitWidth, font.MeasureString(d.ToString()).X);
        _digitWidth = MathF.Ceiling(_digitWidth);
        _specialWidth = MathF.Ceiling(MathF.Max(
            font.MeasureString(":").X,
            MathF.Max(font.MeasureString(".").X, font.MeasureString("s").X)));
    }

    private void ResizeForBuffCount(int count)
    {
        if (count <= 0) return;
        var h = TitleHeight + Pad + count * RowHeight + Math.Max(0, count - 1) * RowGap + Pad;
        _bounds = new Rectangle(_bounds.X, _bounds.Y, Width, h);
    }

    private void ClampToViewport()
    {
        var x = Math.Clamp(_bounds.X, 0, Math.Max(0, GameViewport.Width - _bounds.Width));
        var y = Math.Clamp(_bounds.Y, 0, Math.Max(0, GameViewport.Height - _bounds.Height));
        _bounds = new Rectangle(x, y, _bounds.Width, _bounds.Height);
    }

    private static void UiWindowBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        const int t = 1;
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, t), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, t, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), color);
    }
}
