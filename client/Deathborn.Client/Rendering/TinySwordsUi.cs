using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Tiny Swords UI sprites — 9-slice panels, ribbons, buttons, and bars.</summary>
public static class TinySwordsUi
{
    public const int SlotCell = 64;
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

        if (kind is PanelKind.Paper or PanelKind.PaperSpecial or PanelKind.Wood && tex.Width >= 300)
            DrawNineSliceGrid(sb, tex, dest, grid: 3, gutter: 12, alpha: alpha);
        else
            DrawNineSliceGrid(sb, tex, dest, grid: 3, gutter: 0, alpha: alpha);
    }

    public static void DrawRibbon(
        SpriteBatch sb, Rectangle dest, RibbonKind kind, bool pointed = false, float alpha = 1f)
    {
        if (_ribbonSmall == null) return;
        var row = RibbonRowIndex(kind, pointed);
        DrawHorizontalThreeSlice(sb, _ribbonSmall, dest, row, alpha);
    }

    public static void DrawBigRibbon(
        SpriteBatch sb, Rectangle dest, RibbonKind kind, bool pointed = false, float alpha = 1f)
    {
        if (_ribbonBig == null) return;
        var row = RibbonRowIndex(kind, pointed);
        DrawHorizontalThreeSlice(sb, _ribbonBig, dest, row, alpha);
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
        DrawNineSliceGrid(sb, tex, dest, grid: 3, gutter: 12, alpha: alpha);
    }

    public static void DrawBar(
        SpriteBatch sb, Rectangle dest, float fill01, bool big = true, Color? fillTint = null, float alpha = 1f)
    {
        var baseTex = big ? _barBigBase : _barSmallBase;
        var fillTex = big ? _barBigFill : _barSmallFill;
        if (baseTex == null || fillTex == null) return;

        DrawHorizontalThreeSlice(sb, baseTex, dest, row: 0, alpha);
        var inset = 6;
        var inner = new Rectangle(dest.X + inset, dest.Y + inset, dest.Width - inset * 2, dest.Height - inset * 2);
        if (inner.Width <= 0 || inner.Height <= 0) return;

        var fillW = Math.Max(0, (int)((inner.Width - 4) * MathHelper.Clamp(fill01, 0f, 1f)));
        if (fillW <= 0) return;
        var fillRect = new Rectangle(inner.X + 2, inner.Y + 2, fillW, inner.Height - 4);
        var tint = (fillTint ?? Color.White) * alpha;
        sb.Draw(fillTex, fillRect, new Rectangle(0, 0, fillTex.Width, fillTex.Height), tint);
    }

    public static Rectangle MeasureRibbonTextArea(Rectangle ribbonDest, int padX = 12) =>
        new(ribbonDest.X + padX, ribbonDest.Y + 4, Math.Max(0, ribbonDest.Width - padX * 2), Math.Max(0, ribbonDest.Height - 8));

    private static int RibbonRowIndex(RibbonKind kind, bool pointed) =>
        (int)kind * 2 + (pointed ? 0 : 1);

    private static void DrawHorizontalThreeSlice(
        SpriteBatch sb, Texture2D tex, Rectangle dest, int row, float alpha)
    {
        var cell = RibbonCol;
        var cap = Math.Min(cell, dest.Height);
        var leftW = Math.Min(cap, dest.Width / 3);
        var rightW = Math.Min(cap, dest.Width / 3);
        var midW = Math.Max(0, dest.Width - leftW - rightW);
        var y = dest.Y + (dest.Height - cap) / 2;

        var color = Color.White * alpha;
        var srcY = row * cell;
        sb.Draw(tex, new Rectangle(dest.X, y, leftW, cap), new Rectangle(0, srcY, cell, cell), color);
        if (midW > 0)
            sb.Draw(tex, new Rectangle(dest.X + leftW, y, midW, cap), new Rectangle(cell, srcY, cell, cell), color);
        sb.Draw(tex, new Rectangle(dest.X + leftW + midW, y, rightW, cap),
            new Rectangle(cell * 2, srcY, cell, cell), color);
    }

    private static void DrawNineSliceGrid(
        SpriteBatch sb, Texture2D tex, Rectangle dest, int grid, int gutter, float alpha)
    {
        var totalGutter = gutter * (grid - 1);
        var cellW = (tex.Width - totalGutter) / grid;
        var cellH = (tex.Height - totalGutter) / grid;

        int SrcX(int col) => col * (cellW + gutter);
        int SrcY(int row) => row * (cellH + gutter);

        var left = cellW;
        var right = cellW;
        var top = cellH;
        var bottom = cellH;
        var centerW = Math.Max(0, dest.Width - left - right);
        var centerH = Math.Max(0, dest.Height - top - bottom);
        var color = Color.White * alpha;

        // corners
        sb.Draw(tex, new Rectangle(dest.X, dest.Y, left, top),
            new Rectangle(SrcX(0), SrcY(0), cellW, cellH), color);
        sb.Draw(tex, new Rectangle(dest.Right - right, dest.Y, right, top),
            new Rectangle(SrcX(2), SrcY(0), cellW, cellH), color);
        sb.Draw(tex, new Rectangle(dest.X, dest.Bottom - bottom, left, bottom),
            new Rectangle(SrcX(0), SrcY(2), cellW, cellH), color);
        sb.Draw(tex, new Rectangle(dest.Right - right, dest.Bottom - bottom, right, bottom),
            new Rectangle(SrcX(2), SrcY(2), cellW, cellH), color);

        // edges
        if (centerW > 0)
        {
            sb.Draw(tex, new Rectangle(dest.X + left, dest.Y, centerW, top),
                new Rectangle(SrcX(1), SrcY(0), cellW, cellH), color);
            sb.Draw(tex, new Rectangle(dest.X + left, dest.Bottom - bottom, centerW, bottom),
                new Rectangle(SrcX(1), SrcY(2), cellW, cellH), color);
        }

        if (centerH > 0)
        {
            sb.Draw(tex, new Rectangle(dest.X, dest.Y + top, left, centerH),
                new Rectangle(SrcX(0), SrcY(1), cellW, cellH), color);
            sb.Draw(tex, new Rectangle(dest.Right - right, dest.Y + top, right, centerH),
                new Rectangle(SrcX(2), SrcY(1), cellW, cellH), color);
        }

        if (centerW > 0 && centerH > 0)
        {
            sb.Draw(tex, new Rectangle(dest.X + left, dest.Y + top, centerW, centerH),
                new Rectangle(SrcX(1), SrcY(1), cellW, cellH), color);
        }
    }
}
