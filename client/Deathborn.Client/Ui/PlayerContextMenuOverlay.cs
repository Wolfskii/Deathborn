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

        if (TinySwordsUi.IsLoaded)
        {
            TinySwordsUi.DrawPanel(sb, _bounds, TinySwordsUi.PanelKind.Paper);
            var titleRibbon = new Rectangle(_bounds.X + 6, _bounds.Y + 6, _bounds.Width - 12, 28);
            TinySwordsUi.DrawRibbon(sb, titleRibbon, TinySwordsUi.RibbonKind.Steel, pointed: false);
            var name = TargetName.Length > 16 ? TargetName[..13] + "..." : TargetName;
            sb.DrawString(font, SpriteFontSafe.Filter(name), new Vector2(titleRibbon.X + 10, titleRibbon.Y + 5),
                new Color(240, 235, 220), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            var y = _bounds.Y + Pad + 30;
            foreach (var item in _items)
            {
                var rect = new Rectangle(_bounds.X + 6, y, _bounds.Width - 12, ItemHeight - 2);
                _itemRects.Add(rect);
                var hover = item.Enabled && rect.Contains(mouse);
                if (hover)
                    TinySwordsUi.DrawRibbon(sb, rect, TinySwordsUi.RibbonKind.Gold, pointed: false, 0.85f);
                sb.DrawString(font, item.Label, new Vector2(rect.X + 10, rect.Y + 7),
                    item.Enabled ? (hover ? Color.White : new Color(55, 42, 30)) : new Color(130, 120, 110));
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
}
