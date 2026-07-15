using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG UI/Inventory sprites — hotbar slots and inventory panel.</summary>
public static class FarmRpgInventoryUi
{
    // Slots.png — hotbar slot frame + fills (224×288 sheet).
    public static readonly Rectangle HotbarSlotFrame = new(6, 6, 36, 31);
    public static readonly Rectangle HotbarSlotFillDark = new(151, 6, 18, 31);
    public static readonly Rectangle HotbarSlotFillLight = new(182, 6, 18, 31);
    public static readonly Rectangle HotbarSlotTileDark = new(151, 38, 18, 18);
    public static readonly Rectangle HotbarSlotTileLight = new(182, 38, 18, 18);
    public static readonly Rectangle HotbarBarEmpty = new(6, 105, 164, 28);
    public static readonly Rectangle HotbarSelectionOverlay = new(119, 6, 18, 18);

    // inventory.png — panel + slot tiles (112×112 sheet).
    public static readonly Rectangle InventoryPanel = new(0, 64, 48, 48);
    public static readonly Rectangle InventorySlotDark = new(7, 6, 18, 21);
    public static readonly Rectangle InventorySlotLight = new(7, 41, 18, 18);

    private static Texture2D? _slots;
    private static Texture2D? _inventory;

    public static bool IsLoaded => _slots != null && _inventory != null;

    public static void Load(ContentManager content)
    {
        _slots = content.Load<Texture2D>("Ui/FarmRpg/slots");
        _inventory = content.Load<Texture2D>("Ui/FarmRpg/inventory");
    }

    public static void DrawHotbarBar(SpriteBatch sb, Rectangle dest)
    {
        if (_slots == null) return;
        sb.Draw(_slots, dest, HotbarBarEmpty, Color.White);
    }

    public static void DrawHotbarSlot(SpriteBatch sb, Rectangle dest, bool active)
    {
        if (_slots == null) return;

        sb.Draw(_slots, dest, HotbarSlotFrame, Color.White);

        var inset = InsetForFrame(dest, 8, 6);
        var fill = active ? HotbarSlotTileLight : HotbarSlotTileDark;
        sb.Draw(_slots, inset, fill, Color.White);
    }

    public static void DrawHotbarSelection(SpriteBatch sb, Rectangle dest)
    {
        if (_slots == null) return;
        const int pad = 3;
        var overlay = new Rectangle(dest.X - pad, dest.Y - pad, dest.Width + pad * 2, dest.Height + pad * 2);
        sb.Draw(_slots, overlay, HotbarSelectionOverlay, Color.White);
    }

    public static void DrawInventoryPanel(SpriteBatch sb, Rectangle dest)
    {
        if (_inventory == null) return;
        sb.Draw(_inventory, dest, InventoryPanel, Color.White);
    }

    public static void DrawInventorySlot(SpriteBatch sb, Rectangle dest, bool hover)
    {
        if (_inventory == null) return;
        var src = hover ? InventorySlotLight : InventorySlotDark;
        sb.Draw(_inventory, dest, src, Color.White);
    }

    private static Rectangle InsetForFrame(Rectangle dest, int horizontalPad, int verticalPad)
    {
        var w = Math.Max(1, dest.Width - horizontalPad * 2);
        var h = Math.Max(1, dest.Height - verticalPad * 2);
        return new Rectangle(dest.X + horizontalPad, dest.Y + verticalPad, w, h);
    }
}
