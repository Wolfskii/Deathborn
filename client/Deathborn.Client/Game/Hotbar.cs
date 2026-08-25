using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class HotbarSlot
{
    public string KeyLabel = "?";
    public Dictionary<string, object>? Entry;
    public bool Flash;
    public float CooldownRemaining;
    public float CooldownTotal;

    private float _flashT;

    public bool IsOnCooldown => CooldownRemaining > 0.001f;

    public void StartCooldown(float seconds)
    {
        if (seconds <= 0f) return;
        CooldownRemaining = seconds;
        CooldownTotal = seconds;
    }

    public void Update(float dt)
    {
        if (CooldownRemaining > 0f)
            CooldownRemaining = MathF.Max(0f, CooldownRemaining - dt);

        if (!Flash) return;
        _flashT += dt;
        if (_flashT > 0.2f) { Flash = false; _flashT = 0; }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Rectangle bounds, bool selected, PlayerInventory? inventory = null)
    {
        var onCooldown = IsOnCooldown;
        var active = Flash && !onCooldown;

        if (FarmRpgInventoryUi.IsLoaded)
            FarmRpgInventoryUi.DrawHotbarSlot(sb, bounds, active);
        else
        {
            var bg = active ? new Color(55, 70, 95) : new Color(20, 20, 26);
            DrawPrimitives.FillRect(sb, bounds, bg);

            var border = active ? new Color(190, 215, 255) : new Color(90, 95, 105);
            DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
            DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
            DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
            DrawPrimitives.FillRect(sb, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);
        }

        var iconPad = FarmRpgInventoryUi.IsLoaded ? 10 : 1;
        var icon = new Rectangle(bounds.X + iconPad, bounds.Y + iconPad,
            bounds.Width - iconPad * 2, bounds.Height - iconPad * 2);
        var spellId = Entry?.GetValueOrDefault("itemId") as string
            ?? Entry?.GetValueOrDefault(HotbarEntry.IdKey) as string;
        HotbarIconDraw.Draw(sb, spellId, icon);

        var keySize = font.MeasureString(KeyLabel);
        var keyBadge = new Rectangle(bounds.X + 2, bounds.Y + 2, (int)keySize.X + 7, (int)keySize.Y + 3);
        if (FarmRpgUi.IsLoaded)
            FarmRpgUi.DrawButton(sb, keyBadge);
        else
            DrawPrimitives.FillRect(sb, keyBadge, new Color(0, 0, 0, 0.62f));
        sb.DrawString(font, KeyLabel, new Vector2(bounds.X + 5, bounds.Y + 3),
            FarmRpgUi.IsLoaded ? FarmRpgUi.Ink : new Color(215, 215, 190));

        if (inventory != null && Entry != null)
        {
            var itemId = ResolveConsumableItemId(Entry);
            if (itemId != null)
            {
                var count = GetConsumableCount(Entry, inventory, itemId);
                if (count > 0)
                    DrawStackCount(sb, font, bounds, count);
            }
        }

        if (onCooldown && CooldownTotal > 0f)
            DrawCooldownOverlay(sb, font, bounds);
    }

    private static string? ResolveConsumableItemId(Dictionary<string, object> entry)
    {
        var itemId = entry.GetValueOrDefault("itemId") as string;
        if (itemId != null && ItemCatalog.IsConsumable(itemId))
            return itemId;

        var abilityId = entry.GetValueOrDefault(HotbarEntry.IdKey) as string;
        if (abilityId != null && ItemCatalog.IsConsumable(abilityId))
            return abilityId;

        return null;
    }

    private static int GetConsumableCount(Dictionary<string, object> entry, PlayerInventory inventory, string itemId)
    {
        if (entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
        {
            var slot = slotObj switch
            {
                int i => i,
                long l => (int)l,
                _ => -1,
            };
            if (slot >= 0) return inventory.CountAt(slot);
        }

        return inventory.CountOf(itemId);
    }

    private static void DrawStackCount(SpriteBatch sb, SpriteFont font, Rectangle bounds, int count)
    {
        var smallFont = DeathbornGame.Instance.FontSmall;
        var label = count.ToString();
        var size = smallFont.MeasureString(label);
        var labelX = bounds.Right - size.X - 3;
        var labelY = bounds.Bottom - size.Y - 2;
        DrawPrimitives.FillRect(sb,
            new Rectangle((int)labelX - 2, (int)labelY - 1, (int)size.X + 4, (int)size.Y + 2),
            new Color(0, 0, 0, 0.62f));
        sb.DrawString(smallFont, label, new Vector2(labelX, labelY), new Color(245, 240, 220));
    }

    private void DrawCooldownOverlay(SpriteBatch sb, SpriteFont font, Rectangle bounds)
    {
        var progress = 1f - CooldownRemaining / CooldownTotal;
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

        var smallFont = DeathbornGame.Instance.FontSmall;
        var label = FormatCooldownLabel(CooldownRemaining);
        var size = smallFont.MeasureString(label);
        var textPos = new Vector2(bounds.Center.X - size.X / 2f, bounds.Center.Y - size.Y / 2f + 4f);
        DrawPrimitives.FillRect(sb,
            new Rectangle((int)textPos.X - 3, (int)textPos.Y - 1, (int)size.X + 6, (int)size.Y + 2),
            new Color(0, 0, 0, 0.5f));
        sb.DrawString(smallFont, label, textPos, new Color(245, 240, 220));
    }

    private static string FormatCooldownLabel(float remaining) =>
        remaining >= 3f ? MathF.Ceiling(remaining).ToString("0")
        : remaining >= 0.05f ? remaining.ToString("0.0")
        : "0";
}

public sealed class Hotbar
{
    public const int SlotWidth = 60;
    public const int SlotHeight = 60;
    public const int SlotGap = 7;
    public const int BarPadding = 10;

    public static readonly string[] KeyLabels = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
    private static readonly Keys[] HotbarKeys =
    [
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5,
        Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0,
    ];

    public readonly HotbarSlot[] Slots = new HotbarSlot[10];
    public int SelectedIndex { get; private set; }
    public event Action<int, Dictionary<string, object>?>? SlotActivated;
    public event Action<int, float>? CooldownBlocked;

    public Hotbar()
    {
        for (var i = 0; i < 10; i++)
            Slots[i] = new HotbarSlot { KeyLabel = KeyLabels[i] };
    }

    public void SetSlot(int index, Dictionary<string, object>? entry) => Slots[index].Entry = entry;

    public void AssignSlot(int index, Dictionary<string, object>? entry)
    {
        Slots[index].Entry = entry == null ? null : HotbarEntry.Clone(entry);
        Slots[index].CooldownRemaining = 0;
        Slots[index].CooldownTotal = 0;
    }

    public static void GetBarLayout(out int x0, out int y)
    {
        var totalW = 10 * SlotWidth + 9 * SlotGap;
        x0 = GameViewport.Width / 2 - totalW / 2;
        y = GameViewport.Height - SlotHeight - 28;
    }

    public static Rectangle GetSlotBounds(int index)
    {
        GetBarLayout(out var x0, out var y);
        return new Rectangle(x0 + index * (SlotWidth + SlotGap), y, SlotWidth, SlotHeight);
    }

    public static bool TryGetSlotIndexAt(Point p, out int index)
    {
        GetBarLayout(out var x0, out var y);
        for (var i = 0; i < 10; i++)
        {
            var rect = new Rectangle(x0 + i * (SlotWidth + SlotGap), y, SlotWidth, SlotHeight);
            if (!rect.Contains(p)) continue;
            index = i;
            return true;
        }
        index = -1;
        return false;
    }

    public void SelectSlot(int index)
    {
        if (index < 0) index = 9;
        else if (index > 9) index = 0;
        SelectedIndex = index;
    }

    public void Update(float dt, KeyboardState kb, KeyboardState prevKb, MouseState mouse, MouseState prevMouse,
        bool acceptInput = true)
    {
        foreach (var slot in Slots) slot.Update(dt);
        if (!acceptInput) return;

        for (var i = 0; i < 10; i++)
        {
            if (!kb.IsKeyDown(HotbarKeys[i]) || prevKb.IsKeyDown(HotbarKeys[i])) continue;
            SelectSlot(i);
            if (ActivatesOnNumberKey(Slots[i].Entry))
                TryActivate(i);
        }

        var wheelDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (wheelDelta == 0) return;

        var steps = wheelDelta / 120;
        if (steps == 0)
            steps = wheelDelta > 0 ? 1 : -1;
        SelectSlot((SelectedIndex - steps + 10_000) % 10);
    }

    private static bool ActivatesOnNumberKey(Dictionary<string, object>? entry)
    {
        if (entry == null) return false;

        var itemId = entry.GetValueOrDefault("itemId") as string;
        if (itemId != null)
        {
            if (ItemCatalog.IsWeapon(itemId))
                return false;
            return ItemCatalog.IsConsumable(itemId);
        }

        // Spells/abilities assigned directly (not dragged from inventory).
        return true;
    }

    public bool TryActivate(int index)
    {
        var slot = Slots[index];
        if (slot.IsOnCooldown)
        {
            CooldownBlocked?.Invoke(index, slot.CooldownRemaining);
            return false;
        }

        slot.Flash = true;
        SlotActivated?.Invoke(index, slot.Entry);
        return true;
    }

    public void StartCooldown(int index, float seconds) => Slots[index].StartCooldown(seconds);

    public void Draw(SpriteBatch sb, SpriteFont font, PlayerInventory? inventory = null, int selectedIndex = 0)
    {
        var totalW = 10 * SlotWidth + 9 * SlotGap;
        var x0 = GameViewport.Width / 2 - totalW / 2;
        var y = GameViewport.Height - SlotHeight - 28;
        var barRect = new Rectangle(x0 - BarPadding, y - BarPadding + 6,
            totalW + BarPadding * 2, SlotHeight + BarPadding * 2 - 8);

        if (FarmRpgInventoryUi.IsLoaded)
            FarmRpgInventoryUi.DrawHotbarBar(sb, barRect);
        else
            DrawPrimitives.FillRect(sb, barRect, new Color(12, 15, 20, 220));

        for (var i = 0; i < 10; i++)
        {
            var rect = new Rectangle(x0 + i * (SlotWidth + SlotGap), y, SlotWidth, SlotHeight);
            Slots[i].Draw(sb, font, rect, i == selectedIndex, inventory);
        }
    }
}
