using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

internal static class ChatBubbleDraw
{
    public static void DrawSpeech(SpriteBatch sb, SpriteFont font, Vector2 anchor, string text, float zoom, float alpha)
    {
        if (string.IsNullOrWhiteSpace(text) || alpha < 0.01f) return;

        var lines = WrapText(font, text, 160f * zoom);
        if (lines.Count == 0) return;

        var lineH = font.LineSpacing;
        var maxW = 0f;
        foreach (var line in lines)
            maxW = MathF.Max(maxW, font.MeasureString(line).X);

        var pad = 5f * zoom;
        var bubbleW = maxW + pad * 2;
        var bubbleH = lines.Count * lineH + pad * 2;
        var tailH = 5f * zoom;
        var x = anchor.X - bubbleW / 2f;
        var y = anchor.Y - bubbleH - tailH;

        var fill = new Color(0.96f, 0.94f, 0.88f, alpha);
        var border = new Color(0.2f, 0.18f, 0.16f, alpha);

        DrawPixelPanel(sb, x, y, bubbleW, bubbleH, fill, border, zoom);

        // Tail pointing down toward the player
        var tailW = 7f * zoom;
        var tailX = anchor.X - tailW / 2f;
        var tailY = y + bubbleH;
        DrawPrimitives.FillRect(sb, Rect(tailX, tailY, tailW, tailH), border);
        DrawPrimitives.FillRect(sb, Rect(tailX + zoom, tailY, tailW - 2f * zoom, tailH - zoom), fill);
        DrawPrimitives.FillRect(sb, Rect(anchor.X - zoom, tailY + tailH - zoom, 3f * zoom, 3f * zoom), fill);

        for (var i = 0; i < lines.Count; i++)
        {
            var pos = new Vector2(x + pad, y + pad + i * lineH);
            sb.DrawString(font, lines[i], pos, new Color(0.12f, 0.1f, 0.1f, alpha));
        }
    }

    private static void DrawPixelPanel(SpriteBatch sb, float x, float y, float w, float h, Color fill, Color border, float zoom)
    {
        var outer = Rect(x - zoom, y - zoom, w + 2f * zoom, h + 2f * zoom);
        DrawPrimitives.FillRect(sb, outer, border);
        DrawPrimitives.FillRect(sb, Rect(x, y, w, h), fill);
    }

    private static Rectangle Rect(float x, float y, float w, float h) =>
        new((int)MathF.Floor(x), (int)MathF.Floor(y), (int)MathF.Ceiling(w), (int)MathF.Ceiling(h));

    private static List<string> WrapText(SpriteFont font, string text, float maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            lines.Add(text.Length > 0 ? text : "");
            return lines;
        }

        var current = words[0];
        for (var i = 1; i < words.Length; i++)
        {
            var trial = current + " " + words[i];
            if (font.MeasureString(trial).X <= maxWidth)
                current = trial;
            else
            {
                lines.Add(current);
                current = words[i];
            }
        }
        lines.Add(current);
        return lines;
    }
}
