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
        var rect = new Rectangle(mouse.X - size / 2, mouse.Y - size / 2, size, size);
        DrawPrimitives.FillRect(sb, rect, new Color(20, 22, 30, 210));
        DrawBorder(sb, rect, new Color(210, 170, 80));

        var icon = HotbarIconDraw.FitSquare(rect, top: 6, bottom: 6, horizontalPad: 6);
        HotbarIconDraw.Draw(sb, Active.IconId, icon);

        var name = Active.DisplayName ?? "?";
        if (name.Length > 14) name = name[..12] + "…";
        var ts = font.MeasureString(name);
        var labelPos = new Vector2(mouse.X - ts.X / 2f, rect.Bottom + 4);
        DrawPrimitives.FillRect(sb,
            new Rectangle((int)labelPos.X - 3, (int)labelPos.Y - 1, (int)ts.X + 6, (int)ts.Y + 2),
            new Color(0, 0, 0, 0.65f));
        sb.DrawString(font, name, labelPos, Color.White);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
