using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>
/// Generates an HSB palette variant once per source layer and selection. Dark
/// neutral outline pixels stay untouched while the source shade ramp supplies
/// the highlight/shadow value for the new color.
/// </summary>
public static class CharacterPaletteTextureCache
{
    private static readonly Dictionary<(Texture2D Source, CharacterPalette Palette), Texture2D> Cache = [];

    public static Texture2D Get(Texture2D source, CharacterPalette palette)
    {
        palette = palette.Clamp();
        if (palette == CharacterPalette.Identity)
            return source;
        if (Cache.TryGetValue((source, palette), out var cached))
            return cached;

        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = Recolor(pixels[i], palette);

        var recolored = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
        recolored.SetData(pixels);
        Cache[(source, palette)] = recolored;
        return recolored;
    }

    public static void Clear()
    {
        foreach (var texture in Cache.Values)
            texture.Dispose();
        Cache.Clear();
    }

    private static Color Recolor(Color color, CharacterPalette palette)
    {
        if (color.A == 0)
            return color;

        ToHsv(color, out _, out var saturation, out var value);
        // Keep almost-black neutral line work dark and crisp.
        if (value < 0.16f && saturation < 0.42f)
            return color;

        var targetSaturation = palette.Saturation / 100f;
        var targetValue = value * palette.Brightness / 100f;
        return FromHsv(palette.Hue, targetSaturation, targetValue, color.A);
    }

    private static void ToHsv(Color color, out float hue, out float saturation, out float value)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var delta = max - min;
        value = max;
        saturation = max <= 0f ? 0f : delta / max;
        hue = delta <= 0.0001f ? 0f : max == r
            ? 60f * (((g - b) / delta + 6f) % 6f)
            : max == g
                ? 60f * ((b - r) / delta + 2f)
                : 60f * ((r - g) / delta + 4f);
    }

    private static Color FromHsv(float hue, float saturation, float value, byte alpha)
    {
        hue = (hue % 360f + 360f) % 360f;
        saturation = MathHelper.Clamp(saturation, 0f, 1f);
        value = MathHelper.Clamp(value, 0f, 1f);
        var chroma = value * saturation;
        var x = chroma * (1f - MathF.Abs((hue / 60f) % 2f - 1f));
        var match = value - chroma;
        var (r, g, b) = hue switch
        {
            < 60f => (chroma, x, 0f),
            < 120f => (x, chroma, 0f),
            < 180f => (0f, chroma, x),
            < 240f => (0f, x, chroma),
            < 300f => (x, 0f, chroma),
            _ => (chroma, 0f, x),
        };
        return new Color(r + match, g + match, b + match, alpha / 255f);
    }
}
