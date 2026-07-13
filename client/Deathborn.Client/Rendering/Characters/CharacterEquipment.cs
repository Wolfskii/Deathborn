namespace Deathborn.Client.Rendering.Characters;

public readonly struct CharacterEquipment
{
    public string? HeadCosmetic { get; init; }

    public static CharacterEquipment Empty => default;
}
