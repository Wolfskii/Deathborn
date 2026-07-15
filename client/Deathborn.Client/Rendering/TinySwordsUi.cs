using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Tiny Swords UI sprites — 9-slice panels, ribbons, buttons, and bars.</summary>
public static class TinySwordsUi
{
    public const int SlotCell = 64;
    public const int GridPanelBorder = 22;
    public const int RibbonRow = 64;
    public const int RibbonCol = 64;

    private static Texture2D? _bannerSlots;
    private static Texture2D? _woodSlots;
    private static Texture2D? _paperRegular;
    private static Texture2D? _paperSpecial;
    private static Texture2D? _ribbonSmall;
    private static Texture2D? _ribbonBig;
    private static Texture2D? _btnBlue;
    private static Texture2D? _btnBluePressed;
    private static Texture2D? _btnRed;
    private static Texture2D? _btnRedPressed;
    private static Texture2D? _btnRedTiny;
    private static Texture2D? _barBigBase;
    private static Texture2D? _barBigFill;
    private static Texture2D? _barSmallBase;
    private static Texture2D? _barSmallFill;

    public static bool IsLoaded => _woodSlots != null;

    public static void Load(ContentManager content)
    {
        _bannerSlots = content.Load<Texture2D>("Ui/ui_banner_slots");
        _woodSlots = content.Load<Texture2D>("Ui/ui_wood_slots");
        _paperRegular = content.Load<Texture2D>("Ui/ui_paper_regular");
        _paperSpecial = content.Load<Texture2D>("Ui/ui_paper_special");
        _ribbonSmall = content.Load<Texture2D>("Ui/ui_ribbon_small");
        _ribbonBig = content.Load<Texture2D>("Ui/ui_ribbon_big");
        _btnBlue = content.Load<Texture2D>("Ui/ui_btn_blue");
        _btnBluePressed = content.Load<Texture2D>("Ui/ui_btn_blue_pressed");
        _btnRed = content.Load<Texture2D>("Ui/ui_btn_red");
        _btnRedPressed = content.Load<Texture2D>("Ui/ui_btn_red_pressed");
        _btnRedTiny = content.Load<Texture2D>("Ui/ui_btn_red_tiny");
        _barBigBase = content.Load<Texture2D>("Ui/ui_bar_big_base");
        _barBigFill = content.Load<Texture2D>("Ui/ui_bar_big_fill");
        _barSmallBase = content.Load<Texture2D>("Ui/ui_bar_small_base");
        _barSmallFill = content.Load<Texture2D>("Ui/ui_bar_small_fill");
    }

    public enum PanelKind { Wood, Banner, Paper, PaperSpecial }

    public enum RibbonKind { Teal, Red, Gold, Purple, Steel }

    public enum ButtonKind { Blue, Red }

    public static void DrawPanel(SpriteBatch sb, Rectangle dest, PanelKind kind, float alpha = 1f)
    {
        var tex = kind switch
        {
            PanelKind.Wood => _woodSlots,
            PanelKind.Banner => _bannerSlots,
            PanelKind.Paper => _paperRegular,
            PanelKind.PaperSpecial => _paperSpecial,
            _ => _woodSlots,
        };
        if (tex == null) return;

        if (kind is PanelKind.Paper or PanelKind.PaperSpecial)
            DrawShowcase320NineSlice(sb, tex, dest, PaperSlices, alpha);
        else
            DrawGridNineSlice(sb, tex, dest, grid: 3, cell: SlotCell, alpha);
    }

    public static void DrawRibbon(
        SpriteBatch sb, Rectangle dest, RibbonKind kind, bool pointed = false, float alpha = 1f)
    {
        if (_ribbonSmall == null) return;
        var row = RibbonRowIndex(kind, pointed);
        var caps = pointed ? SmallRibbonPointedCaps : SmallRibbonAsymmetricCaps;
        DrawRibbonThreeSlice(sb, _ribbonSmall, dest, row, caps, alpha);
    }

    public static void DrawBigRibbon(
        SpriteBatch sb, Rectangle dest, RibbonKind kind, bool pointed = false, float alpha = 1f)
    {
        if (_ribbonBig == null) return;
        var row = RibbonRowIndex(kind, pointed);
        var caps = pointed ? BigRibbonPointedCaps : BigRibbonAsymmetricCaps;
        DrawRibbonThreeSlice(sb, _ribbonBig, dest, row, caps, alpha);
    }

