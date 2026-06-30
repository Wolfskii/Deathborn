namespace Deathborn.Client.Gameplay;

public enum ResourceCostKind { None, Mana, Stamina, Health }

/// <summary>Spend and describe ability resource costs.</summary>
public static class AbilityResourceCosts
{
    public static bool CanAfford(CharacterStats stats, AbilityInfo info) =>
        TrySpend(stats, info, dryRun: true, out _);

    public static bool TrySpend(CharacterStats stats, AbilityInfo info, out string message) =>
        TrySpend(stats, info, dryRun: false, out message);

    private static bool TrySpend(CharacterStats stats, AbilityInfo info, bool dryRun, out string message)
    {
        message = "";

        if (info.HealthCost > 0 && stats.Hp <= info.HealthCost)
        {
            message = $"Not enough HP ({info.HealthCost:0} required).";
            return false;
        }

        var (kind, amount) = ResolveCost(stats, info);
        if (amount > 0 && !HasResource(stats, kind, amount))
        {
            message = kind switch
            {
                ResourceCostKind.Mana => $"Not enough mana ({amount:0} required).",
                ResourceCostKind.Stamina => $"Not enough stamina ({amount:0} required).",
                ResourceCostKind.Health => $"Not enough HP ({amount:0} required).",
                _ => "Cannot use ability.",
            };
            return false;
        }

        if (dryRun) return true;

        if (info.HealthCost > 0)
            stats.Hp = MathF.Max(1f, stats.Hp - info.HealthCost);

        if (amount > 0)
            Deduct(stats, kind, amount);

        return true;
    }

    private static (ResourceCostKind kind, float amount) ResolveCost(CharacterStats stats, AbilityInfo info)
    {
        if (info.Cost <= 0 || info.CostKind == ResourceCostKind.None)
            return (ResourceCostKind.None, 0);

        if (HasResource(stats, info.CostKind, info.Cost))
            return (info.CostKind, info.Cost);

        if (info.AltCostKind != ResourceCostKind.None && info.AltCost > 0
            && HasResource(stats, info.AltCostKind, info.AltCost))
            return (info.AltCostKind, info.AltCost);

        return (info.CostKind, info.Cost);
    }

    private static bool HasResource(CharacterStats stats, ResourceCostKind kind, float amount) => kind switch
    {
        ResourceCostKind.Mana => stats.Mana >= amount,
        ResourceCostKind.Stamina => stats.Stamina >= amount,
        ResourceCostKind.Health => stats.Hp > amount,
        _ => true,
    };

    private static void Deduct(CharacterStats stats, ResourceCostKind kind, float amount)
    {
        switch (kind)
        {
            case ResourceCostKind.Mana:
                stats.Mana = MathF.Max(0, stats.Mana - amount);
                break;
            case ResourceCostKind.Stamina:
                stats.Stamina = MathF.Max(0, stats.Stamina - amount);
                break;
            case ResourceCostKind.Health:
                stats.Hp = MathF.Max(1f, stats.Hp - amount);
                break;
        }
    }

    public static string FormatCostLine(AbilityInfo info)
    {
        var parts = new List<string>();
        if (info.Cost > 0 && info.CostKind != ResourceCostKind.None)
            parts.Add($"{FormatKind(info.CostKind)} {info.Cost:0}");
        if (info.HealthCost > 0)
            parts.Add($"HP {info.HealthCost:0}");
        if (info.AltCost > 0 && info.AltCostKind != ResourceCostKind.None)
            parts.Add($"or {FormatKind(info.AltCostKind)} {info.AltCost:0}");
        return parts.Count == 0 ? "No cost" : string.Join(" | ", parts);
    }

    public static string FormatKind(ResourceCostKind kind) => kind switch
    {
        ResourceCostKind.Mana => "Mana",
        ResourceCostKind.Stamina => "Stamina",
        ResourceCostKind.Health => "HP",
        _ => "",
    };
}
