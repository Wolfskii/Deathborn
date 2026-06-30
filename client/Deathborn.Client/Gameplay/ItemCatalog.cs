namespace Deathborn.Client.Gameplay;

public sealed class ItemInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
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
            Id = "house_key", Name = "Homestead Key", MaxStack = 1, Cooldown = 0f,
            Description = "Proves ownership of a homestead plot. Drop it on death — anyone can claim the plot by picking it up.",
        },
    };

    public static ItemInfo? Get(string id) => All.GetValueOrDefault(id);

    public static IReadOnlyCollection<ItemInfo> AllItems => All.Values;

    public static Dictionary<string, object> ToHotbarEntry(string itemId)
    {
        var info = Get(itemId) ?? throw new ArgumentException($"Unknown item: {itemId}");
        var abilityId = itemId switch
        {
            "bandage" => "bandage",
            "health_potion" => "health_potion",
            "mana_potion" => "mana_potion",
            "stamina_potion" => "stamina_potion",
            _ => itemId,
        };
        return HotbarEntry.Clone(new Dictionary<string, object>
        {
            [HotbarEntry.IdKey] = abilityId,
            ["name"] = info.Name,
            ["kind"] = "item",
            ["itemId"] = itemId,
            ["fromInventory"] = true,
            [HotbarEntry.CooldownKey] = info.Cooldown,
        });
    }
}
