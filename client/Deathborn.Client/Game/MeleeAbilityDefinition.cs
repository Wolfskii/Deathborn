namespace Deathborn.Client.Gameplay;

/// <summary>Static data for a melee ability (e.g. sword slash).</summary>
public sealed class MeleeAbilityDefinition
{
    public required string Id { get; init; }
    public int Damage { get; init; }
    public float Range { get; init; } = 44f;
    public float HalfWidth { get; init; } = 24f;
    public int HitFrameStart { get; init; } = 2;
    public int HitFrameEnd { get; init; } = 5;
}

public static class MeleeAbilityDefinitions
{
    public static readonly MeleeAbilityDefinition Slash = new()
    {
        Id = "slash",
        Damage = Config.SlashDamage,
    };

    public static readonly MeleeAbilityDefinition ShieldBash = new()
    {
        Id = "shield_bash",
        Damage = Config.ShieldBashDamage,
        Range = Config.ShieldBashRange,
        HalfWidth = 28f,
        HitFrameStart = 2,
        HitFrameEnd = 4,
    };
}
