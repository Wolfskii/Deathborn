namespace Deathborn.Client.Gameplay;

public sealed class InventorySlot
{
    public string? ItemId;
    public int Count;
    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
}

public sealed class PlayerInventory
{
    public const int SlotCount = 20;
    public const int Columns = 5;
    public const int CellSize = 44;
    public const int CellGap = 6;

    public readonly InventorySlot[] Slots = new InventorySlot[SlotCount];

    public PlayerInventory()
    {
        for (var i = 0; i < SlotCount; i++)
            Slots[i] = new InventorySlot();
        SeedStarterItems();
    }

    private void SeedStarterItems()
    {
        AddItem("bandage", 5);
        AddItem("health_potion", 3);
        AddItem("mana_potion", 2);
        AddItem("stamina_potion", 2);
        AddItem("antidote", 1);
    }

    public bool AddItem(string itemId, int count)
    {
        if (count <= 0) return false;
        var info = ItemCatalog.Get(itemId);
        if (info == null) return false;

        var remaining = count;
        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (Slots[i].ItemId != itemId) continue;
            var space = info.MaxStack - Slots[i].Count;
            if (space <= 0) continue;
            var add = Math.Min(space, remaining);
            Slots[i].Count += add;
            remaining -= add;
        }

        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (!Slots[i].IsEmpty) continue;
            var add = Math.Min(info.MaxStack, remaining);
            Slots[i].ItemId = itemId;
            Slots[i].Count = add;
            remaining -= add;
        }

        return remaining < count;
    }

    public bool Consume(string itemId, int count = 1)
    {
        if (count <= 0) return false;
        var remaining = count;
        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (Slots[i].ItemId != itemId) continue;
            var take = Math.Min(Slots[i].Count, remaining);
            Slots[i].Count -= take;
            remaining -= take;
            if (Slots[i].Count <= 0)
            {
                Slots[i].ItemId = null;
                Slots[i].Count = 0;
            }
        }
        return remaining == 0;
    }

    public int CountOf(string itemId)
    {
        var total = 0;
        foreach (var slot in Slots)
            if (slot.ItemId == itemId)
                total += slot.Count;
        return total;
    }

    public bool HasItem(string itemId, int count = 1) => CountOf(itemId) >= count;
}
