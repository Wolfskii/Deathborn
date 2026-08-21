using Deathborn.Client.Net;

namespace Deathborn.Client.Rendering.Characters;

public static class CharacterAppearanceDataMapper
{
    public static CharacterAppearance ToAppearance(string? race, CharacterAppearanceData? data)
    {
        data ??= new CharacterAppearanceData();
        return new CharacterAppearance
        {
            RaceId = CharacterCreationCatalog.NormalizeRace(race),
            GenderId = CharacterCreationCatalog.NormalizeGender(data.Gender),
            BodyTypeId = CharacterAnimationCatalog.FarmRpg,
            SkinTone = ParseSkinTone(data.SkinTone),
            EyeColor = ParseEyeColor(data.EyeColor),
            HairColor = HairColor.Brown,
            HairStyleId = CharacterCreationCatalog.NormalizeHairStyle(data.HairStyleId),
            SkinPalette = new CharacterPalette(data.SkinHue, data.SkinSaturation, data.SkinBrightness).Clamp(),
            EyePalette = new CharacterPalette(data.EyeHue, data.EyeSaturation, data.EyeBrightness).Clamp(),
            HairPalette = new CharacterPalette(data.HairHue, data.HairSaturation, data.HairBrightness).Clamp(),
        };
    }

    public static CharacterAppearanceData ToData(in CharacterAppearance appearance) => new()
    {
        Gender = CharacterCreationCatalog.NormalizeGender(appearance.GenderId),
        SkinTone = appearance.SkinTone.ToString().ToLowerInvariant(),
        EyeColor = appearance.EyeColor.ToString().ToLowerInvariant(),
        HairStyleId = CharacterCreationCatalog.NormalizeHairStyle(appearance.HairStyleId),
        SkinHue = appearance.SkinPalette.Clamp().Hue,
        SkinSaturation = appearance.SkinPalette.Clamp().Saturation,
        SkinBrightness = appearance.SkinPalette.Clamp().Brightness,
        EyeHue = appearance.EyePalette.Clamp().Hue,
        EyeSaturation = appearance.EyePalette.Clamp().Saturation,
        EyeBrightness = appearance.EyePalette.Clamp().Brightness,
        HairHue = appearance.HairPalette.Clamp().Hue,
        HairSaturation = appearance.HairPalette.Clamp().Saturation,
        HairBrightness = appearance.HairPalette.Clamp().Brightness,
    };

    private static SkinTone ParseSkinTone(string? value) =>
        Enum.TryParse<SkinTone>(value, true, out var tone) ? tone : SkinTone.Fair;

    private static EyeColor ParseEyeColor(string? value) =>
        Enum.TryParse<EyeColor>(value, true, out var color) ? color : EyeColor.Brown;
}
