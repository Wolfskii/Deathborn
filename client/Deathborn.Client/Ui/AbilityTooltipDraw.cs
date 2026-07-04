using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public static class AbilityTooltipDraw
{
    public static void DrawItem(SpriteBatch sb, SpriteFont font, ItemInfo info, Rectangle anchor, Point viewportSize)
    {
        var lines = new List<string> { info.Description };
        if (info.Kind == ItemKind.Cosmetic)
            lines.Add("Click to wear or remove.");
        if (info.Heal is > 0) lines.Add($"Heals: {info.Heal} HP");
        if (info.ManaRestore is > 0) lines.Add($"Restores: {info.ManaRestore:0} mana");
        if (info.StaminaRestore is > 0) lines.Add($"Restores: {info.StaminaRestore:0} stamina");
        if (info.Cooldown > 0) lines.Add($"Cooldown: {info.Cooldown:0.#}s");
        DrawPanel(sb, font, lines, info.Name, anchor, viewportSize);
    }

    public static void DrawHotbarEntry(
        SpriteBatch sb, SpriteFont font, Dictionary<string, object> entry, Rectangle anchor, Point viewportSize)
    {
        var id = entry.GetValueOrDefault(HotbarEntry.IdKey) as string;
        var kind = entry.GetValueOrDefault("kind") as string;
        if (kind == "item" || kind == "cosmetic")
        {
            var itemId = entry.GetValueOrDefault("itemId") as string ?? id;
            if (itemId != null && ItemCatalog.Get(itemId) is { } item)
            {
                var lines = BuildItemLines(item);
                if (kind == "cosmetic")
                    lines.Add("Click to wear or remove.");
                DrawPanel(sb, font, lines, item.Name, anchor, viewportSize, preferAbove: true);
            }
            return;
        }

        if (id != null && AbilityCatalog.Get(id) is { } ability)
            DrawAbility(sb, font, ability, anchor, viewportSize, preferAbove: true);
    }

    public static void DrawAbility(
        SpriteBatch sb, SpriteFont font, AbilityInfo info, Rectangle anchor, Point viewportSize, bool preferAbove = false)
    {
        var lines = BuildLines(info);
        DrawPanel(sb, font, lines, info.Name, anchor, viewportSize, preferAbove);
    }

    private static List<string> BuildItemLines(ItemInfo info)
    {
        var lines = new List<string> { info.Description };
        if (info.Heal is > 0) lines.Add($"Heals: {info.Heal} HP");
        if (info.ManaRestore is > 0) lines.Add($"Restores: {info.ManaRestore:0} mana");
        if (info.StaminaRestore is > 0) lines.Add($"Restores: {info.StaminaRestore:0} stamina");
        if (info.Cooldown > 0) lines.Add($"Cooldown: {info.Cooldown:0.#}s");
        return lines;
    }

    private static List<string> BuildLines(AbilityInfo info)
    {
        var lines = new List<string> { info.Description };
        if (info.Damage is > 0) lines.Add($"Damage: {info.Damage}");
        if (info.Heal is > 0) lines.Add($"Heal: {info.Heal} HP");
        if (info.Range is > 0) lines.Add($"Range: {info.Range:0} px");
        lines.Add($"Cost: {AbilityResourceCosts.FormatCostLine(info)}");
        if (info.Cooldown > 0) lines.Add($"Cooldown: {info.Cooldown:0.#}s");
        if (!string.IsNullOrEmpty(info.ExtraStats))
            lines.Add(info.ExtraStats);
        return lines;
    }

    private static void DrawPanel(
        SpriteBatch sb, SpriteFont font, IReadOnlyList<string> lines, string title,
        Rectangle anchor, Point viewportSize, bool preferAbove = false)
    {
        const int pad = 8;
        var maxW = 0f;
        maxW = MathF.Max(maxW, font.MeasureString(title).X);
        foreach (var line in lines)
            maxW = MathF.Max(maxW, font.MeasureString(line).X);

        var w = (int)maxW + pad * 2;
        var h = pad * 2 + font.LineSpacing * (lines.Count + 1);

        int x;
        int y;
        if (preferAbove)
        {
            x = anchor.X + anchor.Width / 2 - w / 2;
            y = anchor.Y - h - 8;
            if (y < 4) y = anchor.Bottom + 8;
        }
        else
        {
            x = anchor.Right + 8;
            y = anchor.Y;
            if (x + w > viewportSize.X) x = anchor.X - w - 8;
            if (y + h > viewportSize.Y) y = viewportSize.Y - h - 4;
        }

        x = Math.Clamp(x, 4, Math.Max(4, viewportSize.X - w - 4));

        var panel = new Rectangle(x, y, w, h);
        Deathborn.Client.Rendering.DrawPrimitives.FillRect(sb, panel, new Color(12, 14, 22, 240));
        DrawBorder(sb, panel, new Color(210, 170, 80));
        sb.DrawString(font, SpriteFontSafe.Filter(title), new Vector2(panel.X + pad, panel.Y + pad), new Color(235, 210, 140));
        var ly = panel.Y + pad + font.LineSpacing + 2;
        foreach (var line in lines)
        {
            sb.DrawString(font, SpriteFontSafe.Filter(line), new Vector2(panel.X + pad, ly), new Color(195, 200, 210));
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
