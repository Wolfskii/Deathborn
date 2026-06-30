using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Ui;

public static class AbilityTooltipDraw
{
    public static void DrawAbility(SpriteBatch sb, SpriteFont font, AbilityInfo info, Rectangle anchor, Point viewportSize)
    {
        var lines = BuildLines(info);
        DrawPanel(sb, font, lines, info.Name, anchor, viewportSize);
    }

    public static void DrawItem(SpriteBatch sb, SpriteFont font, ItemInfo info, Rectangle anchor, Point viewportSize)
    {
        var lines = new List<string> { info.Description };
        if (info.Heal is > 0) lines.Add($"Heals: {info.Heal} HP");
        if (info.ManaRestore is > 0) lines.Add($"Restores: {info.ManaRestore:0} mana");
        if (info.StaminaRestore is > 0) lines.Add($"Restores: {info.StaminaRestore:0} stamina");
        if (info.Cooldown > 0) lines.Add($"Cooldown: {info.Cooldown:0.#}s");
        DrawPanel(sb, font, lines, info.Name, anchor, viewportSize);
    }

    private static List<string> BuildLines(AbilityInfo info)
    {
        var lines = new List<string> { info.Description };
        if (info.Damage is > 0) lines.Add($"Damage: {info.Damage}");
        if (info.Heal is > 0) lines.Add($"Heal: {info.Heal} HP");
        if (info.Range is > 0) lines.Add($"Range: {info.Range:0} px");
        if (info.CostKind != ResourceCostKind.None && info.Cost > 0)
        {
            var kind = info.CostKind == ResourceCostKind.Mana ? "Mana" : "Stamina";
            lines.Add($"{kind} cost: {info.Cost:0}");
        }
        if (info.Cooldown > 0) lines.Add($"Cooldown: {info.Cooldown:0.#}s");
        if (!string.IsNullOrEmpty(info.ExtraStats))
            lines.Add(info.ExtraStats);
        return lines;
    }

    private static void DrawPanel(
        SpriteBatch sb, SpriteFont font, IReadOnlyList<string> lines, string title,
        Rectangle anchor, Point viewportSize)
    {
        const int pad = 8;
        var maxW = 0f;
        maxW = MathF.Max(maxW, font.MeasureString(title).X);
        foreach (var line in lines)
            maxW = MathF.Max(maxW, font.MeasureString(line).X);

        var w = (int)maxW + pad * 2;
        var h = pad * 2 + font.LineSpacing * (lines.Count + 1);
        var x = anchor.Right + 8;
        var y = anchor.Y;
        if (x + w > viewportSize.X) x = anchor.X - w - 8;
        if (y + h > viewportSize.Y) y = viewportSize.Y - h - 4;

        var panel = new Rectangle(x, y, w, h);
        Deathborn.Client.Rendering.DrawPrimitives.FillRect(sb, panel, new Color(12, 14, 22, 240));
        DrawBorder(sb, panel, new Color(210, 170, 80));
        sb.DrawString(font, title, new Vector2(panel.X + pad, panel.Y + pad), new Color(235, 210, 140));
        var ly = panel.Y + pad + font.LineSpacing + 2;
        foreach (var line in lines)
        {
            sb.DrawString(font, line, new Vector2(panel.X + pad, ly), new Color(195, 200, 210));
            ly += font.LineSpacing;
        }
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        Deathborn.Client.Rendering.DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        Deathborn.Client.Rendering.DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        Deathborn.Client.Rendering.DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        Deathborn.Client.Rendering.DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
