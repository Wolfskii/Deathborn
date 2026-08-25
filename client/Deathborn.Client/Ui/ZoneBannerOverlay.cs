using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Top-center banner for zone entry and PvP status notices.</summary>
public sealed class ZoneBannerOverlay
{
    private enum BannerKind { Safe, Wilderness, Boss }

    private string _title = "";
    private string _subtitle = "";
    private float _timer;
    private float _fade = 1f;
    private bool _active;
    private BannerKind _bannerKind = BannerKind.Safe;

    public string? PersistentZoneName { get; private set; }
    public bool InSafeZone { get; private set; }

    public void ShowEnter(string zoneName, string subtitle, bool wilderness = false, bool boss = false)
    {
        _title = SpriteFontSafe.Filter(zoneName);
        _subtitle = SpriteFontSafe.Filter(subtitle);
        _bannerKind = boss ? BannerKind.Boss
            : wilderness ? BannerKind.Wilderness
            : BannerKind.Safe;
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
        if (InSafeZone && !string.IsNullOrEmpty(PersistentZoneName))
            DrawPersistentPill(sb, font);

        if (!_active || _fade <= 0.01f) return;
        DrawCenterBanner(sb, font, _title, _subtitle, _bannerKind, _fade);
    }

    private void DrawPersistentPill(SpriteBatch sb, SpriteFont font)
    {
        var label = SpriteFontSafe.Filter($"{PersistentZoneName} - Safe");
        var size = SpriteFontSafe.MeasureString(font, label);
        var panelW = Math.Min((int)size.X + 32, Math.Max(120, GameViewport.Width - 24));
        var rect = new Rectangle(GameViewport.Width / 2 - panelW / 2, 6, panelW, 30);
        FarmRpgUi.DrawTitle(sb, rect, 0.94f);
        DrawCenteredText(sb, font, label, rect, FarmRpgUi.Ink, 1f, 12);
    }

    private static void DrawCenterBanner(
        SpriteBatch sb, SpriteFont font, string title, string subtitle,
        BannerKind bannerKind, float fade)
    {
        var titleSize = SpriteFontSafe.MeasureString(font, title);
        var subSize = string.IsNullOrEmpty(subtitle) ? Vector2.Zero : SpriteFontSafe.MeasureString(font, subtitle);
        var bodyW = (int)MathF.Max(titleSize.X, subSize.X * 0.9f) + 56;
        var maxW = Math.Max(180, Math.Min(520, GameViewport.Width - 24));
        var totalW = Math.Min(Math.Max(bodyW, Math.Min(280, maxW)), maxW);
        var totalH = string.IsNullOrEmpty(subtitle) ? 78 : 108;
        var panel = new Rectangle(GameViewport.Width / 2 - totalW / 2, 16, totalW, totalH);

        FarmRpgUi.DrawWindowPanel(sb, panel, fade * 0.96f);
        var titlePanel = new Rectangle(panel.X + 12, panel.Y + 10, panel.Width - 24, 38);
        FarmRpgUi.DrawTitle(sb, titlePanel, fade);
        DrawCenteredText(sb, font, title, titlePanel, BannerTextColor(bannerKind) * fade, 1f, 14);

        if (!string.IsNullOrEmpty(subtitle))
        {
            var subtitlePanel = new Rectangle(panel.X + 18, titlePanel.Bottom + 8, panel.Width - 36, 38);
            FarmRpgUi.DrawInsetPanel(sb, subtitlePanel, fade);
            DrawCenteredText(sb, font, subtitle, subtitlePanel, FarmRpgUi.InkMuted * fade, 0.9f, 12);
        }
    }

    private static Color BannerTextColor(BannerKind kind) => kind switch
    {
        BannerKind.Wilderness => FarmRpgUi.Rust,
        BannerKind.Boss => new Color(112, 62, 111),
        _ => new Color(55, 112, 72),
    };

    private static void DrawCenteredText(
        SpriteBatch sb, SpriteFont font, string text, Rectangle area, Color color, float preferredScale, int horizontalPadding)
    {
        var size = SpriteFontSafe.MeasureString(font, text);
        var availableW = Math.Max(1, area.Width - horizontalPadding * 2);
        var scale = MathF.Min(preferredScale, availableW / MathF.Max(1f, size.X));
        var pos = new Vector2(
            area.X + (area.Width - size.X * scale) * 0.5f,
            area.Y + (area.Height - size.Y * scale) * 0.5f);
        SpriteFontSafe.DrawString(sb, font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
