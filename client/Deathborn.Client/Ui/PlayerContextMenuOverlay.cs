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
    private const int ItemHeight = 28;
    private const int MinWidth = 148;
    private const int Pad = 6;

    private static readonly Color PanelFill = new(28, 24, 18);
    private static readonly Color PanelBorder = new(210, 170, 80);
    private static readonly Color ItemHover = new(55, 48, 36);
    private static readonly Color ItemText = new(235, 225, 200);
    private static readonly Color ItemDisabled = new(110, 105, 95);

    private readonly List<(PlayerContextAction Action, string Label, bool Enabled)> _items = [];
    private Rectangle _bounds;

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

        var height = Pad * 2 + _items.Count * ItemHeight;
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

            var localY = mouse.Y - _bounds.Y - Pad;
            var index = localY / ItemHeight;
            if (index >= 0 && index < _items.Count)
            {
                var item = _items[index];
                if (item.Enabled)
                    ItemChosen?.Invoke(item.Action, TargetCharacterId, TargetName);
                Close();
            }
            return true;
        }

        return _bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!IsOpen) return;

        DrawPrimitives.FillRect(sb, _bounds, PanelFill);
        DrawBorder(sb, _bounds, PanelBorder, 2);

        var mouse = Mouse.GetState().Position;
        var y = _bounds.Y + Pad;
        foreach (var item in _items)
        {
            var rect = new Rectangle(_bounds.X + 2, y, _bounds.Width - 4, ItemHeight);
            var hover = item.Enabled && rect.Contains(mouse);
            if (hover)
                DrawPrimitives.FillRect(sb, rect, ItemHover);
            sb.DrawString(font, item.Label, new Vector2(rect.X + 8, rect.Y + 5),
                item.Enabled ? (hover ? Color.White : ItemText) : ItemDisabled);
            y += ItemHeight;
        }
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
