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
    private const int TitleHeight = 22;
    private const int RowHeight = 36;
    private const int Pad = 6;
    private const int Width = 220;

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
        sb.DrawString(font, "Buffs", new Vector2(_bounds.X + 8, _bounds.Y + 4), new Color(200, 205, 220));

        var y = _bounds.Y + TitleHeight + Pad;
        foreach (var buff in tracker.Active)
        {
            var row = new Rectangle(_bounds.X + Pad, y, _bounds.Width - Pad * 2, RowHeight - 4);
            var (name, desc, tint) = BuffCatalog.Describe(buff.Id);
            DrawPrimitives.FillRect(sb, row, new Color(24, 26, 32, 200));
            DrawPrimitives.FillRect(sb, new Rectangle(row.X, row.Y, 4, row.Height), tint);

            sb.DrawString(font, name, new Vector2(row.X + 10, row.Y + 2), Color.White);
            sb.DrawString(font, desc, new Vector2(row.X + 10, row.Y + 16), new Color(170, 175, 185));

            var time = buff.Remaining >= 1f ? $"{buff.Remaining:0}s" : $"{buff.Remaining:0.0}s";
            var size = font.MeasureString(time);
            sb.DrawString(font, time, new Vector2(row.Right - size.X - 4, row.Y + 10), new Color(240, 220, 140));

            var pct = buff.Duration > 0f ? buff.Remaining / buff.Duration : 0f;
            var bar = new Rectangle(row.X + 10, row.Bottom - 4, row.Width - 20, 3);
            DrawPrimitives.FillRect(sb, bar, new Color(40, 42, 50));
            var fillW = (int)((bar.Width - 1) * MathHelper.Clamp(pct, 0f, 1f));
            if (fillW > 0)
                DrawPrimitives.FillRect(sb, new Rectangle(bar.X, bar.Y, fillW, bar.Height), tint * 0.85f);

            y += RowHeight;
        }
    }

    private void ResizeForBuffCount(int count)
    {
        if (count <= 0) return;
        var h = TitleHeight + Pad + count * RowHeight + Pad;
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