    public static void DrawButton(
        SpriteBatch sb, Rectangle dest, ButtonKind kind, bool pressed, float alpha = 1f)
    {
        var tex = kind switch
        {
            ButtonKind.Red => pressed ? _btnRedPressed : _btnRed,
            _ => pressed ? _btnBluePressed : _btnBlue,
        };
        if (tex == null) return;

        var spec = ButtonSlices;
        if (dest.Width < spec.BorderLeft + spec.BorderRight - 8
            || dest.Height < spec.BorderTop + spec.BorderBottom - 8)
        {
            DrawSprite(sb, tex, dest, spec.Center, alpha);
            return;
        }

        DrawShowcase320NineSlice(sb, tex, dest, spec, alpha);
    }

    /// <summary>Small round red close/icon button (title bars, compact UI).</summary>
    public static void DrawCloseButton(SpriteBatch sb, Rectangle dest, bool pressed, float alpha = 1f)
    {
        if (_btnRedTiny == null)
        {
            DrawButton(sb, dest, ButtonKind.Red, pressed, alpha);
            return;
        }

        var tint = (pressed ? new Color(210, 210, 210) : Color.White) * alpha;
        sb.Draw(_btnRedTiny, dest, tint);
    }

    // ui_bar_small_fill.png — thin red strip centered in the 64×64 cell.
    private static readonly Rectangle SmallBarFillSrc = new(0, 30, 64, 3);

    // ui_bar_big_fill.png
    private static readonly Rectangle BigBarFillSrc = new(0, 20, 64, 24);

    // Inner groove insets from bar art, scaled by drawn height (not track width).
    private readonly struct BarGrooveInset(int left, int right, int top, int bottom, float nativeH)
    {
        public void GetInnerRect(Rectangle dest, out Rectangle inner)
        {
            var scale = dest.Height / Math.Max(nativeH, 1f);
            var innerLeft = Math.Max(0, (int)MathF.Round(left * scale));
            var innerRight = Math.Max(0, (int)MathF.Round(right * scale));
            var innerTop = Math.Max(0, (int)MathF.Round(top * scale));
            var innerBottom = Math.Max(0, (int)MathF.Round(bottom * scale));
            inner = new Rectangle(
                dest.X + innerLeft,
                dest.Y + innerTop,
                Math.Max(1, dest.Width - innerLeft - innerRight),
                Math.Max(1, dest.Height - innerTop - innerBottom));
        }
    }

    // Small bar outer art: groove insets in source pixels at native height 19.
    private static readonly BarGrooveInset SmallBarGroove = new(8, 7, 5, 5, 19f);

    // Big bar outer art at native height 51.
    private static readonly BarGrooveInset BigBarGroove = new(0, 0, 9, 0, 51f);

    public static void DrawBar(
        SpriteBatch sb, Rectangle dest, float fill01, bool big = true, Color? fillTint = null, float alpha = 1f)
    {
        var baseTex = big ? _barBigBase : _barSmallBase;
        var fillTex = big ? _barBigFill : _barSmallFill;
        if (baseTex == null || fillTex == null) return;

        var caps = big ? BigBarCaps : SmallBarCaps;
        DrawHorizontalBarSlice(sb, baseTex, dest, caps, alpha);

        var groove = big ? BigBarGroove : SmallBarGroove;
        groove.GetInnerRect(dest, out var inner);

        var fillW = Math.Max(0, (int)(inner.Width * MathHelper.Clamp(fill01, 0f, 1f)));
        if (fillW <= 0) return;

        var fillRect = new Rectangle(inner.X, inner.Y, fillW, inner.Height);
        var fillSrc = big ? BigBarFillSrc : SmallBarFillSrc;
        var tint = (fillTint ?? Color.White) * alpha;
        sb.Draw(fillTex, fillRect, fillSrc, tint);
    }

    public static Rectangle MeasureRibbonTextArea(Rectangle ribbonDest, int padX = 12) =>
        new(ribbonDest.X + padX, ribbonDest.Y + 4, Math.Max(0, ribbonDest.Width - padX * 2), Math.Max(0, ribbonDest.Height - 8));

    private static int RibbonRowIndex(RibbonKind kind, bool pointed) =>
        (int)kind * 2 + (pointed ? 0 : 1);

    /// <summary>Pre-baked 9-slice rects for 320×320 showcase sheets (buttons / parchment).</summary>
    private static readonly NineSliceSpec ButtonSlices = new(
        new(19, 17, 45, 47),
        new(128, 17, 64, 47),
        new(256, 17, 45, 47),
        new(19, 128, 45, 64),
        new(128, 128, 64, 64),
        new(256, 128, 45, 64),
        new(19, 256, 45, 47),
        new(128, 256, 64, 47),
        new(256, 256, 45, 47));

