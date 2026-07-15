namespace Deathborn.Client.Gameplay;

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
    public float HealthCost { get; init; }
    public ResourceCostKind AltCostKind { get; init; }
    public float AltCost { get; init; }
    public string? ExtraStats { get; init; }
    /// <summary>When false (default), movement input is blocked for the ability duration.</summary>
    public bool CanMoveWhileUsing { get; init; }
    public AbilityEffectPlacement EffectPlacement { get; init; } = AbilityEffectPlacement.None;
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
            string id, string name, string kind, string page, string desc, float cd,
            ResourceCostKind costKind = ResourceCostKind.None, float cost = 0,
            float healthCost = 0,
            ResourceCostKind altKind = ResourceCostKind.None, float altCost = 0,
            int? dmg = null, int? heal = null, float? range = null, string? extra = null,
            bool canMoveWhileUsing = false,
            AbilityEffectPlacement effectPlacement = AbilityEffectPlacement.None) =>
            new()
            {
                Id = id, Name = name, Kind = kind, Page = page, Description = desc,
                Cooldown = cd, CostKind = costKind, Cost = cost,
                HealthCost = healthCost, AltCostKind = altKind, AltCost = altCost,
                Damage = dmg, Heal = heal, Range = range, ExtraStats = extra,
                CanMoveWhileUsing = canMoveWhileUsing,
                EffectPlacement = effectPlacement,
            };

        return new Dictionary<string, AbilityInfo>
        {
            ["slash"] = A("slash", "Slash", "melee", "Warrior",
                "A quick sword swing in front of you.", 0f,
                ResourceCostKind.Stamina, 8,
                dmg: Config.SlashDamage, range: 44f),
            ["shield_bash"] = A("shield_bash", "Shield Bash", "melee", "Warrior",
                "Bash foes with your shield.", Config.ShieldBashCooldown,
                ResourceCostKind.Stamina, 18, dmg: Config.ShieldBashDamage, range: Config.ShieldBashRange),
            ["whirlwind"] = A("whirlwind", "Whirlwind", "melee", "Warrior",
                "Spin and strike all nearby enemies.", Config.WhirlwindCooldown,
                ResourceCostKind.Stamina, 28, dmg: Config.WhirlwindDamage, range: Config.WhirlwindRadius,
                extra: "AoE", canMoveWhileUsing: true, effectPlacement: AbilityEffectPlacement.FollowOwner),
            ["warrior_dash"] = A("warrior_dash", "Charge", "melee", "Warrior",
                "Dash forward, damaging enemies you collide with.", Config.WarriorDashCooldown,
                ResourceCostKind.Stamina, 22, dmg: Config.WarriorDashDamage, range: Config.WarriorDashDistance,
                effectPlacement: AbilityEffectPlacement.FollowOwner),
            ["fireball"] = A("fireball", "Fireball", "spell", "Arcane",
                "Hurl a blazing orb that explodes on impact.", Config.FireballCooldown,
                ResourceCostKind.Mana, 22, dmg: Config.FireballDamage, range: Config.FireballMaxRange,
                effectPlacement: AbilityEffectPlacement.Projectile),
            ["ice_shard"] = A("ice_shard", "Ice Shard", "spell", "Arcane",
                "Launch a fast shard of ice.", Config.IceShardCooldown,
                ResourceCostKind.Mana, 14, dmg: Config.IceShardDamage, range: Config.IceShardMaxRange,
                effectPlacement: AbilityEffectPlacement.Projectile),
            ["arc_bolt"] = A("arc_bolt", "Arc Bolt", "spell", "Arcane",
                "A short-range lightning bolt in a cone.", Config.ArcBoltCooldown,
                ResourceCostKind.Mana, 12, dmg: Config.ArcBoltDamage, range: Config.ArcBoltRange,
                effectPlacement: AbilityEffectPlacement.WorldPlaced),
            ["blood_bolt"] = A("blood_bolt", "Blood Bolt", "spell", "Arcane",
                "Sacrifice life force to unleash a devastating bolt.", Config.BloodBoltCooldown,
                ResourceCostKind.Mana, 16, healthCost: 12,
                dmg: Config.BloodBoltDamage, range: Config.ArcBoltRange, extra: "Costs HP + mana",
                effectPlacement: AbilityEffectPlacement.WorldPlaced),
            ["poison_cloud"] = A("poison_cloud", "Poison Cloud", "spell", "Arcane",
                "Release a toxic cloud around you; strains the body.", Config.PoisonCloudCooldown,
                ResourceCostKind.Mana, 18, healthCost: 5,
                dmg: Config.PoisonCloudDamage, range: 90f, extra: "Ground AoE | costs HP",
                effectPlacement: AbilityEffectPlacement.WorldPlaced),
            ["battle_shout"] = A("battle_shout", "Battle Shout", "buff", "Support",
                "Increase damage dealt for a short time.", Config.BattleShoutCooldown,
                ResourceCostKind.Stamina, 15, extra: $"+25% dmg | {Config.BattleShoutDuration:0}s"),
            ["iron_skin"] = A("iron_skin", "Iron Skin", "buff", "Support",
                "Harden your body, reducing damage taken.", Config.IronSkinCooldown,
                ResourceCostKind.Stamina, 20, extra: $"-30% taken | {Config.IronSkinDuration:0}s"),
            ["hunter_mark"] = A("hunter_mark", "Hunter's Mark", "utility", "Support",
                "Mark a foe so you can track them.", Config.HunterMarkCooldown,
                ResourceCostKind.Mana, 10, altKind: ResourceCostKind.Stamina, altCost: 12,
                range: Config.HunterMarkRange, extra: "15 min track"),
            ["bandage"] = A("bandage", "Bandage", "item", "Support",
                "Apply a bandage to heal over time.", Config.BandageCooldown,
                ResourceCostKind.Stamina, 6,
                heal: Config.BandageTotalHeal, extra: $"{Config.BandageDuration:0}s HoT"),
            ["second_wind"] = A("second_wind", "Second Wind", "heal", "Support",
                "Draw on inner strength to recover health.", Config.SecondWindCooldown,
                ResourceCostKind.Stamina, 20, altKind: ResourceCostKind.Mana, altCost: 30,
                heal: Config.SecondWindHeal, extra: "Uses stamina, or mana if low"),
        };
    }
}
