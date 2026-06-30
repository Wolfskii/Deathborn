using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public enum InteractableKind { Generic, Tree, Rock, Chest, Npc, Fishing, Bank, Sign, Anvil }

public sealed class InteractableEntity
{
    public string Id = "";
    public string DisplayName = "";
    public Vector2 Position;
    public InteractableKind Kind;
    public Color Tint = new(0.55f, 0.45f, 0.32f);
    public float PickRadius = 20f;
    public float InteractRange = Config.InteractRange;
    public bool Highlighted;

    public bool IsNearPoint(Vector2 worldPos) =>
        Vector2.Distance(Position, worldPos) <= PickRadius;

    public bool IsInRange(Vector2 from) =>
        Vector2.Distance(Position, from) <= InteractRange;

    public string InteractMessage() => Kind switch
    {
        InteractableKind.Tree => $"You inspect {DisplayName}. Woodcutting coming in a later milestone.",
        InteractableKind.Rock => $"You inspect {DisplayName}. Mining coming in a later milestone.",
        InteractableKind.Fishing => $"You inspect {DisplayName}. Fishing coming in a later milestone.",
        InteractableKind.Chest => $"You open {DisplayName}. It is empty for now.",
        InteractableKind.Bank => $"You approach {DisplayName}. Banking coming in a later milestone.",
        InteractableKind.Anvil => $"You inspect {DisplayName}. Smithing coming in a later milestone.",
        InteractableKind.Npc => $"{DisplayName} says: \"Stay alive out there.\"",
        InteractableKind.Sign => $"The sign reads: {DisplayName}",
        _ => $"Interacted with {DisplayName}.",
    };

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        var body = Highlighted ? Tint * 1.25f : Tint;
        var outline = Highlighted ? new Color(1f, 0.92f, 0.55f, 0.95f) : new Color(0, 0, 0, 0.55f);
        var r = PickRadius * zoom;

        switch (Kind)
        {
            case InteractableKind.Tree:
                DrawPrimitives.FillCircle(sb, screenPos + new Vector2(0, 4 * zoom), r * 0.55f, new Color(0.35f, 0.22f, 0.12f));
                DrawPrimitives.FillCircle(sb, screenPos + new Vector2(0, -6 * zoom), r * 0.85f, body);
                break;
            case InteractableKind.Rock:
                DrawPrimitives.FillCircle(sb, screenPos, r * 0.9f, body);
                break;
            case InteractableKind.Chest:
                DrawPrimitives.FillRect(sb, CenteredRect(screenPos, r * 2, r * 1.2f), body);
                break;
            case InteractableKind.Npc:
                DrawPrimitives.FillCircle(sb, screenPos, r * 0.75f, body);
                DrawPrimitives.FillCircle(sb, screenPos + new Vector2(0, -r * 0.9f), r * 0.45f, body * 1.1f);
                break;
            case InteractableKind.Fishing:
                DrawPrimitives.FillCircle(sb, screenPos + new Vector2(0, 6 * zoom), r * 1.1f, new Color(0.18f, 0.35f, 0.62f, 0.85f));
                DrawPrimitives.DrawLine(sb, screenPos + new Vector2(-8, -8) * zoom, screenPos + new Vector2(10, -18) * zoom, body, 2.5f * zoom);
                break;
            case InteractableKind.Bank:
                DrawPrimitives.FillRect(sb, CenteredRect(screenPos, r * 1.8f, r), body);
                break;
            case InteractableKind.Sign:
                DrawPrimitives.FillRect(sb, new Rectangle((int)screenPos.X - (int)(2 * zoom), (int)screenPos.Y - (int)r, (int)(4 * zoom), (int)(r * 1.6f)), new Color(0.4f, 0.28f, 0.16f));
                DrawPrimitives.FillRect(sb, CenteredRect(screenPos + new Vector2(0, -r * 0.8f), r * 1.6f, r * 0.55f), body);
                break;
            case InteractableKind.Anvil:
                DrawPrimitives.FillRect(sb, CenteredRect(screenPos + new Vector2(0, 4 * zoom), r * 1.4f, 8 * zoom), body * 0.85f);
                DrawPrimitives.FillRect(sb, CenteredRect(screenPos + new Vector2(0, -8 * zoom), r * 0.9f, r * 0.45f), body);
                break;
            default:
                DrawPrimitives.FillCircle(sb, screenPos, r, body);
                break;
        }

        DrawPrimitives.DrawCircleOutline(sb, screenPos, r + 4 * zoom, outline, 32, Highlighted ? 2f * zoom : 1.5f * zoom);

        var label = font.MeasureString(DisplayName);
        sb.DrawString(font, DisplayName, screenPos + new Vector2(-label.X / 2, -r - 22 * zoom), Color.White);
    }

    private static Rectangle CenteredRect(Vector2 center, float w, float h) =>
        new((int)(center.X - w / 2), (int)(center.Y - h / 2), (int)w, (int)h);
}
