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
        DrawOutlined(sb, font, text, position, fill, outline, scale: 1f, outlinePx);
    }

    public static void DrawOutlined(
        SpriteBatch sb, SpriteFont font, string? text, Vector2 position, Color fill, Color outline,
        float scale, float outlinePx = 1f, float alpha = 1f)
    {
        var safe = Filter(text);
        if (safe.Length == 0) return;

        fill *= alpha;
        outline *= alpha;
        var o = MathF.Max(1f, outlinePx);
        DrawOutlinePass(sb, font, safe, position, outline, scale, o);
        sb.DrawString(font, safe, position, fill, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        if (scale > 1.02f)
            sb.DrawString(font, safe, position + new Vector2(0.5f, 0f), fill * 0.55f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static void DrawOutlinePass(
        SpriteBatch sb, SpriteFont font, string safe, Vector2 position, Color outline, float scale, float o)
    {
        sb.DrawString(font, safe, position + new Vector2(-o, 0), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(o, 0), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(0, -o), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(0, o), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(-o, -o), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(o, -o), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(-o, o), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        sb.DrawString(font, safe, position + new Vector2(o, o), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
