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

    public static readonly ProjectileDefinition IceShard = new()
    {
        Id = "ice_shard",
        Radius = Config.IceShardRadius,
        Speed = Config.IceShardSpeed,
        MaxRange = Config.IceShardMaxRange,
        BurstDuration = Config.IceShardBurstDuration,
        ClashWithProjectiles = true,
        Damage = Config.IceShardDamage,
    };

    public static readonly ProjectileDefinition PoisonBolt = new()
    {
        Id = "poison_bolt",
        Radius = Config.PoisonBoltRadius,
        Speed = Config.PoisonBoltSpeed,
        MaxRange = Config.PoisonBoltMaxRange,
        BurstDuration = Config.PoisonBoltBurstDuration,
        ClashWithProjectiles = true,
        Damage = Config.PoisonBoltDamage,
    };

    private static readonly Dictionary<string, ProjectileDefinition> ById = new(StringComparer.Ordinal)
    {
        [Fireball.Id] = Fireball,
        [IceShard.Id] = IceShard,
        [PoisonBolt.Id] = PoisonBolt,
    };

    public static ProjectileDefinition Get(string id) =>
        ById.TryGetValue(id, out var def) ? def : Fireball;
}
