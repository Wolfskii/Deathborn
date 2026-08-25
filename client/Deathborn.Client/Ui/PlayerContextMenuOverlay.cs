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
    private const int ItemHeight = 32;
    private const int MinWidth = 168;
    private const int Pad = 8;

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

        if (FarmRpgUi.IsLoaded)
        {
            FarmRpgUi.DrawWindowPanel(sb, _bounds);
            var titleRibbon = new Rectangle(_bounds.X + 6, _bounds.Y + 6, _bounds.Width - 12, 28);
            FarmRpgUi.DrawTitle(sb, titleRibbon);
            var name = FitText(font, SpriteFontSafe.Filter(TargetName), titleRibbon.Width - 20);
            sb.DrawString(font, name, new Vector2(titleRibbon.X + 10, titleRibbon.Y + 5), FarmRpgUi.Ink);

            var y = _bounds.Y + Pad + 30;
            foreach (var item in _items)
            {
                var rect = new Rectangle(_bounds.X + 6, y, _bounds.Width - 12, ItemHeight - 2);
                _itemRects.Add(rect);
                var hover = item.Enabled && rect.Contains(mouse);
                FarmRpgUi.DrawButton(sb, rect, pressed: hover, disabled: !item.Enabled);
                sb.DrawString(font, item.Label, new Vector2(rect.X + 10, rect.Y + 7),
                    item.Enabled ? FarmRpgUi.Ink : FarmRpgUi.InkMuted * 0.65f);
                y += ItemHeight;
            }
            return;
        }

        DrawPrimitives.FillRect(sb, _bounds, new Color(28, 24, 18));
        var ly = _bounds.Y + Pad;
        foreach (var item in _items)
        {
            var rect = new Rectangle(_bounds.X + 2, ly, _bounds.Width - 4, ItemHeight);
            _itemRects.Add(rect);
            sb.DrawString(font, item.Label, new Vector2(rect.X + 8, rect.Y + 5), Color.White);
            ly += ItemHeight;
        }
    }

    private static string FitText(SpriteFont font, string text, int maxWidth)
    {
        if (font.MeasureString(text).X <= maxWidth) return text;
        const string suffix = "...";
        while (text.Length > 0 && font.MeasureString(text + suffix).X > maxWidth)
            text = text[..^1];
        return text + suffix;
    }
}
