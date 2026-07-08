using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Top-center banner for zone entry and PvP status notices.</summary>
public sealed class ZoneBannerOverlay
{
    private string _title = "";
    private string _subtitle = "";
    private float _timer;
    private float _fade = 1f;
    private bool _active;
    private TinySwordsUi.RibbonKind _ribbon = TinySwordsUi.RibbonKind.Gold;

    public string? PersistentZoneName { get; private set; }
    public bool InSafeZone { get; private set; }

    public void ShowEnter(string zoneName, string subtitle, bool wilderness = false, bool boss = false)
    {
        _title = zoneName;
        _subtitle = subtitle;
        _ribbon = boss ? TinySwordsUi.RibbonKind.Purple
            : wilderness ? TinySwordsUi.RibbonKind.Red
            : TinySwordsUi.RibbonKind.Teal;
        _timer = 4.5f;
        _fade = 1f;
        _active = true;
    }

    public void SetPersistentZone(string? zoneName, bool safe)
    {
        PersistentZoneName = zoneName;
        InSafeZone = safe;
    }

    public void Update(float dt)
    {
        if (!_active) return;
        _timer -= dt;
        if (_timer <= 1.2f)
            _fade = MathF.Max(0f, _timer / 1.2f);
        if (_timer <= 0f)
        {
            _active = false;
            _fade = 0f;
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (TinySwordsUi.IsLoaded)
        {
            if (InSafeZone && !string.IsNullOrEmpty(PersistentZoneName))
                DrawPersistentPill(sb, font);

            if (!_active || _fade <= 0.01f) return;
            DrawCenterBanner(sb, font, _title, _subtitle, _ribbon, pointed: true, _fade);
            return;
        }

        DrawLegacy(sb, font);
    }

    private void DrawPersistentPill(SpriteBatch sb, SpriteFont font)
    {
        var label = $"{PersistentZoneName} - Safe";
        var size = font.MeasureString(label);
        var ribbonW = (int)size.X + 40;
        var ribbonH = 30;
        var rect = new Rectangle(GameViewport.Width / 2 - ribbonW / 2, 6, ribbonW, ribbonH);
        TinySwordsUi.DrawRibbon(sb, rect, TinySwordsUi.RibbonKind.Teal, pointed: false, 0.92f);
        sb.DrawString(font, label, new Vector2(rect.X + 20, rect.Y + 5), new Color(200, 255, 210));
    }

    private static void DrawCenterBanner(
        SpriteBatch sb, SpriteFont font, string title, string subtitle,
        TinySwordsUi.RibbonKind ribbonKind, bool pointed, float fade)
    {
        var titleSize = font.MeasureString(title);
        var subSize = string.IsNullOrEmpty(subtitle) ? Vector2.Zero : font.MeasureString(subtitle);
        var bodyW = (int)MathF.Max(titleSize.X, subSize.X) + 56;
        var bodyH = (int)(titleSize.Y + (subSize.Y > 0 ? subSize.Y + 8 : 0) + 36);
        var ribbonH = 40;
        var totalW = Math.Clamp(bodyW, 280, 520);
        var totalH = ribbonH + bodyH;
        var panel = new Rectangle(GameViewport.Width / 2 - totalW / 2, 16, totalW, totalH);

        TinySwordsUi.DrawPanel(sb, panel, TinySwordsUi.PanelKind.Banner, fade * 0.96f);
        var ribbon = new Rectangle(panel.X + 12, panel.Y + 10, panel.Width - 24, ribbonH);
        TinySwordsUi.DrawBigRibbon(sb, ribbon, ribbonKind, pointed, fade);

        var textArea = TinySwordsUi.MeasureRibbonTextArea(ribbon, 12);
        sb.DrawString(font, SpriteFontSafe.Filter(title), new Vector2(textArea.X, textArea.Y + 4),
            new Color(255, 248, 220) * fade, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        if (!string.IsNullOrEmpty(subtitle))
        {
            sb.DrawString(font, SpriteFontSafe.Filter(subtitle),
                new Vector2(panel.X + 28, panel.Y + ribbonH + 14),
                new Color(210, 230, 200) * fade, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }
    }

    private void DrawLegacy(SpriteBatch sb, SpriteFont font)
    {
        if (InSafeZone && !string.IsNullOrEmpty(PersistentZoneName))
        {
            var label = $"{PersistentZoneName} - Safe";
            var size = font.MeasureString(label);
            var rect = new Rectangle(GameViewport.Width / 2 - (int)size.X / 2 - 10, 4, (int)size.X + 20, (int)size.Y + 8);
            DrawPrimitives.FillRect(sb, rect, new Color(18, 28, 22, 190));
            sb.DrawString(font, label, new Vector2(rect.X + 10, rect.Y + 4), new Color(140, 220, 160));
        }

        if (!_active || _fade <= 0.01f) return;

        var titleSize = font.MeasureString(_title);
        var subSize = font.MeasureString(_subtitle);
        var w = (int)MathF.Max(titleSize.X, subSize.X) + 36;
        var h = 20 + font.LineSpacing * 2 + 4;
        var panel = new Rectangle(GameViewport.Width / 2 - w / 2, 18, w, h);
        DrawPrimitives.FillRect(sb, panel, new Color(14, 16, 24, (int)(220 * _fade)));
        sb.DrawString(font, _title, new Vector2(panel.Center.X - titleSize.X / 2f, panel.Y + 10), new Color(240, 225, 180) * _fade);
        sb.DrawString(font, _subtitle, new Vector2(panel.Center.X - subSize.X / 2f, panel.Y + 10 + font.LineSpacing + 2),
            new Color(170, 210, 175) * _fade);
    }
}
