using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Active buffs as compact cards under the player HUD: icon, name, time left and a
/// thin bar. The description shows on hover. Drag any card to move the stack.
/// </summary>
public sealed class BuffBarOverlay
{
    private const int CardWidth = 220;
    private const int CardHeight = 52;
    private const int CardGap = 8;
    private const int Pad = 10;
    private const int IconSize = 32;
    private const int BarHeight = 6;

    private bool _dragging;
    private Point _dragOffset;
    private Point _origin = new((int)Config.BuffBarDefaultX, (int)Config.BuffBarDefaultY);
    private Point _mouse;

    private Rectangle StackBounds(int count) => count <= 0
        ? Rectangle.Empty
        : new Rectangle(_origin.X, _origin.Y, CardWidth, count * CardHeight + (count - 1) * CardGap);

    /// <returns>True when the cursor is over the cards (blocks world clicks).</returns>
    public bool Update(MouseState mouse, MouseState prevMouse, BuffTracker tracker)
    {
        _mouse = mouse.Position;
        var bounds = StackBounds(tracker.Active.Count);
        if (bounds.IsEmpty)
        {
            _dragging = false;
            return false;
        }

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
            && bounds.Contains(mouse.Position))
        {
            _dragging = true;
            _dragOffset = new Point(mouse.X - _origin.X, mouse.Y - _origin.Y);
        }

        if (_dragging)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var x = Math.Clamp(mouse.X - _dragOffset.X, 0, Math.Max(0, GameViewport.Width - bounds.Width));
                var y = Math.Clamp(mouse.Y - _dragOffset.Y, 0, Math.Max(0, GameViewport.Height - bounds.Height));
                _origin = new Point(x, y);
            }
            else _dragging = false;
        }

        return _dragging || bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, BuffTracker tracker)
    {
        var count = tracker.Active.Count;
        if (count == 0) return;

        ActiveBuff? hovered = null;
        var hoverRect = Rectangle.Empty;
        var y = _origin.Y;
        foreach (var buff in tracker.Active)
        {
            var card = new Rectangle(_origin.X, y, CardWidth, CardHeight);
            DrawCard(sb, font, buff, card);
            if (!_dragging && card.Contains(_mouse))
            {
                hovered = buff;
                hoverRect = card;
            }
            y += CardHeight + CardGap;
        }

        if (hovered != null)
        {
            var (name, desc, _) = BuffCatalog.Describe(hovered.Id);
            UiTooltip.Draw(sb, font, name, [desc, $"{UiText.Duration(hovered.Remaining)} remaining"],
                hoverRect, new Point(GameViewport.Width, GameViewport.Height));
        }
    }

    private static void DrawCard(SpriteBatch sb, SpriteFont font, ActiveBuff buff, Rectangle card)
    {
        var (name, _, tint) = BuffCatalog.Describe(buff.Id);
        FarmRpgUi.DrawCompactPanel(sb, card, 0.97f);

        var icon = new Rectangle(card.X + Pad, card.Y + (card.Height - IconSize) / 2, IconSize, IconSize);
        HotbarIconDraw.Draw(sb, buff.Id, icon);

        var textX = icon.Right + 10;
        var right = card.Right - Pad - 2;
        var top = card.Y + Pad - 1;
        var time = UiText.Duration(buff.Remaining);
        var timeW = UiText.Measure(font, time).X;
        UiText.Draw(sb, font, UiText.Truncate(font, name, right - textX - timeW - 8), new Vector2(textX, top), FarmRpgUi.Ink);
        UiText.DrawRight(sb, font, time, right, top, FarmRpgUi.Rust);

        var bar = new Rectangle(textX, card.Bottom - Pad - BarHeight, right - textX, BarHeight);
        var pct = buff.Duration > 0f ? buff.Remaining / buff.Duration : 0f;
        FarmRpgUi.DrawBar(sb, bar, pct, tint);
    }
}
