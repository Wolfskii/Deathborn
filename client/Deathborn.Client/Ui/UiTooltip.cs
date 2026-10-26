using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Hover tooltip: compact panel with a title, a thin rule, and wrapped body lines.</summary>
public static class UiTooltip
{
    private const int Pad = 14;
    private const int MaxTextWidth = 300;
    private const int AnchorGap = 10;

    /// <param name="preferAbove">Place above the anchor (hotbar); otherwise to its right.</param>
    public static void Draw(
        SpriteBatch sb, SpriteFont font, string title, IEnumerable<string> lines,
        Rectangle anchor, Point viewport, bool preferAbove = false, Color? titleColor = null)
    {
        var maxText = Math.Max(80, Math.Min(MaxTextWidth, viewport.X - 40));
        var safeTitle = UiText.Truncate(font, title, maxText);
        var body = new List<string>();
        foreach (var line in lines)
            body.AddRange(UiText.Wrap(font, line, maxText));

        var textW = UiText.Measure(font, safeTitle).X;
        foreach (var line in body)
            textW = MathF.Max(textW, UiText.Measure(font, line).X);

        var lineH = font.LineSpacing + 2;
        var w = (int)MathF.Ceiling(textW) + Pad * 2;
        var h = Pad * 2 + font.LineSpacing + (body.Count > 0 ? 10 + body.Count * lineH : 0);

        int x, y;
        if (preferAbove)
        {
            x = anchor.Center.X - w / 2;
            y = anchor.Y - h - AnchorGap;
            if (y < 4) y = anchor.Bottom + AnchorGap;
        }
        else
        {
            x = anchor.Right + AnchorGap;
            y = anchor.Y;
            if (x + w > viewport.X - 4) x = anchor.X - w - AnchorGap;
        }
        x = Math.Clamp(x, 4, Math.Max(4, viewport.X - w - 4));
        y = Math.Clamp(y, 4, Math.Max(4, viewport.Y - h - 4));

        var panel = new Rectangle(x, y, w, h);
        FarmRpgUi.DrawCompactPanel(sb, panel);

        var cy = panel.Y + Pad;
        UiText.Draw(sb, font, safeTitle, new Vector2(panel.X + Pad, cy), titleColor ?? FarmRpgUi.Ink);
        cy += font.LineSpacing;
        if (body.Count == 0) return;

        cy += 4;
        DrawPrimitives.FillRect(sb, new Rectangle(panel.X + Pad, cy, panel.Width - Pad * 2, 1), FarmRpgUi.InkFaint * 0.6f);
        cy += 6;
        foreach (var line in body)
        {
            UiText.Draw(sb, font, line, new Vector2(panel.X + Pad, cy), FarmRpgUi.InkMuted);
            cy += lineH;
        }
    }
}
