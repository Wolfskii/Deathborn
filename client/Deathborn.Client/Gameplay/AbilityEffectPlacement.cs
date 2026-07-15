namespace Deathborn.Client.Gameplay;

/// <summary>How a spell's world VFX is anchored after cast.</summary>
public enum AbilityEffectPlacement
{
    /// <summary>No persistent world effect (melee swings, buffs, etc.).</summary>
    None,
    /// <summary>Projectile that travels until max range or impact (fireball, ice shard).</summary>
    Projectile,
    /// <summary>Fixed at the cast position in world space (poison cloud, arc bolt).</summary>
    WorldPlaced,
    /// <summary>Stays centered on the caster while active (whirlwind).</summary>
    FollowOwner,
}
