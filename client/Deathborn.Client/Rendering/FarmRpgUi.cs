using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Shared Farm RPG chrome for windows, menus, toasts and controls. Every piece is
/// cut from the Farm RPG UI sheets and drawn at whole-pixel scale so it stays crisp.
/// Layout constants live here too, so all panels share the same spacing.
/// </summary>
public static class FarmRpgUi
{
    // ── Palette ─────────────────────────────────────────────────────────────
    public static readonly Color Ink = new(83, 42, 31);
    public static readonly Color InkMuted = new(128, 82, 64);
    public static readonly Color InkFaint = new(166, 120, 98);
    public static readonly Color Parchment = new(250, 181, 140);
    public static readonly Color Paper = new(246, 221, 199);
    public static readonly Color PaperShade = new(234, 203, 178);
    public static readonly Color Rust = new(188, 75, 42);
    public static readonly Color Cream = new(255, 222, 180);
    public static readonly Color Positive = new(62, 124, 70);
    public static readonly Color Danger = new(176, 58, 46);
    public static readonly Color Warning = new(170, 104, 36);

    // ── Spacing (screen pixels) ─────────────────────────────────────────────
    /// <summary>Art scale for full windows.</summary>
    public const int Scale = 2;
    /// <summary>Frame thickness of a window panel, including its corner flourishes.</summary>
    public const int FrameBorder = PanelBorderSrc * Scale;
    /// <summary>Space between the frame and window content.</summary>
    public const int ContentPadding = 10;
    /// <summary>Default gap between rows of content.</summary>
    public const int Gap = 8;
    /// <summary>Gap before a new section.</summary>
    public const int SectionGap = 16;
    /// <summary>Standard button height (16px art at 2x).</summary>
    public const int ButtonHeight = 32;
    /// <summary>Height of a window title plate (15px pill at 2x).</summary>
    public const int TitlePlateHeight = 30;

    // ── Sheet rectangles ────────────────────────────────────────────────────
    private const int PanelBorderSrc = 12;
    private static readonly Rectangle PanelSrc = new(0, 64, 48, 48);              // inventory.png
    private static readonly Rectangle PillLightSrc = new(2, 113, 44, 15);         // banner.png
    private static readonly Rectangle PillDarkSrc = new(2, 145, 44, 15);          // banner.png
    private const int PillCapSrc = 9;
    private static readonly Rectangle DividerLeftSrc = new(68, 181, 10, 7);       // banner.png
    private static readonly Rectangle DividerMidSrc = new(78, 181, 20, 7);
    private static readonly Rectangle DividerRightSrc = new(98, 181, 10, 7);
    private const int ButtonWideW = 48;
    private const int ButtonWideH = 16;
    private const int ButtonPressedOffsetY = 17;
    private const int IconButtonX = 145;
    private const int IconButtonPitch = 16;
    private const int IconButtonW = 14;
    private static readonly Rectangle SliderKnobSrc = new(50, 98, 12, 12);        // extras.png

    private const int HudCell = 16;
    private static readonly Rectangle CloseIcon = HudCellRect(8, 2);
    private static readonly Rectangle CheckIcon = HudCellRect(16, 5);
    private static readonly Rectangle CrossIcon = HudCellRect(17, 5);
    private static readonly Rectangle BookIcon = HudCellRect(8, 3);
    private static readonly Rectangle GearIcon = HudCellRect(6, 1);

    private static Texture2D? _buttons;
    private static Texture2D? _hud;
    private static Texture2D? _book;
    private static Texture2D? _banner;
    private static Texture2D? _inventory;
    private static Texture2D? _extras;

    public static bool IsLoaded => _inventory != null && _banner != null && _buttons != null;

    public enum Icon
    {
        Close,
        Check,
        Cross,
        Book,
        Gear,
    }

    /// <summary>Colour rows of the button sheet.</summary>
    public enum ButtonTone
    {
        Tan,
        Brown,
        Rose,
    }

    /// <summary>Glyphs on the icon-button sheet (index into each colour row).</summary>
    public enum Glyph
    {
        Blank = 0,
        Next = 14,
        Previous = 15,
        Close = 16,
        Check = 17,
    }

    public static void Load(ContentManager content)
    {
        _buttons = TryLoad(content, "Ui/FarmRpg/buttons");
        _hud = TryLoad(content, "Ui/FarmRpg/hud");
        _book = TryLoad(content, "Ui/FarmRpg/book");
        _banner = TryLoad(content, "Ui/FarmRpg/banner");
        _inventory = TryLoad(content, "Ui/FarmRpg/inventory");
        _extras = TryLoad(content, "Ui/FarmRpg/extras");
    }

