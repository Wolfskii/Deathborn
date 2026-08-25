using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Shared Farm RPG chrome for gameplay windows, controls, tooltips, and HUD panels.</summary>
public static class FarmRpgUi
{
    public static readonly Color Ink = new(83, 42, 31);
    public static readonly Color InkMuted = new(118, 72, 55);
    public static readonly Color Parchment = new(250, 181, 140);
    public static readonly Color Rust = new(188, 75, 42);
    public static readonly Color Cream = new(255, 222, 180);

    private const int HudCell = 16;
    private static readonly Rectangle ButtonNormal = new(1, 1, 46, 14);
    private static readonly Rectangle ButtonPressed = new(1, 18, 46, 14);
    private static readonly Rectangle CloseIcon = HudCellRect(8, 2);
    private static readonly Rectangle CheckIcon = HudCellRect(16, 5);
    private static readonly Rectangle CrossIcon = HudCellRect(17, 5);
    private static readonly Rectangle BookIcon = HudCellRect(8, 3);
    private static readonly Rectangle GearIcon = HudCellRect(6, 1);

    private static Texture2D? _buttons;
    private static Texture2D? _hud;
    private static Texture2D? _book;
    private static Texture2D? _banner;
    private static Texture2D? _bars;

    public static bool IsLoaded => FarmRpgDialogueUi.IsLoaded;

    public enum Icon
    {
        Close,
        Check,
        Cross,
        Book,
        Gear,
    }

    public static void Load(ContentManager content)
    {
        _buttons = TryLoad(content, "Ui/FarmRpg/buttons");
        _hud = TryLoad(content, "Ui/FarmRpg/hud");
        _book = TryLoad(content, "Ui/FarmRpg/book");
        _banner = TryLoad(content, "Ui/FarmRpg/banner");
        _bars = TryLoad(content, "Ui/FarmRpg/bars");
    }

    public static void DrawWindowPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f) =>
        FarmRpgDialogueUi.DrawPanel(sb, dest, alpha, scalloped: true);

    public static void DrawInsetPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f) =>
        FarmRpgDialogueUi.DrawPanel(sb, dest, alpha);

    public static void DrawTitle(SpriteBatch sb, Rectangle dest, float alpha = 1f) =>
        FarmRpgDialogueUi.DrawPanel(sb, dest, alpha);

    public static void DrawBookPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f)
    {
        if (_book != null)
        {
            sb.Draw(_book, dest, new Rectangle(0, 0, 240, 145), Color.White * alpha);
            return;
        }
        DrawWindowPanel(sb, dest, alpha);
    }

    public static void DrawButton(
        SpriteBatch sb, Rectangle dest, bool pressed = false, bool disabled = false, bool danger = false)
    {
        if (_buttons != null)
        {
            var src = pressed ? ButtonPressed : ButtonNormal;
            var tint = disabled ? new Color(160, 140, 130) : danger ? new Color(255, 150, 145) : Color.White;
            sb.Draw(_buttons, dest, src, tint * (disabled ? 0.7f : 1f));
            return;
        }

        DrawInsetPanel(sb, dest, disabled ? 0.6f : 1f);
        var inner = Inset(dest, 5);
        DrawPrimitives.FillRect(sb, inner,
            disabled ? new Color(80, 65, 58, 55)
            : danger ? new Color(150, 42, 35, pressed ? 140 : 105)
            : pressed ? new Color(255, 224, 185, 80)
            : new Color(127, 62, 43, 70));
    }

    public static void DrawCloseButton(SpriteBatch sb, Rectangle dest, bool hover)
    {
        DrawButton(sb, dest, pressed: hover, danger: true);
        DrawIcon(sb, dest, Icon.Close, hover ? Color.White : Cream);
    }

    public static void DrawIcon(SpriteBatch sb, Rectangle dest, Icon icon, Color? tint = null)
    {
        if (_hud == null) return;
        var src = icon switch
        {
            Icon.Close => CloseIcon,
            Icon.Check => CheckIcon,
            Icon.Cross => CrossIcon,
            Icon.Book => BookIcon,
            Icon.Gear => GearIcon,
            _ => CloseIcon,
        };
        sb.Draw(_hud, dest, src, tint ?? Color.White);
    }

    public static void DrawBar(SpriteBatch sb, Rectangle dest, float fill01, Color fill)
    {
        var pad = dest.Height <= 8 ? 1 : 2;
        DrawPrimitives.FillRect(sb, dest, Ink);
        var inner = Inset(dest, pad);
        DrawPrimitives.FillRect(sb, inner, new Color(48, 24, 18));
        var fillW = (int)MathF.Round(inner.Width * MathHelper.Clamp(fill01, 0f, 1f));
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(inner.X, inner.Y, fillW, inner.Height), fill);
        DrawPrimitives.FillRect(sb, new Rectangle(inner.X, inner.Y, inner.Width, 1), Cream * 0.35f);
    }

    public static void DrawToggle(SpriteBatch sb, Rectangle dest, bool on, bool hover)
    {
        DrawInsetPanel(sb, dest);
        DrawPrimitives.FillRect(sb, Inset(dest, 5),
            hover ? new Color(255, 224, 185, 80) : new Color(127, 62, 43, 65));
        DrawIcon(sb, Inset(dest, 2), on ? Icon.Check : Icon.Cross, on ? new Color(80, 150, 75) : Rust);
    }

    public static void DrawScrollbar(SpriteBatch sb, Rectangle track, Rectangle thumb)
    {
        DrawPrimitives.FillRect(sb, track, new Color(91, 42, 31, 80));
        DrawInsetPanel(sb, thumb);
        DrawPrimitives.FillRect(sb, Inset(thumb, 4), new Color(133, 61, 40, 120));
    }

    public static Rectangle Inset(Rectangle rect, int amount) =>
        new(rect.X + amount, rect.Y + amount,
            Math.Max(1, rect.Width - amount * 2), Math.Max(1, rect.Height - amount * 2));

    private static Rectangle HudCellRect(int col, int row) =>
        new((col - 1) * HudCell, (row - 1) * HudCell, HudCell, HudCell);

    private static Texture2D? TryLoad(ContentManager content, string asset)
    {
        try
        {
            return content.Load<Texture2D>(asset);
        }
        catch
        {
            return null;
        }
    }
}
