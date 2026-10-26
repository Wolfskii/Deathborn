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
        Rectangle anchor, Point viewportSize, bool preferAbove = false) =>
        UiTooltip.Draw(sb, font, title, lines, anchor, viewportSize, preferAbove);
}