    private static readonly NineSliceSpec PaperSlices = new(
        new(12, 20, 52, 44),
        new(128, 20, 64, 44),
        new(256, 20, 52, 44),
        new(12, 128, 52, 64),
        new(128, 128, 64, 64),
        new(256, 128, 52, 64),
        new(12, 256, 52, 45),
        new(128, 256, 64, 43),
        new(256, 256, 52, 45));

    /// <summary>Left / middle / right cap source rects (y is relative to row; added at draw time).</summary>
    private readonly struct RibbonCapSpec(Rectangle left, Rectangle mid, Rectangle right)
    {
        public Rectangle Left { get; } = left;
        public Rectangle Mid { get; } = mid;
        public Rectangle Right { get; } = right;
    }

    // ui_ribbon_small.png — caps sit in columns 0, 2, 4 (not 0, 1, 2).
    private static readonly RibbonCapSpec SmallRibbonAsymmetricCaps = new(
        left: new(2, 5, 62, 59),
        mid: new(128, 4, 64, 55),
        right: new(256, 5, 62, 59));

    private static readonly RibbonCapSpec SmallRibbonPointedCaps = new(
        left: new(3, 4, 61, 54),
        mid: new(128, 4, 64, 54),
        right: new(256, 4, 61, 54));

    // ui_ribbon_big.png — wide showcase caps per row style.
    private static readonly RibbonCapSpec BigRibbonAsymmetricCaps = new(
        left: new(41, 20, 87, 44),
        mid: new(192, 22, 64, 42),
        right: new(320, 20, 87, 44));

    private static readonly RibbonCapSpec BigRibbonPointedCaps = new(
        left: new(30, 0, 98, 59),
        mid: new(192, 0, 64, 47),
        right: new(320, 0, 97, 57));

    private readonly struct NineSliceSpec(
        Rectangle topLeftRect, Rectangle topRect, Rectangle topRightRect,
        Rectangle leftRect, Rectangle centerRect, Rectangle rightRect,
        Rectangle bottomLeftRect, Rectangle bottomRect, Rectangle bottomRightRect)
    {
        public int BorderLeft => topLeftRect.Width;
        public int BorderTop => topLeftRect.Height;
        public int BorderRight => topRightRect.Width;
        public int BorderBottom => bottomLeftRect.Height;

        public Rectangle TopLeft { get; } = topLeftRect;
        public Rectangle Top { get; } = topRect;
        public Rectangle TopRight { get; } = topRightRect;
        public Rectangle Left { get; } = leftRect;
        public Rectangle Center { get; } = centerRect;
        public Rectangle Right { get; } = rightRect;
        public Rectangle BottomLeft { get; } = bottomLeftRect;
        public Rectangle Bottom { get; } = bottomRect;
        public Rectangle BottomRight { get; } = bottomRightRect;
    }

    private static void DrawShowcase320NineSlice(
        SpriteBatch sb, Texture2D tex, Rectangle dest, NineSliceSpec spec, float alpha) =>
        DrawNineSlice(sb, tex, dest, spec, alpha);

    private static void DrawGridNineSlice(
        SpriteBatch sb, Texture2D tex, Rectangle dest, int grid, int cell, float alpha)
    {
        var b = GridPanelBorder;
        int Cx(int col) => col * cell;
        int Cy(int row) => row * cell;

        var spec = new NineSliceSpec(
            new(Cx(0), Cy(0), b, b),
            new(Cx(1), Cy(0), cell, b),
            new(Cx(2) + cell - b, Cy(0), b, b),
            new(Cx(0), Cy(1), b, cell),
            new(Cx(1), Cy(1), cell, cell),
            new(Cx(2) + cell - b, Cy(1), b, cell),
            new(Cx(0), Cy(2) + cell - b, b, b),
            new(Cx(1), Cy(2) + cell - b, cell, b),
            new(Cx(2) + cell - b, Cy(2) + cell - b, b, b));

        DrawNineSlice(sb, tex, dest, spec, alpha);
    }

