using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class InventoryWindow : UiWindow
{
    private const int Width = 280;
    private const int Height = 340;

    private DragDropManager? _dragDrop;
    private PlayerInventory? _inventory;
    private int? _hoverSlot;
    private int? _pendingDragSlot;
    private Point _dragStartMouse;
    private readonly List<Rectangle> _cellRects = new(PlayerInventory.SlotCount);

    public InventoryWindow() : base("Inventory", Width, Height, Keys.I, new Point(80, 120)) { }

    public void Bind(DragDropManager dragDrop, PlayerInventory inventory)
    {
        _dragDrop = dragDrop;
        _inventory = inventory;
    }

    protected override void UpdateContent(MouseState mouse, MouseState prevMouse)
    {
        if (_dragDrop == null || _inventory == null) return;

        LayoutCells();
        if (_dragDrop.IsDragging) return;

        _hoverSlot = null;
        for (var i = 0; i < _cellRects.Count; i++)
        {
            if (!_cellRects[i].Contains(mouse.Position)) continue;
            _hoverSlot = i;
            break;
        }

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
            && _hoverSlot is int slot && !_inventory.Slots[slot].IsEmpty)
        {
            _pendingDragSlot = slot;
            _dragStartMouse = mouse.Position;
        }

        if (_pendingDragSlot is int pending && mouse.LeftButton == ButtonState.Pressed)
        {
            var dx = mouse.X - _dragStartMouse.X;
            var dy = mouse.Y - _dragStartMouse.Y;
            if (dx * dx + dy * dy > 36)
            {
                var itemId = _inventory.Slots[pending].ItemId;
                if (itemId != null)
                    _dragDrop.BeginItem(itemId, pending);
                _pendingDragSlot = null;
            }
        }

        if (mouse.LeftButton == ButtonState.Released)
            _pendingDragSlot = null;
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        if (_inventory == null) return;

        LayoutCells();
        sb.DrawString(font, "Drag items to your hotbar", new Vector2(area.X + 8, area.Y + 2), GoldDim);

        for (var i = 0; i < _cellRects.Count; i++)
        {
            var rect = _cellRects[i];
            var slot = _inventory.Slots[i];
            var hover = _hoverSlot == i;
            DrawCell(sb, rect, hover);

            if (slot.IsEmpty) continue;

            var icon = new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 14);
            HotbarIconDraw.Draw(sb, slot.ItemId, icon);

            if (slot.Count > 1)
            {
                var label = slot.Count.ToString();
                var size = font.MeasureString(label);
                sb.DrawString(font, label,
                    new Vector2(rect.Right - size.X - 3, rect.Bottom - size.Y - 1),
                    new Color(245, 240, 220));
            }
        }

        if (_hoverSlot is int hs && !_inventory.Slots[hs].IsEmpty)
        {
            var info = ItemCatalog.Get(_inventory.Slots[hs].ItemId!);
            if (info != null)
                AbilityTooltipDraw.DrawItem(sb, font, info, _cellRects[hs],
                    new Point(GameViewport.Width, GameViewport.Height));
        }
    }

    public bool TryGetSlotAt(Point p, out int index)
    {
        LayoutCells();
        for (var i = 0; i < _cellRects.Count; i++)
        {
            if (!_cellRects[i].Contains(p)) continue;
            index = i;
            return true;
        }
        index = -1;
        return false;
    }

    private void LayoutCells()
    {
        _cellRects.Clear();
        var cols = PlayerInventory.Columns;
        var rows = PlayerInventory.SlotCount / cols;
        var content = new Rectangle(Bounds.X + 12, Bounds.Y + 52, Bounds.Width - 24, Bounds.Height - 64);
        var gridW = cols * PlayerInventory.CellSize + (cols - 1) * PlayerInventory.CellGap;
        var x0 = content.X + (content.Width - gridW) / 2;
        var y0 = content.Y;

        for (var row = 0; row < rows; row++)
        for (var col = 0; col < cols; col++)
        {
            var x = x0 + col * (PlayerInventory.CellSize + PlayerInventory.CellGap);
            var y = y0 + row * (PlayerInventory.CellSize + PlayerInventory.CellGap);
            _cellRects.Add(new Rectangle(x, y, PlayerInventory.CellSize, PlayerInventory.CellSize));
        }
    }

    private static void DrawCell(SpriteBatch sb, Rectangle rect, bool hover)
    {
        DrawPrimitives.FillRect(sb, rect, hover ? new Color(38, 34, 28) : new Color(18, 16, 14));
        DrawBorder(sb, rect, hover ? PanelBorder : GoldDim);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
