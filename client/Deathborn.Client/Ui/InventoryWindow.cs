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

    public event Action<int>? SlotClicked;

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

        if (mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed)
        {
            if (_pendingDragSlot is int clickedSlot)
            {
                var dx = mouse.X - _dragStartMouse.X;
                var dy = mouse.Y - _dragStartMouse.Y;
                if (dx * dx + dy * dy <= 36)
                    SlotClicked?.Invoke(clickedSlot);
            }
            _pendingDragSlot = null;
        }
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        if (_inventory == null) return;

        LayoutCells();
        var gridBounds = GridBounds();
        if (FarmRpgInventoryUi.IsLoaded)
            FarmRpgInventoryUi.DrawInventoryPanel(sb, gridBounds);
        sb.DrawString(font, "Drag items to hotbar or drop in world", new Vector2(area.X + 8, area.Y + 2), GoldDim);

        for (var i = 0; i < _cellRects.Count; i++)
        {
            var rect = _cellRects[i];
            var slot = _inventory.Slots[i];
            var hover = _hoverSlot == i;
            DrawCell(sb, rect, hover);

            if (slot.IsEmpty) continue;

            var icon = ItemCatalog.IsCosmetic(slot.ItemId)
                ? HotbarIconDraw.FitSquare(rect, top: 4, bottom: 4, horizontalPad: 4)
                : new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            HotbarIconDraw.Draw(sb, slot.ItemId, icon);

            if (slot.IsOnCooldown && slot.CooldownTotal > 0f)
                DrawCooldownOverlay(sb, font, rect, slot.CooldownRemaining, slot.CooldownTotal);

            if (slot.Count > 1)
            {
                var label = slot.Count.ToString();
                var size = font.MeasureString(label);
                var labelX = rect.Right - size.X - 4;
                var labelY = rect.Bottom - size.Y - 3;
                DrawPrimitives.FillRect(sb,
                    new Rectangle((int)labelX - 2, (int)labelY - 1, (int)size.X + 4, (int)size.Y + 2),
                    new Color(0, 0, 0, 0.62f));
                sb.DrawString(font, label, new Vector2(labelX, labelY), new Color(245, 240, 220));
            }
        }

        if (_dragDrop?.IsDragging != true
            && _hoverSlot is int hs && !_inventory.Slots[hs].IsEmpty)
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

    public bool TryGetSlotAt(Point p, out int index, out Rectangle rect)
    {
        LayoutCells();
        for (var i = 0; i < _cellRects.Count; i++)
        {
            if (!_cellRects[i].Contains(p)) continue;
            index = i;
            rect = _cellRects[i];
            return true;
        }
        index = -1;
        rect = Rectangle.Empty;
        return false;
    }

    private Rectangle GridBounds()
    {
        if (_cellRects.Count == 0) return Rectangle.Empty;
        var bounds = _cellRects[0];
        for (var i = 1; i < _cellRects.Count; i++)
            bounds = Rectangle.Union(bounds, _cellRects[i]);
        const int pad = 10;
        return new Rectangle(bounds.X - pad, bounds.Y - pad, bounds.Width + pad * 2, bounds.Height + pad * 2);
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
        if (FarmRpgInventoryUi.IsLoaded)
        {
            FarmRpgInventoryUi.DrawInventorySlot(sb, rect, hover);
            return;
        }

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

    private static void DrawCooldownOverlay(
        SpriteBatch sb, SpriteFont font, Rectangle bounds, float remaining, float total)
    {
        var progress = 1f - remaining / total;
        progress = MathHelper.Clamp(progress, 0f, 1f);

        var lineY = bounds.Bottom - progress * bounds.Height;
        var coverHeight = (int)MathF.Ceiling(lineY - bounds.Y);
        if (coverHeight > 0)
        {
            var cover = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, coverHeight);
            DrawPrimitives.FillRect(sb, cover, new Color(0, 0, 0, 0.42f));
        }

        var line = new Rectangle(bounds.X + 2, (int)lineY - 1, bounds.Width - 4, 2);
        DrawPrimitives.FillRect(sb, line, new Color(210, 185, 95, 0.85f));

        var label = FormatCooldownLabel(remaining);
        var size = font.MeasureString(label) * 0.65f;
        var textPos = new Vector2(bounds.Center.X - size.X / 2f, bounds.Center.Y - size.Y / 2f + 3f);
        DrawPrimitives.FillRect(sb,
            new Rectangle((int)textPos.X - 3, (int)textPos.Y - 1, (int)size.X + 6, (int)size.Y + 2),
            new Color(0, 0, 0, 0.5f));
        sb.DrawString(font, label, textPos, new Color(245, 240, 220),
            0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
    }

    private static string FormatCooldownLabel(float remaining) =>
        remaining >= 3f ? MathF.Ceiling(remaining).ToString("0")
        : remaining >= 0.05f ? remaining.ToString("0.0")
        : "0";
}
