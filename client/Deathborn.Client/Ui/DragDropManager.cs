using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public enum DragPayloadKind { Ability, Item, Hotbar }

public sealed class DragPayload
{
    public DragPayloadKind Kind { get; init; }
    public string? AbilityId { get; init; }
    public string? ItemId { get; init; }
    public Dictionary<string, object>? HotbarEntry { get; init; }
    public int SourceHotbarIndex { get; init; } = -1;
    public int SourceInventoryIndex { get; init; } = -1;

    public string? IconId => AbilityId ?? ItemId
        ?? HotbarEntry?.GetValueOrDefault(Gameplay.HotbarEntry.IdKey) as string;
    public string? DisplayName =>
        HotbarEntry?.GetValueOrDefault("name") as string
        ?? AbilityCatalog.Get(AbilityId ?? "")?.Name
        ?? ItemCatalog.Get(ItemId ?? "")?.Name;
}

/// <summary>Shared drag-and-drop state for spell book, inventory, and hotbar.</summary>
public sealed class DragDropManager
{
    public DragPayload? Active { get; private set; }
    public bool IsDragging => Active != null;

    public void BeginAbility(string abilityId) =>
        Active = new DragPayload { Kind = DragPayloadKind.Ability, AbilityId = abilityId };

    public void BeginItem(string itemId, int inventoryIndex) =>
        Active = new DragPayload { Kind = DragPayloadKind.Item, ItemId = itemId, SourceInventoryIndex = inventoryIndex };

    public void BeginHotbar(Dictionary<string, object> entry, int hotbarIndex) =>
        Active = new DragPayload
        {
            Kind = DragPayloadKind.Hotbar,
            HotbarEntry = HotbarEntry.Clone(entry),
            SourceHotbarIndex = hotbarIndex,
        };

    public void Cancel() => Active = null;

    public void End() => Active = null;

    public void DrawGhost(SpriteBatch sb, SpriteFont font, Point mouse)
    {
        if (Active == null) return;

        const int size = 48;
        var rect = new Rectangle(
            Math.Clamp(mouse.X - size / 2, 2, Math.Max(2, GameViewport.Width - size - 2)),
            Math.Clamp(mouse.Y - size / 2, 2, Math.Max(2, GameViewport.Height - size - 2)),
            size,
            size);
        FarmRpgUi.DrawInsetPanel(sb, rect, 0.9f);

        var icon = HotbarIconDraw.FitSquare(rect, top: 6, bottom: 6, horizontalPad: 6);
        HotbarIconDraw.Draw(sb, Active.IconId, icon);

        var name = SpriteFontSafe.Filter(Active.DisplayName ?? "?");
        if (name.Length > 14) name = name[..11] + "...";
        var ts = font.MeasureString(name);
        var labelW = (int)MathF.Ceiling(ts.X) + 10;
        var labelH = (int)MathF.Ceiling(ts.Y) + 6;
        var labelX = Math.Clamp(mouse.X - labelW / 2, 2, Math.Max(2, GameViewport.Width - labelW - 2));
        var labelY = rect.Bottom + 4;
        if (labelY + labelH > GameViewport.Height - 2)
            labelY = rect.Y - labelH - 4;
        var labelRect = new Rectangle(labelX, Math.Max(2, labelY), labelW, labelH);
        FarmRpgUi.DrawInsetPanel(sb, labelRect, 0.9f);
        sb.DrawString(font, name, new Vector2(labelRect.X + 5, labelRect.Y + 3), FarmRpgUi.Ink);
    }
}
