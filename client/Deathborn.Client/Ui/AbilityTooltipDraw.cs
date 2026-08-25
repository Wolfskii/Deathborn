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
        var maxContentW = Math.Max(48, Math.Min(360, viewportSize.X - 24 - pad * 2));
        var wrappedLines = new List<string>();
        foreach (var line in lines)
            WrapLine(font, SpriteFontSafe.Filter(line), maxContentW, wrappedLines);

        var safeTitle = FitText(font, SpriteFontSafe.Filter(title), maxContentW);
        var maxW = font.MeasureString(safeTitle).X;
        foreach (var line in wrappedLines)
            maxW = MathF.Max(maxW, font.MeasureString(line).X);

        var w = Math.Min(viewportSize.X - 8, (int)MathF.Ceiling(maxW) + pad * 2);
        var titleH = font.LineSpacing + 8;
        var h = pad * 2 + titleH + 4 + font.LineSpacing * wrappedLines.Count;

        int x, y;
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
        y = Math.Clamp(y, 4, Math.Max(4, viewportSize.Y - h - 4));

        var panel = new Rectangle(x, y, w, h);
        FarmRpgUi.DrawInsetPanel(sb, panel);
        var titleRect = new Rectangle(panel.X + 6, panel.Y + 6, panel.Width - 12, titleH);
        FarmRpgUi.DrawTitle(sb, titleRect);
        sb.DrawString(font, safeTitle, new Vector2(titleRect.X + 4, titleRect.Y + 4), FarmRpgUi.Ink);
        var ly = titleRect.Bottom + 4;
        foreach (var line in wrappedLines)
        {
            sb.DrawString(font, line, new Vector2(panel.X + pad, ly), FarmRpgUi.InkMuted);
            ly += font.LineSpacing;
        }
    }

    private static void WrapLine(SpriteFont font, string text, int maxWidth, List<string> output)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            output.Add("");
            return;
        }

        var current = words[0];
        for (var i = 1; i < words.Length; i++)
        {
            var candidate = current + " " + words[i];
            if (font.MeasureString(candidate).X <= maxWidth)
                current = candidate;
            else
            {
                output.Add(FitText(font, current, maxWidth));
                current = words[i];
            }
        }
        output.Add(FitText(font, current, maxWidth));
    }

    private static string FitText(SpriteFont font, string text, int maxWidth)
    {
        if (font.MeasureString(text).X <= maxWidth) return text;
        const string suffix = "...";
        while (text.Length > 0 && font.MeasureString(text + suffix).X > maxWidth)
            text = text[..^1];
        return text + suffix;
    }
}
