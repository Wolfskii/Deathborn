using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public enum UiCursorKind
{
    Normal,
    Hover,
    Blocked,
}

public static class UiCursorTheme
{
    /// <summary>Farm RPG HUD.png — 26×6 grid of 16×16 cells.</summary>
    private const int HudCols = 26;
    private const int HudRows = 6;
    private const int HudCell = 16;
    private const float CursorScale = 2f;

    // Row 1 col 2 (1-based) — tan pointer.
    private static readonly Rectangle SrcNormal = Cell(col: 2, row: 1);
    // Soft hover uses the pink pointer on row 2 col 2 when available.
    private static readonly Rectangle SrcHover = Cell(col: 2, row: 2);
    // Row 2 col 9 — red prohibit (left of power icon).
    private static readonly Rectangle SrcBlocked = Cell(col: 9, row: 2);

    private static Texture2D? _hud;
    private static Texture2D? _normalLegacy;
    private static Texture2D? _hoverLegacy;
    private static Texture2D? _blockedLegacy;
    private static Texture2D? _slotOverlay;

    private static Rectangle Cell(int col, int row) =>
        new((col - 1) * HudCell, (row - 1) * HudCell, HudCell, HudCell);

    public static void Load(ContentManager content)
    {
        _hud = null;
        try
        {
            _hud = content.Load<Texture2D>("Ui/FarmRpg/hud");
        }
        catch
        {
            // Optional until MGCB rebuild.
        }

        _normalLegacy = content.Load<Texture2D>("Cursors/cursor_main");
        _hoverLegacy = content.Load<Texture2D>("Cursors/cursor_hover");
        _blockedLegacy = content.Load<Texture2D>("Cursors/cursor_blocked");
        _slotOverlay = content.Load<Texture2D>("Cursors/cursor_slot_overlay");
    }

    public static void DrawCursor(SpriteBatch sb, Point mouse, UiCursorKind kind)
    {
        if (_hud != null)
        {
            var src = kind switch
            {
                UiCursorKind.Blocked => SrcBlocked,
                UiCursorKind.Hover => SrcHover,
                _ => SrcNormal,
            };
            // Tip of the pointer / center of the prohibit circle.
            var origin = kind == UiCursorKind.Blocked
                ? new Vector2(HudCell * 0.5f, HudCell * 0.5f)
                : new Vector2(5f, 3f);
            sb.Draw(_hud, new Vector2(mouse.X, mouse.Y), src, Color.White, 0f, origin,
                CursorScale, SpriteEffects.None, 0f);
            return;
        }

        var tex = kind switch
        {
            UiCursorKind.Hover => _hoverLegacy,
            UiCursorKind.Blocked => _blockedLegacy,
            _ => _normalLegacy,
        };
        if (tex == null) return;

        var legacyOrigin = kind switch
        {
            UiCursorKind.Hover => new Vector2(23, 17),
            UiCursorKind.Blocked => new Vector2(28, 13),
            _ => new Vector2(22, 17),
        };
        sb.Draw(tex, new Vector2(mouse.X, mouse.Y), null, Color.White, 0f, legacyOrigin, 1f,
            SpriteEffects.None, 0f);
    }

    public static void DrawSlotOverlay(SpriteBatch sb, Rectangle slotRect)
    {
        if (_slotOverlay == null) return;
        var dest = new Rectangle(slotRect.X - 6, slotRect.Y - 6, slotRect.Width + 12, slotRect.Height + 12);
        sb.Draw(_slotOverlay, dest, Color.White);
    }
}