    private static void DrawNineSlice(
        SpriteBatch sb, Texture2D tex, Rectangle dest, NineSliceSpec spec, float alpha)
    {
        var left = Math.Min(spec.BorderLeft, Math.Max(1, dest.Width / 2));
        var right = Math.Min(spec.BorderRight, Math.Max(1, dest.Width - left));
        var top = Math.Min(spec.BorderTop, Math.Max(1, dest.Height / 2));
        var bottom = Math.Min(spec.BorderBottom, Math.Max(1, dest.Height - top));
        var centerW = Math.Max(0, dest.Width - left - right);
        var centerH = Math.Max(0, dest.Height - top - bottom);
        var color = Color.White * alpha;

        sb.Draw(tex, new Rectangle(dest.X, dest.Y, left, top), spec.TopLeft, color);
        if (centerW > 0)
            sb.Draw(tex, new Rectangle(dest.X + left, dest.Y, centerW, top), spec.Top, color);
        sb.Draw(tex, new Rectangle(dest.Right - right, dest.Y, right, top), spec.TopRight, color);

        if (centerH > 0)
        {
            sb.Draw(tex, new Rectangle(dest.X, dest.Y + top, left, centerH), spec.Left, color);
            if (centerW > 0)
                sb.Draw(tex, new Rectangle(dest.X + left, dest.Y + top, centerW, centerH), spec.Center, color);
            sb.Draw(tex, new Rectangle(dest.Right - right, dest.Y + top, right, centerH), spec.Right, color);
        }

        sb.Draw(tex, new Rectangle(dest.X, dest.Bottom - bottom, left, bottom), spec.BottomLeft, color);
        if (centerW > 0)
            sb.Draw(tex, new Rectangle(dest.X + left, dest.Bottom - bottom, centerW, bottom), spec.Bottom, color);
        sb.Draw(tex, new Rectangle(dest.Right - right, dest.Bottom - bottom, right, bottom), spec.BottomRight, color);
    }

    private static void DrawRibbonThreeSlice(
        SpriteBatch sb, Texture2D tex, Rectangle dest, int row, RibbonCapSpec caps, float alpha)
    {
        var srcY = row * RibbonRow;
        var leftSrc = OffsetY(caps.Left, srcY);
        var midSrc = OffsetY(caps.Mid, srcY);
        var rightSrc = OffsetY(caps.Right, srcY);

        var scale = dest.Height / (float)Math.Max(leftSrc.Height, 1);
        var leftW = Math.Max(1, (int)MathF.Round(leftSrc.Width * scale));
        var rightW = Math.Max(1, (int)MathF.Round(rightSrc.Width * scale));
        var capH = dest.Height;
        var midW = Math.Max(0, dest.Width - leftW - rightW);
        var y = dest.Y + (dest.Height - capH) / 2;
        var color = Color.White * alpha;

        sb.Draw(tex, new Rectangle(dest.X, y, leftW, capH), leftSrc, color);
        if (midW > 0)
            sb.Draw(tex, new Rectangle(dest.X + leftW, y, midW, capH), midSrc, color);
        sb.Draw(tex, new Rectangle(dest.X + leftW + midW, y, rightW, capH), rightSrc, color);
    }

    private readonly struct BarCapSpec(Rectangle left, Rectangle mid, Rectangle right)
    {
        public Rectangle Left { get; } = left;
        public Rectangle Mid { get; } = mid;
        public Rectangle Right { get; } = right;
    }

    // ui_bar_small_base.png — measured cap / repeat / cap regions on the 320×64 sheet.
    private static readonly BarCapSpec SmallBarCaps = new(
        left: new(49, 22, 15, 19),
        mid: new(128, 22, 64, 19),
        right: new(256, 22, 15, 19));

    // ui_bar_big_base.png
    private static readonly BarCapSpec BigBarCaps = new(
        left: new(40, 9, 24, 51),
        mid: new(128, 9, 64, 51),
        right: new(256, 9, 24, 51));

    private static void DrawHorizontalBarSlice(
        SpriteBatch sb, Texture2D tex, Rectangle dest, BarCapSpec caps, float alpha)
    {
        var scale = dest.Height / (float)Math.Max(caps.Left.Height, 1);
        var leftW = Math.Max(1, (int)MathF.Round(caps.Left.Width * scale));
        var rightW = Math.Max(1, (int)MathF.Round(caps.Right.Width * scale));
        var midW = Math.Max(0, dest.Width - leftW - rightW);
        var color = Color.White * alpha;

        sb.Draw(tex, new Rectangle(dest.X, dest.Y, leftW, dest.Height), caps.Left, color);
        if (midW > 0)
            sb.Draw(tex, new Rectangle(dest.X + leftW, dest.Y, midW, dest.Height), caps.Mid, color);
        sb.Draw(tex, new Rectangle(dest.X + leftW + midW, dest.Y, rightW, dest.Height), caps.Right, color);
    }

    private static Rectangle OffsetY(Rectangle src, int y) => new(src.X, src.Y + y, src.Width, src.Height);

    private static void DrawSprite(
        SpriteBatch sb, Texture2D tex, Rectangle dest, Rectangle src, float alpha) =>
        sb.Draw(tex, dest, src, Color.White * alpha);
}
