using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Text helpers for UI. Everything goes through <see cref="SpriteFontSafe"/> so
/// characters the font lacks (player names, culture separators) never crash a draw.
/// </summary>
public static class UiText
{
    public static Vector2 Measure(SpriteFont font, string? text) => SpriteFontSafe.MeasureString(font, text);

    public static void Draw(SpriteBatch sb, SpriteFont font, string? text, Vector2 position, Color color) =>
        SpriteFontSafe.DrawString(sb, font, text, Snap(position), color);

    /// <summary>Draws text right-aligned to <paramref name="rightX"/>.</summary>
    public static void DrawRight(SpriteBatch sb, SpriteFont font, string? text, float rightX, float y, Color color)
    {
        var size = Measure(font, text);
        Draw(sb, font, text, new Vector2(rightX - size.X, y), color);
    }

    /// <summary>Draws text centred in <paramref name="area"/>.</summary>
    public static void DrawCentered(SpriteBatch sb, SpriteFont font, string? text, Rectangle area, Color color)
    {
        var size = Measure(font, text);
        Draw(sb, font, text, new Vector2(area.X + (area.Width - size.X) / 2f, area.Y + (area.Height - size.Y) / 2f), color);
    }

    /// <summary>Splits text into lines that fit <paramref name="maxWidth"/>, breaking on spaces.</summary>
    public static List<string> Wrap(SpriteFont font, string? text, float maxWidth)
    {
        var lines = new List<string>();
        var safe = SpriteFontSafe.Filter(text);
        if (string.IsNullOrWhiteSpace(safe)) return lines;

        var line = "";
        foreach (var word in safe.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && font.MeasureString(candidate).X > maxWidth)
            {
                lines.Add(line);
                line = word;
            }
            else
            {
                line = candidate;
            }
        }
        if (line.Length > 0) lines.Add(line);
        return lines;
    }

    /// <summary>Shortens text with "..." so it fits <paramref name="maxWidth"/>.</summary>
    public static string Truncate(SpriteFont font, string? text, float maxWidth)
    {
        var s = SpriteFontSafe.Filter(text);
        if (font.MeasureString(s).X <= maxWidth) return s;
        const string ellipsis = "...";
        while (s.Length > 0 && font.MeasureString(s + ellipsis).X > maxWidth)
            s = s[..^1];
        return s + ellipsis;
    }

    /// <summary>Whole number with comma grouping, independent of the OS locale.</summary>
    public static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>Seconds as "4:17", or "45s" under a minute.</summary>
    public static string Duration(float seconds)
    {
        seconds = MathF.Max(0f, seconds);
        if (seconds >= 60f)
        {
            var total = (int)MathF.Floor(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
        return seconds >= 10f ? $"{seconds:0}s" : $"{MathF.Ceiling(seconds):0}s";
    }

    private static Vector2 Snap(Vector2 p) => new(MathF.Round(p.X), MathF.Round(p.Y));
}
