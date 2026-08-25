namespace Deathborn.Client.Gameplay;

    public enum ItemKind
{
    Consumable,
    Cosmetic,
    Key,
    Weapon,
    Tool,
    Seed,
    Produce,
    Animal,
    Fish,
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
            Description = "A basic sword. Assign to the hotbar and attack with Space or click.",
        },
        ["hoe"] = new()
        {
            Id = "hoe", Name = "Hoe", Kind = ItemKind.Tool, MaxStack = 1, Cooldown = 0.25f,
            Description = "Till homestead soil, or harvest a ready crop. Click a yard tile.",
        },
        ["watering_can"] = new()
        {
            Id = "watering_can", Name = "Watering Can", Kind = ItemKind.Tool, MaxStack = 1, Cooldown = 0.2f,
            Description = "Water tilled soil so crops can grow. Crops pause when the soil dries.",
        },
        ["fishing_rod"] = new()
        {
            Id = "fishing_rod", Name = "Fishing Rod", Kind = ItemKind.Tool, MaxStack = 1, Cooldown = 0.4f,
            Description = "Cast from a shoreline into water. Keep the fish in the green bar to land it.",
        },
        ["worm_bait"] = new()
        {
            Id = "worm_bait", Name = "Worm Bait", Kind = ItemKind.Consumable, MaxStack = 40, Cooldown = 0f,
            Description = "Used automatically when you land a fish. Improves rare catches.",
        },
        ["animal_feed"] = new()
        {
            Id = "animal_feed", Name = "Animal Feed", Kind = ItemKind.Consumable, MaxStack = 40, Cooldown = 0.3f,
            Description = "Feed homestead animals to keep them producing.",
        },
        ["chicken"] = new()
        {
            Id = "chicken", Name = "Chicken", Kind = ItemKind.Animal, MaxStack = 1, Cooldown = 0.4f,
            Description = "Place in your homestead yard. Collect eggs when they are ready.",
        },
        ["cow"] = new()
        {
            Id = "cow", Name = "Cow", Kind = ItemKind.Animal, MaxStack = 1, Cooldown = 0.4f,
            Description = "Place in your homestead yard. Collect milk when ready.",
        },
        ["sheep"] = new()
        {
            Id = "sheep", Name = "Sheep", Kind = ItemKind.Animal, MaxStack = 1, Cooldown = 0.4f,
            Description = "Place in your homestead yard. Collect wool when ready.",
        },
        ["pig"] = new()
        {
            Id = "pig", Name = "Pig", Kind = ItemKind.Animal, MaxStack = 1, Cooldown = 0.4f,
            Description = "A friendly homestead pig. Place it in the yard and pet it.",
        },
        ["chicken_egg"] = new()
        {
            Id = "chicken_egg", Name = "Chicken Egg", Kind = ItemKind.Produce, MaxStack = 40, Cooldown = 0f,
            Description = "Collected from a homestead chicken.",
        },
        ["milk"] = new()
        {
            Id = "milk", Name = "Milk", Kind = ItemKind.Produce, MaxStack = 40, Cooldown = 0f,
            Description = "Collected from a homestead cow.",
        },
        ["wool"] = new()
        {
            Id = "wool", Name = "Wool", Kind = ItemKind.Produce, MaxStack = 40, Cooldown = 0f,
            Description = "Sheared from a homestead sheep.",
        },
    };

    static ItemCatalog()
    {
        foreach (var c in CosmeticCatalog.All)
            All[c.Id] = c;
        foreach (var crop in FarmCatalog.Crops.Values)
        {
            All[crop.SeedItem] = new ItemInfo
            {
                Id = crop.SeedItem,
                Name = crop.Name + " Seeds",
                Kind = ItemKind.Seed,
                MaxStack = 40,
                Cooldown = 0.2f,
                Description = $"Plant on tilled homestead soil. Grows into {crop.Name.ToLowerInvariant()}.",
            };
            All[crop.ProduceItem] = new ItemInfo
            {
                Id = crop.ProduceItem,
                Name = crop.Name,
                Kind = ItemKind.Produce,
                MaxStack = 40,
                Cooldown = 0f,
                Description = $"Harvested {crop.Name.ToLowerInvariant()} from your homestead.",
            };
        }
        foreach (var fish in FishCatalog.All)
        {
            All[fish.Id] = new ItemInfo
            {
                Id = fish.Id,
                Name = fish.Name,
                Kind = ItemKind.Fish,
                MaxStack = 40,
                Cooldown = 0f,
                Description = fish.Sea
                    ? $"A saltwater catch. Fishing level {fish.Level}."
                    : $"A freshwater catch. Fishing level {fish.Level}.",
            };
        }
        foreach (var recipe in CookCatalog.Recipes)
        {
            var parts = new List<string>();
            if (recipe.Heal > 0) parts.Add($"restores {recipe.Heal} HP");
            if (recipe.Stamina > 0) parts.Add($"+{recipe.Stamina} stamina");
            All[recipe.Result] = new ItemInfo
            {
                Id = recipe.Result,
                Name = recipe.Name,
                Kind = ItemKind.Consumable,
                MaxStack = 20,
                Cooldown = 2.5f,
                Heal = recipe.Heal > 0 ? recipe.Heal : null,
                StaminaRestore = recipe.Stamina > 0 ? recipe.Stamina : null,
                Description = $"Homestead cooking. {string.Join(", ", parts)}.",
            };
        }
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
            var weapon = new Dictionary<string, object>
            {
                [HotbarEntry.IdKey] = itemId,
                ["name"] = info.Name,
                ["kind"] = "weapon",
                ["itemId"] = itemId,
                ["fromInventory"] = true,
                [HotbarEntry.CooldownKey] = info.Cooldown,
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
