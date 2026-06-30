namespace Deathborn.Client.Gameplay;

public sealed class InventorySlot
{
    public string? ItemId;
    public int Count;
    public long HouseId;
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
    }

    public void ApplyFromServer(IReadOnlyList<Net.InventoryItemState>? items)
    {
        foreach (var slot in Slots)
        {
            slot.ItemId = null;
            slot.Count = 0;
            slot.HouseId = 0;
        }
        if (items == null) return;

        var idx = 0;
        foreach (var item in items)
        {
            if (idx >= SlotCount || item.Count <= 0) break;
            if (ItemCatalog.Get(item.ItemId) == null) continue;
            Slots[idx].ItemId = item.ItemId;
            Slots[idx].Count = item.Count;
            Slots[idx].HouseId = item.HouseId;
            idx++;
        }
    }

    public bool AddItem(string itemId, int count, long houseId = 0)
    {
        if (count <= 0) return false;
        var info = ItemCatalog.Get(itemId);
        if (info == null) return false;

        var remaining = count;
        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (Slots[i].ItemId != itemId || Slots[i].HouseId != houseId) continue;
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
            Slots[i].HouseId = houseId;
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
                Slots[i].HouseId = 0;
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

    public bool HasHouseKey() => CountOf("house_key") > 0;

    public long HouseKeyId()
    {
        foreach (var slot in Slots)
            if (slot.ItemId == "house_key" && slot.HouseId > 0)
                return slot.HouseId;
        return 0;
    }
}
