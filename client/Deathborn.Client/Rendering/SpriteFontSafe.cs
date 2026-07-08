using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>SpriteFont only includes printable ASCII; other chars crash DrawString.</summary>
public static class SpriteFontSafe
{
    public static string Filter(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is >= (char)32 and <= (char)126)
                sb.Append(c);
            else if (c is '\n' or '\r' or '\t')
                sb.Append(' ');
        }
        return sb.ToString();
    }

    public static Vector2 MeasureString(SpriteFont font, string? text) =>
        font.MeasureString(Filter(text));

    public static void DrawString(SpriteBatch sb, SpriteFont font, string? text, Vector2 position, Color color) =>
        sb.DrawString(font, Filter(text), position, color);

    public static void DrawString(
        SpriteBatch sb, SpriteFont font, string? text, Vector2 position, Color color,
        float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth) =>
        sb.DrawString(font, Filter(text), position, color, rotation, origin, scale, effects, layerDepth);

    /// <summary>Draw text with a dark outline for readability on busy backgrounds.</summary>
    public static void DrawOutlined(
        SpriteBatch sb, SpriteFont font, string? text, Vector2 position, Color fill, Color outline, float outlinePx = 1f)
    {
        var safe = Filter(text);
        if (safe.Length == 0) return;

        var o = MathF.Max(1f, outlinePx);
        sb.DrawString(font, safe, position + new Vector2(-o, 0), outline);
        sb.DrawString(font, safe, position + new Vector2(o, 0), outline);
        sb.DrawString(font, safe, position + new Vector2(0, -o), outline);
        sb.DrawString(font, safe, position + new Vector2(0, o), outline);
        sb.DrawString(font, safe, position + new Vector2(-o, -o), outline);
        sb.DrawString(font, safe, position + new Vector2(o, -o), outline);
        sb.DrawString(font, safe, position + new Vector2(-o, o), outline);
        sb.DrawString(font, safe, position + new Vector2(o, o), outline);
        sb.DrawString(font, safe, position, fill);
    }
}
