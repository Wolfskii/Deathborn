using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Persistent top-left HP / stamina / mana bars with a diamond portrait.</summary>
public sealed class PlayerStatusHud
{
    private const int Margin = 16;
    private const float DiamondSide = 56f;
    private const float PortraitScale = 5.0f;
    /// <summary>First opaque hair row in the 32px idle cell.</summary>
    private const float HairSourceTopPx = 6f;
    /// <summary>How far the hair peeks past the diamond tip, in screen pixels.</summary>
    private const float HeadStickOut = 10f;
    private const int BarHeight = 18;
    private const int BarGap = 6;
    private const int HpBarW = 196;
    private const int StaminaBarW = 176;
    private const int ManaBarW = 176;

    private static readonly Color HpFill = new(183, 58, 52);
    private static readonly Color StaminaFill = new(62, 148, 58);
    private static readonly Color ManaFill = new(62, 108, 186);
    private static readonly Color Track = new(36, 20, 16);
    private static readonly Color Frame = new(62, 36, 28);
    private static readonly Color DiamondFill = new(48, 26, 20);
    private static readonly Color ValueOnFill = new(255, 236, 210);

    /// <summary>Keep the framebuffer, still run fragments so stencil can write.</summary>
    private static readonly BlendState StencilMaskBlend = new()
    {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    private static readonly DepthStencilState StencilWrite = new()
    {
        StencilEnable = true,
        StencilFunction = CompareFunction.Always,
        StencilPass = StencilOperation.Replace,
        StencilMask = int.MaxValue,
        ReferenceStencil = 1,
        DepthBufferEnable = false,
        DepthBufferWriteEnable = false,
    };

    private static readonly DepthStencilState StencilTest = new()
    {
        StencilEnable = true,
        StencilFunction = CompareFunction.Equal,
        StencilPass = StencilOperation.Keep,
        StencilMask = int.MaxValue,
        ReferenceStencil = 1,
        DepthBufferEnable = false,
        DepthBufferWriteEnable = false,
    };

    public void Draw(SpriteBatch sb, SpriteFont font, PlayerEntity? player, PlayerSkills? skills)
    {
        if (player == null) return;

        var stats = player.Stats;
        var diamondR = DiamondSide / MathF.Sqrt(2f);
        var cx = Margin + diamondR;
        var cy = Margin + HeadStickOut + diamondR;
        var center = new Vector2(cx, cy);

        var barsLeft = (int)MathF.Round(cx + diamondR * 0.28f);
        var barsTop = (int)MathF.Round(cy - BarHeight * 1.5f - BarGap);
        var nameY = barsTop - font.LineSpacing - 2;

        var name = SpriteFontSafe.Filter(player.Name);
        var level = skills != null ? skills.TotalLevel : stats.Level;
        SpriteFontSafe.DrawOutlined(sb, font, name, new Vector2(barsLeft + 10, nameY), ValueOnFill, Color.Black);
        var lvl = $"LVL {level}";
        var lvlSize = font.MeasureString(lvl);
        SpriteFontSafe.DrawOutlined(sb, font, lvl,
            new Vector2(barsLeft + HpBarW - lvlSize.X, nameY), new Color(210, 190, 160), Color.Black);

        DrawResourceBar(sb, font, new Rectangle(barsLeft, barsTop, HpBarW, BarHeight),
            stats.Hp, stats.HpMax, HpFill);
        DrawResourceBar(sb, font, new Rectangle(barsLeft, barsTop + BarHeight + BarGap, StaminaBarW, BarHeight),
            stats.Stamina, stats.StaminaMax, StaminaFill);
        DrawResourceBar(sb, font, new Rectangle(barsLeft, barsTop + (BarHeight + BarGap) * 2, ManaBarW, BarHeight),
            stats.Mana, stats.ManaMax, ManaFill);

        DrawDiamond(sb, center, DiamondSide + 4f, FarmRpgUi.Ink);
        DrawDiamond(sb, center, DiamondSide, DiamondFill);
        DrawDiamondOutline(sb, center, diamondR + 1f, FarmRpgUi.Cream, 2.2f);
        DrawDiamondOutline(sb, center, diamondR, FarmRpgUi.Ink, 1.4f);

        DrawPortrait(sb, player, center, diamondR);
    }

    private static void DrawPortrait(SpriteBatch sb, PlayerEntity player, Vector2 center, float diamondR)
    {
        var gd = DeathbornGame.Instance.GraphicsDevice;
        var spriteTop = center.Y - diamondR - HeadStickOut - HairSourceTopPx * PortraitScale;
        var feet = new Vector2(center.X, spriteTop + 32f * PortraitScale);

        sb.End();
        gd.Clear(ClearOptions.Stencil, Color.Transparent, 0f, 0);

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: StencilMaskBlend,
            depthStencilState: StencilWrite,
            rasterizerState: RasterizerState.CullNone);
        DrawDiamond(sb, center, DiamondSide, Color.White);
        // Same fill path as the diamond (rotated-rect draw). A dest-rect FillRect was not
        // opening stencil above the tip, so hair got clipped at the point.
        var pokeH = HeadStickOut + 12f + diamondR * 0.85f;
        var pokeW = diamondR * 1.85f;
        var pokeCenter = new Vector2(
            center.X,
            center.Y - diamondR - HeadStickOut - 12f + pokeH * 0.5f);
        DrawPrimitives.FillRotatedRect(sb, pokeCenter, pokeW, pokeH, 0f, Color.White);
        sb.End();

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            depthStencilState: StencilTest,
            rasterizerState: RasterizerState.CullNone);
        player.DrawHudPortrait(sb, feet, PortraitScale);
        sb.End();
        sb.Begin(samplerState: SamplerState.PointClamp);
    }

    private static void DrawResourceBar(
        SpriteBatch sb, SpriteFont font, Rectangle rect, float current, float max, Color fill)
    {
        DrawPrimitives.FillRoundedRect(sb, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), Color.Black, 5);
        DrawPrimitives.FillRoundedRect(sb, rect, Track, 3);
        var pct = max > 0f ? MathHelper.Clamp(current / max, 0f, 1f) : 0f;
        var fillW = (int)MathF.Round(rect.Width * pct);
        if (fillW > 0)
            DrawPrimitives.FillRoundedRect(sb, new Rectangle(rect.X, rect.Y, Math.Max(fillW, 6), rect.Height), fill, 3);

        var label = $"{(int)current} / {(int)max}";
        var size = font.MeasureString(label);
        var pos = new Vector2(rect.Right - size.X - 6, rect.Y + (rect.Height - size.Y) * 0.5f);
        sb.DrawString(font, label, pos, ValueOnFill);
    }

    private static void DrawDiamond(SpriteBatch sb, Vector2 center, float side, Color color) =>
        DrawPrimitives.FillRotatedRect(sb, center, side, side, MathHelper.PiOver4, color);

    private static void DrawDiamondOutline(SpriteBatch sb, Vector2 center, float r, Color color, float thickness)
    {
        var top = center + new Vector2(0, -r);
        var right = center + new Vector2(r, 0);
        var bottom = center + new Vector2(0, r);
        var left = center + new Vector2(-r, 0);
        DrawPrimitives.DrawLine(sb, top, right, color, thickness);
        DrawPrimitives.DrawLine(sb, right, bottom, color, thickness);
        DrawPrimitives.DrawLine(sb, bottom, left, color, thickness);
        DrawPrimitives.DrawLine(sb, left, top, color, thickness);
    }
}
