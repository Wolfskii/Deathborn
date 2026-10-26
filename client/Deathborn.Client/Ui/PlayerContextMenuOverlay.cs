using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public enum PlayerContextAction
{
    AddFriend,
    Whisper,
}

public sealed class PlayerContextMenuOverlay
{
    private const int ItemHeight = 30;
    private const int MinWidth = 180;
    private const int Pad = 12;

    private readonly List<(PlayerContextAction Action, string Label, bool Enabled)> _items = [];
    private Rectangle _bounds;
    private readonly List<Rectangle> _itemRects = [];

    public bool IsOpen { get; private set; }
    public long TargetCharacterId { get; private set; }
    public string TargetName { get; private set; } = "";

    public event Action<PlayerContextAction, long, string>? ItemChosen;

    public void Open(Point screenPos, long characterId, string name, bool canAddFriend, bool canWhisper)
    {
        TargetCharacterId = characterId;
        TargetName = name;
        _items.Clear();
        if (canAddFriend)
            _items.Add((PlayerContextAction.AddFriend, "Add Friend", true));
        else
            _items.Add((PlayerContextAction.AddFriend, "Add Friend", false));
        _items.Add((PlayerContextAction.Whisper, "Whisper", canWhisper));

        var height = Pad * 2 + 30 + _items.Count * ItemHeight;
        var x = Math.Clamp(screenPos.X, 0, Math.Max(0, GameViewport.Width - MinWidth));
        var y = Math.Clamp(screenPos.Y, 0, Math.Max(0, GameViewport.Height - height));
        _bounds = new Rectangle(x, y, MinWidth, height);
        IsOpen = true;
    }

    public void Close() => IsOpen = false;

    public bool Update(MouseState mouse, MouseState prevMouse, bool blockInput)
    {
        if (!IsOpen)
            return false;

        if (blockInput)
        {
            Close();
            return false;
        }

        if (mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released)
        {
            Close();
            return true;
        }

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (!_bounds.Contains(mouse.Position))
            {
                Close();
                return false;
            }

            for (var i = 0; i < _itemRects.Count; i++)
            {
                if (!_itemRects[i].Contains(mouse.Position)) continue;
                var item = _items[i];
                if (item.Enabled)
                    ItemChosen?.Invoke(item.Action, TargetCharacterId, TargetName);
                Close();
                return true;
            }
        }

        return _bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!IsOpen) return;

        _itemRects.Clear();
        var mouse = Mouse.GetState().Position;

        // Size to the longest label (the target name may be long).
        var name = SpriteFontSafe.Filter(TargetName);
        var widest = UiText.Measure(font, name).X;
        foreach (var item in _items)
            widest = MathF.Max(widest, UiText.Measure(font, item.Label).X);
        var width = Math.Clamp((int)widest + Pad * 2 + 20, MinWidth, 280);
        var height = Pad * 2 + font.LineSpacing + 10 + _items.Count * ItemHeight;
        var x = Math.Clamp(_bounds.X, 0, Math.Max(0, GameViewport.Width - width));
        var y = Math.Clamp(_bounds.Y, 0, Math.Max(0, GameViewport.Height - height));
        _bounds = new Rectangle(x, y, width, height);

        FarmRpgUi.DrawCompactPanel(sb, _bounds);
        var inner = new Rectangle(_bounds.X + Pad, _bounds.Y + Pad, _bounds.Width - Pad * 2, _bounds.Height - Pad * 2);
        UiText.Draw(sb, font, UiText.Truncate(font, name, inner.Width), new Vector2(inner.X + 4, inner.Y), FarmRpgUi.Ink);
        var ruleY = inner.Y + font.LineSpacing + 4;
        DrawPrimitives.FillRect(sb, new Rectangle(inner.X, ruleY, inner.Width, 1), FarmRpgUi.InkFaint * 0.6f);

        var rowY = ruleY + 6;
        foreach (var item in _items)
        {
            var rect = new Rectangle(inner.X, rowY, inner.Width, ItemHeight);
            _itemRects.Add(rect);
            if (item.Enabled && rect.Contains(mouse))
                DrawPrimitives.FillRoundedRect(sb, rect, FarmRpgUi.PaperShade, 4);
            var textY = rect.Y + (rect.Height - font.LineSpacing) / 2f;
            UiText.Draw(sb, font, item.Label, new Vector2(rect.X + 8, textY),
                item.Enabled ? FarmRpgUi.Ink : FarmRpgUi.InkFaint);
            rowY += ItemHeight;
        }
    }
}
