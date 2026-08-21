using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>
/// A bounded HSB adjustment persisted with a character. The resulting tint is
/// applied only to the appropriate modular sprite layer, never to equipment.
/// </summary>
public readonly record struct CharacterPalette(int Hue, int Saturation, int Brightness)
{
    public static CharacterPalette Identity => new(0, 0, 100);

    public CharacterPalette Clamp() => new(
        Math.Clamp(Hue, 0, 360),
        Math.Clamp(Saturation, 0, 100),
        Math.Clamp(Brightness, 0, 100));

    public Color ToTint()
    {
        var value = Clamp();
        var saturation = value.Saturation / 100f;
        var brightness = value.Brightness / 100f;
        var chroma = brightness * saturation;
        var hue = value.Hue / 60f;
        var x = chroma * (1f - MathF.Abs(hue % 2f - 1f));
        var (r, g, b) = hue switch
        {
            < 1f => (chroma, x, 0f),
            < 2f => (x, chroma, 0f),
            < 3f => (0f, chroma, x),
            < 4f => (0f, x, chroma),
            < 5f => (x, 0f, chroma),
            _ => (chroma, 0f, x),
        };
        var match = brightness - chroma;
        return new Color(r + match, g + match, b + match);
    }
}
