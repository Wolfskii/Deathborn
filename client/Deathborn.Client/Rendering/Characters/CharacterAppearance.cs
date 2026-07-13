namespace Deathborn.Client.Rendering.Characters;

public readonly struct CharacterAppearance
{
    public string BodyTypeId { get; init; }
    public SkinTone SkinTone { get; init; }
    public EyeColor EyeColor { get; init; }
    public HairColor HairColor { get; init; }
    public string? HairStyleId { get; init; }

    public static CharacterAppearance DefaultSwordsman => new()
    {
        BodyTypeId = CharacterAnimationCatalog.SwordsmanV2,
        SkinTone = SkinTone.Fair,
        EyeColor = EyeColor.Brown,
        HairColor = HairColor.Brown,
        HairStyleId = "hair-short",
    };
}
