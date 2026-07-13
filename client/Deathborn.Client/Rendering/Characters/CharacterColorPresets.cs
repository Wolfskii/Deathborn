using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

public enum SkinTone
{
    Fair,
    Tan,
    Olive,
    Dark,
}

public enum EyeColor
{
    Brown,
    Blue,
    Green,
    Gray,
    Hazel,
}

public enum HairColor
{
    Brown,
    Black,
    Blonde,
    Red,
    White,
}

/// <summary>Canonical colors baked into body and hair layer art — remapped at runtime.</summary>
public static class CharacterColorPresets
{
    public static readonly Color CanonicalSkin = new(216, 180, 140);
    public static readonly Color CanonicalSkinShadow = new(176, 132, 96);
    public static readonly Color CanonicalEyeWhite = new(240, 240, 240);
    public static readonly Color CanonicalEyeIris = new(60, 80, 120);

    public static readonly Color CanonicalHairBase = new(80, 55, 40);
    public static readonly Color CanonicalHairHighlight = new(110, 80, 60);
    public static readonly Color CanonicalHairShadow = new(50, 35, 25);

    public static Color SkinBase(SkinTone tone) => tone switch
    {
        SkinTone.Fair => CanonicalSkin,
        SkinTone.Tan => new Color(194, 142, 102),
        SkinTone.Olive => new Color(168, 128, 88),
        SkinTone.Dark => new Color(120, 84, 58),
        _ => CanonicalSkin,
    };

    public static Color SkinShadow(SkinTone tone) => tone switch
    {
        SkinTone.Fair => CanonicalSkinShadow,
        SkinTone.Tan => new Color(150, 104, 72),
        SkinTone.Olive => new Color(132, 96, 64),
        SkinTone.Dark => new Color(88, 58, 40),
        _ => CanonicalSkinShadow,
    };

    public static Color EyeIris(EyeColor color) => color switch
    {
        EyeColor.Brown => new Color(60, 80, 120),
        EyeColor.Blue => new Color(72, 128, 196),
        EyeColor.Green => new Color(56, 128, 72),
        EyeColor.Gray => new Color(120, 128, 140),
        EyeColor.Hazel => new Color(128, 104, 56),
        _ => CanonicalEyeIris,
    };

    public static (Color Base, Color Highlight, Color Shadow) Hair(HairColor color) => color switch
    {
        HairColor.Black => (new Color(36, 32, 30), new Color(58, 54, 50), new Color(20, 18, 16)),
        HairColor.Blonde => (new Color(196, 156, 72), new Color(228, 196, 108), new Color(148, 112, 48)),
        HairColor.Red => (new Color(148, 56, 40), new Color(184, 84, 56), new Color(104, 36, 28)),
        HairColor.White => (new Color(220, 220, 228), new Color(244, 244, 248), new Color(176, 176, 188)),
        _ => (CanonicalHairBase, CanonicalHairHighlight, CanonicalHairShadow),
    };

    public static IReadOnlyList<(Color From, Color To)> BodyRemap(SkinTone skin, EyeColor eyes)
    {
        var iris = EyeIris(eyes);
        if (skin == SkinTone.Fair && eyes == EyeColor.Brown)
            return Array.Empty<(Color, Color)>();

        return
        [
            (CanonicalSkin, SkinBase(skin)),
            (CanonicalSkinShadow, SkinShadow(skin)),
            (CanonicalEyeIris, iris),
        ];
    }

    public static IReadOnlyList<(Color From, Color To)> HairRemap(HairColor color)
    {
        if (color == HairColor.Brown)
            return Array.Empty<(Color, Color)>();

        var (baseC, hi, shadow) = Hair(color);
        return
        [
            (CanonicalHairBase, baseC),
            (CanonicalHairHighlight, hi),
            (CanonicalHairShadow, shadow),
        ];
    }
}
