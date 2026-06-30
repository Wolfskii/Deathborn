namespace Deathborn.Client.Gameplay;

public enum ResourceCostKind { None, Mana, Stamina }

public sealed class AbilityInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string Description { get; init; }
    public required string Page { get; init; }
    public float Cooldown { get; init; }
    public int? Damage { get; init; }
    public int? Heal { get; init; }
    public float? Range { get; init; }
    public ResourceCostKind CostKind { get; init; }
    public float Cost { get; init; }
    public string? ExtraStats { get; init; }
}

/// <summary>All player-learned abilities (currently every implemented ability).</summary>
public static class AbilityCatalog
{
    public static readonly string[] PageOrder = ["Warrior", "Arcane", "Support"];

    private static readonly Dictionary<string, AbilityInfo> All = Build();

    public static IReadOnlyList<AbilityInfo> ForPage(string page) =>
        All.Values.Where(a => a.Page == page).ToList();

    public static AbilityInfo? Get(string id) => All.GetValueOrDefault(id);

    public static Dictionary<string, object> ToHotbarEntry(string id)
    {
        var info = Get(id) ?? throw new ArgumentException($"Unknown ability: {id}");
        var entry = new Dictionary<string, object>
        {
            [HotbarEntry.IdKey] = info.Id,
            ["name"] = info.Name,
            ["kind"] = info.Kind,
            [HotbarEntry.CooldownKey] = info.Cooldown,
        };
        if (info.Id == "fireball")
            entry["projectileId"] = ProjectileDefinitions.Fireball.Id;
        return HotbarEntry.Clone(entry);
    }

    private static Dictionary<string, AbilityInfo> Build()
    {
        AbilityInfo A(
            string id, string name, string kind, string page, string desc,
            float cd, ResourceCostKind costKind = ResourceCostKind.None, float cost = 0,
            int? dmg = null, int? heal = null, float? range = null, string? extra = null) =>
            new()
            {
                Id = id, Name = name, Kind = kind, Page = page, Description = desc,
                Cooldown = cd, CostKind = costKind, Cost = cost,
                Damage = dmg, Heal = heal, Range = range, ExtraStats = extra,
            };

        return new Dictionary<string, AbilityInfo>
        {
            ["slash"] = A("slash", "Slash", "melee", "Warrior",
                "A quick sword swing in front of you.", 0f,
                ResourceCostKind.Stamina, 8,
                Config.SlashDamage, range: 44f),
            ["shield_bash"] = A("shield_bash", "Shield Bash", "melee", "Warrior",
                "Bash foes with your shield, stunning at close range.", Config.ShieldBashCooldown,
                ResourceCostKind.Stamina, 18, Config.ShieldBashDamage, range: Config.ShieldBashRange),
            ["whirlwind"] = A("whirlwind", "Whirlwind", "melee", "Warrior",
                "Spin and strike all nearby enemies.", Config.WhirlwindCooldown,
                ResourceCostKind.Stamina, 28, Config.WhirlwindDamage, range: Config.WhirlwindRadius,
                extra: "AoE"),
            ["warrior_dash"] = A("warrior_dash", "Charge", "melee", "Warrior",
                "Dash forward, damaging enemies you collide with.", Config.WarriorDashCooldown,
                ResourceCostKind.Stamina, 22, Config.WarriorDashDamage, range: Config.WarriorDashDistance),
            ["fireball"] = A("fireball", "Fireball", "spell", "Arcane",
                "Hurl a blazing orb that explodes on impact.", Config.FireballCooldown,
                ResourceCostKind.Mana, 22, Config.FireballDamage, range: Config.FireballMaxRange),
            ["ice_shard"] = A("ice_shard", "Ice Shard", "spell", "Arcane",
                "Launch a fast shard of ice.", Config.IceShardCooldown,
                ResourceCostKind.Mana, 14, Config.IceShardDamage, range: Config.IceShardMaxRange),
            ["arc_bolt"] = A("arc_bolt", "Arc Bolt", "spell", "Arcane",
                "A short-range lightning bolt in a cone.", Config.ArcBoltCooldown,
                ResourceCostKind.Mana, 12, Config.ArcBoltDamage, range: Config.ArcBoltRange),
            ["poison_cloud"] = A("poison_cloud", "Poison Cloud", "spell", "Arcane",
                "Release a toxic cloud around you.", Config.PoisonCloudCooldown,
                ResourceCostKind.Mana, 18, Config.PoisonCloudDamage, range: 90f, extra: "Ground AoE"),
            ["battle_shout"] = A("battle_shout", "Battle Shout", "buff", "Support",
                "Increase damage dealt for a short time.", Config.BattleShoutCooldown,
                ResourceCostKind.Stamina, 15, extra: $"+25% dmg · {Config.BattleShoutDuration:0}s"),
            ["iron_skin"] = A("iron_skin", "Iron Skin", "buff", "Support",
                "Harden your body, reducing damage taken.", Config.IronSkinCooldown,
                ResourceCostKind.Stamina, 20, extra: $"-30% taken · {Config.IronSkinDuration:0}s"),
            ["hunter_mark"] = A("hunter_mark", "Hunter's Mark", "utility", "Support",
                "Mark a foe so you can track them.", Config.HunterMarkCooldown,
                ResourceCostKind.Mana, 10, range: Config.HunterMarkRange,
                extra: $"{Config.HunterMarkDuration:0}s track"),
            ["bandage"] = A("bandage", "Bandage", "item", "Support",
                "Apply a bandage to heal over time.", Config.BandageCooldown,
                heal: Config.BandageTotalHeal, extra: $"{Config.BandageDuration:0}s HoT"),
            ["second_wind"] = A("second_wind", "Second Wind", "heal", "Support",
                "Draw on inner strength to instantly recover health.", Config.SecondWindCooldown,
                ResourceCostKind.Stamina, 25, heal: Config.SecondWindHeal),
        };
    }
}
