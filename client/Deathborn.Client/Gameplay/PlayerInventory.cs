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

        var nextLegacy = 0;
        foreach (var item in items)
        {
            if (item.Count <= 0 || ItemCatalog.Get(item.ItemId) == null) continue;

            var idx = item.Slot >= 0 && item.Slot < SlotCount ? item.Slot : FindNextEmpty(ref nextLegacy);
            if (idx < 0) break;

            Slots[idx].ItemId = item.ItemId;
            Slots[idx].Count = item.Count;
            Slots[idx].HouseId = item.HouseId;
        }
    }

    public bool MoveSlot(int from, int to)
    {
        if (from < 0 || from >= SlotCount || to < 0 || to >= SlotCount || from == to)
            return false;

        var src = Slots[from];
        if (src.IsEmpty) return false;

        var dst = Slots[to];
        if (dst.IsEmpty)
        {
            CopySlot(src, dst);
            ClearSlot(src);
            return true;
        }

        if (StacksMatch(src, dst))
        {
            var info = ItemCatalog.Get(src.ItemId!);
            if (info == null) return false;
            var space = info.MaxStack - dst.Count;
            if (space <= 0) return false;
            var move = Math.Min(space, src.Count);
            dst.Count += move;
            src.Count -= move;
            if (src.Count <= 0) ClearSlot(src);
            return true;
        }

        (Slots[from], Slots[to]) = (Slots[to], Slots[from]);
        return true;
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

    public bool Consume(string itemId, int count = 1) => ConsumeFromSlots(itemId, count, null);

    public bool ConsumeAt(int slotIndex, int count = 1)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return false;
        var slot = Slots[slotIndex];
        if (slot.IsEmpty) return false;
        return ConsumeFromSlots(slot.ItemId!, count, slotIndex);
    }

    public int CountAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return 0;
        return Slots[slotIndex].IsEmpty ? 0 : Slots[slotIndex].Count;
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

    private bool ConsumeFromSlots(string itemId, int count, int? onlySlot)
    {
        if (count <= 0) return false;
        var remaining = count;

        if (onlySlot is int slotIdx)
        {
            var slot = Slots[slotIdx];
            if (slot.IsEmpty || slot.ItemId != itemId) return false;
            var take = Math.Min(slot.Count, remaining);
            slot.Count -= take;
            remaining -= take;
            if (slot.Count <= 0) ClearSlot(slot);
            return remaining == 0;
        }

        for (var i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (Slots[i].ItemId != itemId) continue;
            var take = Math.Min(Slots[i].Count, remaining);
            Slots[i].Count -= take;
            remaining -= take;
            if (Slots[i].Count <= 0) ClearSlot(Slots[i]);
        }
        return remaining == 0;
    }

    private static bool StacksMatch(InventorySlot a, InventorySlot b) =>
        a.ItemId == b.ItemId && a.HouseId == b.HouseId;

    private static void CopySlot(InventorySlot from, InventorySlot to)
    {
        to.ItemId = from.ItemId;
        to.Count = from.Count;
        to.HouseId = from.HouseId;
    }

    private static void ClearSlot(InventorySlot slot)
    {
        slot.ItemId = null;
        slot.Count = 0;
        slot.HouseId = 0;
    }

    private int FindNextEmpty(ref int start)
    {
        for (var i = start; i < SlotCount; i++)
        {
            if (!Slots[i].IsEmpty) continue;
            start = i + 1;
            return i;
        }
        return -1;
    }
}
