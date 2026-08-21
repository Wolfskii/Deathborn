namespace Deathborn.Client.Rendering.Characters;

public readonly struct CharacterAppearance
{
    public string RaceId { get; init; }
    public string GenderId { get; init; }
    public string BodyTypeId { get; init; }
    public SkinTone SkinTone { get; init; }
    public EyeColor EyeColor { get; init; }
    public HairColor HairColor { get; init; }
    public string? HairStyleId { get; init; }
    public CharacterPalette SkinPalette { get; init; }
    public CharacterPalette EyePalette { get; init; }
    public CharacterPalette HairPalette { get; init; }

    public static CharacterAppearance DefaultFarmRpg => new()
    {
        RaceId = "human",
        GenderId = "male",
        BodyTypeId = CharacterAnimationCatalog.FarmRpg,
        SkinTone = SkinTone.Fair,
        EyeColor = EyeColor.Brown,
        HairColor = HairColor.Brown,
        HairStyleId = "farm-hair-josh-brown",
        SkinPalette = CharacterPalette.Identity,
        EyePalette = CharacterPalette.Identity,
        HairPalette = CharacterPalette.Identity,
    };

    public static CharacterAppearance DefaultPlayer => DefaultFarmRpg;
}
