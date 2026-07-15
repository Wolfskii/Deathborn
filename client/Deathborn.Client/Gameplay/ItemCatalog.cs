namespace Deathborn.Client.Gameplay;

public enum ItemKind
{
    Consumable,
    Cosmetic,
    Key,
    Weapon,
}

public sealed class ItemInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public ItemKind Kind { get; init; } = ItemKind.Consumable;
    public int MaxStack { get; init; } = 20;
    public float Cooldown { get; init; }
    public int? Heal { get; init; }
    public float? ManaRestore { get; init; }
    public float? StaminaRestore { get; init; }
}

public static class ItemCatalog
{
    private static readonly Dictionary<string, ItemInfo> All = new()
    {
        ["bandage"] = new()
        {
            Id = "bandage", Name = "Bandage", MaxStack = 10, Cooldown = Config.BandageCooldown,
            Heal = Config.BandageTotalHeal,
            Description = "Heals 25 HP over 5 seconds when used from the hotbar.",
        },
        ["health_potion"] = new()
        {
            Id = "health_potion", Name = "Health Potion", MaxStack = 20, Cooldown = 3f, Heal = 35,
            Description = "Instantly restores 35 HP.",
        },
        ["mana_potion"] = new()
        {
            Id = "mana_potion", Name = "Mana Potion", MaxStack = 20, Cooldown = 2f, ManaRestore = 40f,
            Description = "Restores 40 mana.",
        },
        ["stamina_potion"] = new()
        {
            Id = "stamina_potion", Name = "Stamina Potion", MaxStack = 20, Cooldown = 2f, StaminaRestore = 45f,
            Description = "Restores 45 stamina.",
        },
        ["antidote"] = new()
        {
            Id = "antidote", Name = "Antidote", MaxStack = 10, Cooldown = 1f,
            Description = "Cures poison and minor toxins.",
        },
        ["house_key"] = new()
        {
            Id = "house_key", Name = "Homestead Key", Kind = ItemKind.Key, MaxStack = 1, Cooldown = 0f,
            Description = "Proves ownership of a homestead plot. Drop it on death - anyone can claim the plot by picking it up.",
        },
        ["farm_sword"] = new()
        {
            Id = "farm_sword", Name = "Iron Sword", Kind = ItemKind.Weapon, MaxStack = 1, Cooldown = 0f,
            Description = "A basic sword. Drag to the hotbar to slash foes.",
        },
    };

    static ItemCatalog()
    {
        foreach (var c in CosmeticCatalog.All)
            All[c.Id] = c;
    }

    public static ItemInfo? Get(string id) => All.GetValueOrDefault(id);

    public static IReadOnlyCollection<ItemInfo> AllItems => All.Values;

    public static bool IsCosmetic(string? id) =>
        id != null && All.TryGetValue(id, out var info) && info.Kind == ItemKind.Cosmetic;

    public static bool IsWeapon(string? id) =>
        id != null && All.TryGetValue(id, out var info) && info.Kind == ItemKind.Weapon;

    public static bool IsConsumable(string? id) =>
        id != null && All.TryGetValue(id, out var info) && info.Kind == ItemKind.Consumable;

    public static Dictionary<string, object> ToHotbarEntry(string itemId, int inventorySlot = -1)
    {
        var info = Get(itemId) ?? throw new ArgumentException($"Unknown item: {itemId}");
        if (info.Kind == ItemKind.Cosmetic)
        {
            var entry = new Dictionary<string, object>
            {
                [HotbarEntry.IdKey] = itemId,
                ["name"] = info.Name,
                ["kind"] = "cosmetic",
                ["itemId"] = itemId,
                ["fromInventory"] = true,
            };
            if (inventorySlot >= 0)
                entry[HotbarEntry.InventorySlotKey] = inventorySlot;
            return HotbarEntry.Clone(entry);
        }

        if (info.Kind == ItemKind.Weapon)
        {
            var slash = AbilityCatalog.Get("slash");
            var weapon = new Dictionary<string, object>
            {
                [HotbarEntry.IdKey] = "slash",
                ["name"] = info.Name,
                ["kind"] = "melee",
                ["itemId"] = itemId,
                ["fromInventory"] = true,
                [HotbarEntry.CooldownKey] = slash?.Cooldown ?? 0f,
            };
            if (inventorySlot >= 0)
                weapon[HotbarEntry.InventorySlotKey] = inventorySlot;
            return HotbarEntry.Clone(weapon);
        }

        var abilityId = itemId switch
        {
            "bandage" => "bandage",
            "health_potion" => "health_potion",
            "mana_potion" => "mana_potion",
            "stamina_potion" => "stamina_potion",
            _ => itemId,
        };
        var hotbar = new Dictionary<string, object>
        {
            [HotbarEntry.IdKey] = abilityId,
            ["name"] = info.Name,
            ["kind"] = "item",
            ["itemId"] = itemId,
            ["fromInventory"] = true,
            [HotbarEntry.CooldownKey] = info.Cooldown,
        };
        if (inventorySlot >= 0)
            hotbar[HotbarEntry.InventorySlotKey] = inventorySlot;
        return HotbarEntry.Clone(hotbar);
    }
}