    // ── Panels ──────────────────────────────────────────────────────────────

    /// <summary>Full window frame: cream paper with corner flourishes and a soft drop shadow.</summary>
    public static void DrawWindowPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f)
    {
        DrawShadow(sb, dest, alpha);
        if (_inventory == null)
        {
            DrawFlatPanel(sb, dest, alpha);
            return;
        }
        DrawNineSlice(sb, _inventory, PanelSrc, PanelBorderSrc, PanelBorderSrc, PanelBorderSrc, PanelBorderSrc, dest, Scale, alpha);
    }

    /// <summary>Same frame at 1x for small surfaces: tooltips, toasts, menus, buff cards.</summary>
    public static void DrawCompactPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f)
    {
        DrawShadow(sb, dest, alpha, offset: 2);
        if (_inventory == null)
        {
            DrawFlatPanel(sb, dest, alpha);
            return;
        }
        DrawNineSlice(sb, _inventory, PanelSrc, PanelBorderSrc, PanelBorderSrc, PanelBorderSrc, PanelBorderSrc, dest, 1, alpha);
    }

    /// <summary>A recessed well inside a panel: lists, chat logs, grouped controls.</summary>
    public static void DrawInsetPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f)
    {
        DrawPrimitives.FillRoundedRect(sb, dest, InkFaint * (0.55f * alpha), 4);
        DrawPrimitives.FillRoundedRect(sb, Inset(dest, 1), PaperShade * alpha, 3);
    }

    /// <summary>Title pill sized to the destination (used for banners and inline labels).</summary>
    public static void DrawTitle(SpriteBatch sb, Rectangle dest, float alpha = 1f) =>
        DrawPill(sb, dest, dark: false, alpha);

    /// <summary>Dark pill, for an active tab or a strong label.</summary>
    public static void DrawTitleDark(SpriteBatch sb, Rectangle dest, float alpha = 1f) =>
        DrawPill(sb, dest, dark: true, alpha);

    /// <summary>
    /// Window title plate centred on <paramref name="centerX"/>, sized to the text.
    /// Returns the plate rectangle (useful as a drag handle).
    /// </summary>
    public static Rectangle DrawTitlePlate(SpriteBatch sb, SpriteFont font, string title, int centerX, int top, float alpha = 1f)
    {
        var plate = TitlePlateRect(font, title, centerX, top);
        DrawPill(sb, plate, dark: false, alpha);
        UiText.DrawCentered(sb, font, title, plate, Ink * alpha);
        return plate;
    }

    public static Rectangle TitlePlateRect(SpriteFont font, string title, int centerX, int top)
    {
        var width = (int)UiText.Measure(font, title).X + 64;
        return new Rectangle(centerX - width / 2, top, width, TitlePlateHeight);
    }

    public static void DrawBookPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f)
    {
        if (_book != null)
        {
            sb.Draw(_book, dest, new Rectangle(0, 0, 240, 145), Color.White * alpha);
            return;
        }
        DrawWindowPanel(sb, dest, alpha);
    }

    // ── Separators ──────────────────────────────────────────────────────────

    /// <summary>Thin ornamental rule with arrow caps, vertically centred on <paramref name="centerY"/>.</summary>
    public static void DrawDivider(SpriteBatch sb, int x, int centerY, int width, float alpha = 1f)
    {
        if (_banner == null || width < 48)
        {
            DrawPrimitives.FillRect(sb, new Rectangle(x, centerY, width, 2), InkFaint * alpha);
            return;
        }
        var h = DividerMidSrc.Height * Scale;
        var capW = DividerLeftSrc.Width * Scale;
        var y = centerY - h / 2;
        var tint = Color.White * alpha;
        sb.Draw(_banner, new Rectangle(x, y, capW, h), DividerLeftSrc, tint);
        sb.Draw(_banner, new Rectangle(x + capW, y, width - capW * 2, h), DividerMidSrc, tint);
        sb.Draw(_banner, new Rectangle(x + width - capW, y, capW, h), DividerRightSrc, tint);
    }

    /// <summary>Section label followed by a rule across the remaining width. Returns the height used.</summary>
    public static int DrawSectionHeader(SpriteBatch sb, SpriteFont font, string text, int x, int y, int width, float alpha = 1f)
    {
        var size = UiText.Measure(font, text);
        UiText.Draw(sb, font, text, new Vector2(x, y), Ink * alpha);
        var ruleX = x + (int)size.X + 10;
        var ruleW = x + width - ruleX;
        if (ruleW > 12)
            DrawPrimitives.FillRect(sb, new Rectangle(ruleX, y + (int)(size.Y / 2f), ruleW, 2), InkFaint * (0.6f * alpha));
        return (int)size.Y;
    }

    // ── Buttons and controls ────────────────────────────────────────────────

    public static void DrawButton(
        SpriteBatch sb, Rectangle dest, bool pressed = false, bool disabled = false, bool danger = false)
    {
        var tone = danger ? ButtonTone.Rose : ButtonTone.Tan;
        DrawButton(sb, dest, tone, pressed && !disabled, disabled);
    }

    public static void DrawButton(SpriteBatch sb, Rectangle dest, ButtonTone tone, bool pressed, bool disabled = false)
    {
        var alpha = disabled ? 0.5f : 1f;
        if (_buttons == null)
        {
            DrawPrimitives.FillRoundedRect(sb, dest, (pressed ? PaperShade : Paper) * alpha, 4);
            return;
        }
        var src = new Rectangle(0, ToneRow(tone) + (pressed ? ButtonPressedOffsetY : 0), ButtonWideW,
            pressed ? ButtonWideH - 1 : ButtonWideH);
        // Pressed art is one row shorter; nudge it down so the label appears to sink.
        var target = pressed ? new Rectangle(dest.X, dest.Y + 2, dest.Width, dest.Height - 2) : dest;
        var scale = Math.Max(1, Math.Min(Scale, dest.Height / ButtonWideH));
        DrawNineSlice(sb, _buttons, src, 3, 3, 2, 3, target, scale, alpha);
    }

    /// <summary>Square icon button from the sheet (close, check, arrows).</summary>
    public static void DrawIconButton(SpriteBatch sb, Rectangle dest, Glyph glyph, bool pressed, ButtonTone tone = ButtonTone.Tan, float alpha = 1f)
    {
        if (_buttons == null) return;
        var x = IconButtonX + (int)glyph * IconButtonPitch;
        var src = pressed
            ? new Rectangle(x, ToneRow(tone) + ButtonPressedOffsetY, IconButtonW, ButtonWideH - 1)
            : new Rectangle(x, ToneRow(tone), IconButtonW, ButtonWideH);
        var target = pressed ? new Rectangle(dest.X, dest.Y + 2, dest.Width, dest.Height - 2) : dest;
        sb.Draw(_buttons, target, src, Color.White * alpha);
    }

    /// <summary>Size of an icon button at window scale (14x16 art at 2x).</summary>
    public static Point IconButtonSize => new(IconButtonW * Scale, ButtonWideH * Scale);

    public static void DrawCloseButton(SpriteBatch sb, Rectangle dest, bool hover) =>
        DrawIconButton(sb, dest, Glyph.Close, hover, ButtonTone.Rose);

    /// <summary>Checkbox: a tan square that shows a tick when on.</summary>
    public static void DrawToggle(SpriteBatch sb, Rectangle dest, bool on, bool hover)
    {
        // Keep the 14x16 art proportions at a whole-pixel scale so the tick stays crisp.
        var scale = Math.Max(1, Math.Min(dest.Height / ButtonWideH, dest.Width / IconButtonW));
        var w = IconButtonW * scale;
        var h = ButtonWideH * scale;
        var box = new Rectangle(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
        DrawIconButton(sb, box, on ? Glyph.Check : Glyph.Blank, pressed: false, hover ? ButtonTone.Brown : ButtonTone.Tan);
    }

    /// <summary>Horizontal slider: a bar with a round knob at <paramref name="value01"/>.</summary>
    public static void DrawSlider(SpriteBatch sb, Rectangle track, float value01, Color fill)
    {
        var barH = 10;
        var bar = new Rectangle(track.X, track.Center.Y - barH / 2, track.Width, barH);
        DrawBar(sb, bar, value01, fill);
        var knobSize = SliderKnobSrc.Width * Scale;
        var knobX = bar.X + (int)MathF.Round(bar.Width * MathHelper.Clamp(value01, 0f, 1f)) - knobSize / 2;
        var knob = new Rectangle(knobX, track.Center.Y - knobSize / 2, knobSize, knobSize);
        if (_extras != null)
            sb.Draw(_extras, knob, SliderKnobSrc, Color.White);
        else
            DrawPrimitives.FillCircle(sb, knob.Center.ToVector2(), knobSize / 2f, Cream);
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

    /// <summary>Progress bar: dark outline, warm track, coloured fill with a top highlight.</summary>
    public static void DrawBar(SpriteBatch sb, Rectangle dest, float fill01, Color fill)
    {
        var radius = Math.Min(3, dest.Height / 2);
        DrawPrimitives.FillRoundedRect(sb, dest, Ink, radius);
        var inner = Inset(dest, dest.Height <= 8 ? 1 : 2);
        DrawPrimitives.FillRect(sb, inner, new Color(120, 74, 58));
        var fillW = (int)MathF.Round(inner.Width * MathHelper.Clamp(fill01, 0f, 1f));
        if (fillW > 0)
        {
            DrawPrimitives.FillRect(sb, new Rectangle(inner.X, inner.Y, fillW, inner.Height), fill);
            DrawPrimitives.FillRect(sb, new Rectangle(inner.X, inner.Y, fillW, Math.Max(1, inner.Height / 4)), Color.White * 0.25f);
        }
    }

    public static void DrawScrollbar(SpriteBatch sb, Rectangle track, Rectangle thumb)
    {
        var trackW = Math.Max(4, track.Width / 2);
        var slim = new Rectangle(track.Center.X - trackW / 2, track.Y, trackW, track.Height);
        DrawPrimitives.FillRoundedRect(sb, slim, PaperShade, trackW / 2);
        var thumbW = Math.Max(6, track.Width - 2);
        var slimThumb = new Rectangle(track.Center.X - thumbW / 2, thumb.Y, thumbW, thumb.Height);
        DrawPrimitives.FillRoundedRect(sb, slimThumb, InkMuted, thumbW / 2);
    }

    public static Rectangle Inset(Rectangle rect, int amount) =>
        new(rect.X + amount, rect.Y + amount,
            Math.Max(1, rect.Width - amount * 2), Math.Max(1, rect.Height - amount * 2));

    // ── Internals ───────────────────────────────────────────────────────────

    private static void DrawPill(SpriteBatch sb, Rectangle dest, bool dark, float alpha)
    {
        if (_banner == null)
        {
            DrawPrimitives.FillRoundedRect(sb, dest, (dark ? InkMuted : Paper) * alpha, dest.Height / 2);
            return;
        }
        var src = dark ? PillDarkSrc : PillLightSrc;
        var scale = Math.Max(1, (int)MathF.Round(dest.Height / (float)src.Height));
        DrawNineSlice(sb, _banner, src, PillCapSrc, PillCapSrc, 4, 4, dest, scale, alpha);
    }

    private static void DrawShadow(SpriteBatch sb, Rectangle dest, float alpha, int offset = 4)
    {
        var shadow = new Rectangle(dest.X + offset, dest.Y + offset, dest.Width, dest.Height);
        DrawPrimitives.FillRoundedRect(sb, shadow, Color.Black * (0.22f * alpha), 6);
    }

    private static void DrawFlatPanel(SpriteBatch sb, Rectangle dest, float alpha)
    {
        DrawPrimitives.FillRoundedRect(sb, dest, Ink * alpha, 6);
        DrawPrimitives.FillRoundedRect(sb, Inset(dest, 2), Paper * alpha, 5);
    }

    private static int ToneRow(ButtonTone tone) => tone switch
    {
        ButtonTone.Brown => 16,
        ButtonTone.Rose => 352,
        _ => 112,
    };

    /// <summary>
    /// Nine-slice with independent borders, drawn at an integer <paramref name="scale"/>.
    /// Corners keep their pixel size; edges and centre stretch.
    /// </summary>
    private static void DrawNineSlice(
        SpriteBatch sb, Texture2D tex, Rectangle src, int left, int right, int top, int bottom,
        Rectangle dest, int scale, float alpha)
    {
        var l = Math.Min(left * scale, dest.Width / 2);
        var r = Math.Min(right * scale, dest.Width / 2);
        var t = Math.Min(top * scale, dest.Height / 2);
        var b = Math.Min(bottom * scale, dest.Height / 2);
        var tint = Color.White * alpha;

        int[] dx = [dest.X, dest.X + l, dest.Right - r, dest.Right];
        int[] dy = [dest.Y, dest.Y + t, dest.Bottom - b, dest.Bottom];
        int[] sx = [src.X, src.X + left, src.Right - right, src.Right];
        int[] sy = [src.Y, src.Y + top, src.Bottom - bottom, src.Bottom];

        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var d = new Rectangle(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
                var s = new Rectangle(sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row]);
                if (d.Width <= 0 || d.Height <= 0 || s.Width <= 0 || s.Height <= 0) continue;
                sb.Draw(tex, d, s, tint);
            }
        }
    }

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
