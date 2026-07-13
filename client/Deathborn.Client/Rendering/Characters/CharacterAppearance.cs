namespace Deathborn.Client.Rendering.Characters;

public readonly struct CharacterAppearance
{
    public string BodyTypeId { get; init; }

    public static CharacterAppearance DefaultSwordsman => new()
    {
        BodyTypeId = CharacterAnimationCatalog.SwordsmanV2,
    };
}
