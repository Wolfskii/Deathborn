namespace Deathborn.Client.Gameplay;

/// <summary>Static data for a projectile spell or item ability.</summary>
public sealed class ProjectileDefinition
{
    public required string Id { get; init; }
    public float Radius { get; init; }
    public float Speed { get; init; }
    public float MaxRange { get; init; }
    public float BurstDuration { get; init; }
    public int Damage { get; init; }

    /// <summary>
    /// When true, mid-air contact with another flying projectile that also has this
    /// enabled cancels both (clash).
    /// </summary>
    public bool ClashWithProjectiles { get; init; } = true;
}

public static class ProjectileDefinitions
{
    public static readonly ProjectileDefinition Fireball = new()
    {
        Id = "fireball",
        Radius = Config.FireballRadius,
        Speed = Config.FireballSpeed,
        MaxRange = Config.FireballMaxRange,
        BurstDuration = Config.FireballBurstDuration,
        ClashWithProjectiles = true,
        Damage = Config.FireballDamage,
    };

    private static readonly Dictionary<string, ProjectileDefinition> ById = new(StringComparer.Ordinal)
    {
        [Fireball.Id] = Fireball,
    };

    public static ProjectileDefinition Get(string id) =>
        ById.TryGetValue(id, out var def) ? def : Fireball;
}
