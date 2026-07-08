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
    private static Texture2D? _normal;
    private static Texture2D? _hover;
    private static Texture2D? _blocked;
    private static Texture2D? _slotOverlay;

    public static void Load(ContentManager content)
    {
        _normal = content.Load<Texture2D>("Cursors/cursor_main");
        _hover = content.Load<Texture2D>("Cursors/cursor_hover");
        _blocked = content.Load<Texture2D>("Cursors/cursor_blocked");
        _slotOverlay = content.Load<Texture2D>("Cursors/cursor_slot_overlay");
    }

    public static void DrawCursor(SpriteBatch sb, Point mouse, UiCursorKind kind)
    {
        var tex = kind switch
        {
            UiCursorKind.Hover => _hover,
            UiCursorKind.Blocked => _blocked,
            _ => _normal,
        };
        if (tex == null) return;

        // Arrow tip pixel inside each 64×64 sprite — aligns visual with mouse hotspot.
        var origin = kind switch
        {
            UiCursorKind.Hover => new Vector2(23, 17),
            UiCursorKind.Blocked => new Vector2(28, 13),
            _ => new Vector2(22, 17),
        };
        sb.Draw(tex, new Vector2(mouse.X, mouse.Y), null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
    }

    public static void DrawSlotOverlay(SpriteBatch sb, Rectangle slotRect)
    {
        if (_slotOverlay == null) return;
        var dest = new Rectangle(slotRect.X - 6, slotRect.Y - 6, slotRect.Width + 12, slotRect.Height + 12);
        sb.Draw(_slotOverlay, dest, Color.White);
    }
}
