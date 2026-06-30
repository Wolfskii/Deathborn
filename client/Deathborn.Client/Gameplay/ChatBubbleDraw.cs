using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

internal static class ChatBubbleDraw
{
    public static void DrawSpeech(SpriteBatch sb, SpriteFont font, Vector2 tailTarget, string text, float zoom, float alpha)
    {
        if (string.IsNullOrWhiteSpace(text) || alpha < 0.01f) return;

        var lines = WrapText(font, text, 160f * zoom);
        if (lines.Count == 0) return;

        var lineH = font.LineSpacing;
        var maxW = 0f;
        foreach (var line in lines)
            maxW = MathF.Max(maxW, font.MeasureString(SpriteFontSafe.Filter(line)).X);

        var pad = 6f * zoom;
        var bubbleW = maxW + pad * 2;
        var bubbleH = lines.Count * lineH + pad * 2;
        var gapAboveTarget = 10f * zoom;
        var x = tailTarget.X - bubbleW / 2f;
        var y = tailTarget.Y - bubbleH - gapAboveTarget;

        var fill = new Color(0.96f, 0.94f, 0.88f, alpha);
        var border = new Color(0.2f, 0.18f, 0.16f, alpha);

        DrawRoundedPanel(sb, x, y, bubbleW, bubbleH, fill, border, zoom);

        // Diagonal tail from the lower-right of the bubble toward the player
        var tailStart = new Vector2(x + bubbleW * 0.84f, y + bubbleH - 1f * zoom);
        var tailEnd = tailTarget + new Vector2(8f * zoom, 6f * zoom);
        DrawDiagonalTail(sb, tailStart, tailEnd, fill, border, zoom);

        for (var i = 0; i < lines.Count; i++)
        {
            var pos = new Vector2(x + pad, y + pad + i * lineH);
            sb.DrawString(font, SpriteFontSafe.Filter(lines[i]), pos, new Color(0.12f, 0.1f, 0.1f, alpha));
        }
    }

    private static void DrawRoundedPanel(SpriteBatch sb, float x, float y, float w, float h, Color fill, Color border, float zoom)
    {
        var r = 4f * zoom;
        DrawPrimitives.FillRect(sb, Rect(x + r, y, w - 2f * r, h), fill);
        DrawPrimitives.FillRect(sb, Rect(x, y + r, w, h - 2f * r), fill);
        DrawPrimitives.FillCircle(sb, new Vector2(x + r, y + r), r, fill, 12);
        DrawPrimitives.FillCircle(sb, new Vector2(x + w - r, y + r), r, fill, 12);
        DrawPrimitives.FillCircle(sb, new Vector2(x + r, y + h - r), r, fill, 12);
        DrawPrimitives.FillCircle(sb, new Vector2(x + w - r, y + h - r), r, fill, 12);

        var t = MathF.Max(1f, zoom);
        DrawPrimitives.FillRect(sb, Rect(x + r, y - t, w - 2f * r, t), border);
        DrawPrimitives.FillRect(sb, Rect(x + r, y + h, w - 2f * r, t), border);
        DrawPrimitives.FillRect(sb, Rect(x - t, y + r, t, h - 2f * r), border);
        DrawPrimitives.FillRect(sb, Rect(x + w, y + r, t, h - 2f * r), border);
    }

    private static void DrawDiagonalTail(
        SpriteBatch sb, Vector2 start, Vector2 end, Color fill, Color border, float zoom)
    {
        var dir = end - start;
        var len = dir.Length();
        if (len < 1f) return;
        dir /= len;

        var thickness = 4.5f * zoom;
        var steps = Math.Max(3, (int)(len / (3f * zoom)));

        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var p = Vector2.Lerp(start, end, t);
            var w = thickness * (1f - t * 0.45f);
            DrawPrimitives.FillCircle(sb, p, w * 0.55f + 1f * zoom, border, 10);
            DrawPrimitives.FillCircle(sb, p, w * 0.45f, fill, 10);
        }

        DrawPrimitives.FillCircle(sb, end, 2.5f * zoom, fill, 10);
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
            if (font.MeasureString(SpriteFontSafe.Filter(trial)).X <= maxWidth)
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
