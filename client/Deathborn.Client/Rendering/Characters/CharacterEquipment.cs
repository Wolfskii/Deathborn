namespace Deathborn.Client.Rendering.Characters;

public readonly struct CharacterEquipment
{
    /// <summary>Fun collectible hat overlay (procedural/icon) — not the equipment Helmet layer.</summary>
    public string? HeadCosmetic { get; init; }

    public string? ChestId { get; init; }
    public string? LegsId { get; init; }
    public string? BootsId { get; init; }
    public string? GlovesId { get; init; }
    public string? WeaponId { get; init; }
    public string? ShieldId { get; init; }
    public string? HelmetId { get; init; }
    public string? CapeId { get; init; }

    public static CharacterEquipment Empty => default;

    public static CharacterEquipment Starter => new()
    {
        ChestId = "starter-chest",
        LegsId = "starter-legs",
        BootsId = "starter-boots",
    };
}
