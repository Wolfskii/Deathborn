using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Top-centre zone UI: a short entry banner when the player arrives somewhere, and a
/// small persistent pill with the current zone. The pill hides while the banner shows,
/// so the two never overlap.
/// </summary>
public sealed class ZoneBannerOverlay
{
    private enum BannerKind { Safe, Wilderness, Boss }

    private const float BannerSeconds = 3.5f;
    private const float FadeInSeconds = 0.25f;
    private const float FadeOutSeconds = 0.8f;
    private const int Pad = 16;

    private string _title = "";
    private string _subtitle = "";
    private float _age;
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
        _age = 0f;
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
        _age += dt;
        if (_age >= BannerSeconds)
            _active = false;
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        var bannerAlpha = _active ? BannerAlpha() : 0f;
        if (bannerAlpha > 0.01f)
            DrawBanner(sb, font, bannerAlpha);

        // Cross-fade: the pill comes back as the banner fades out.
        var pillAlpha = 1f - bannerAlpha;
        if (pillAlpha > 0.01f && !string.IsNullOrEmpty(PersistentZoneName))
            DrawPersistentPill(sb, font, pillAlpha);
    }

    private float BannerAlpha()
    {
        var fadeIn = MathF.Min(1f, _age / FadeInSeconds);
        var remaining = BannerSeconds - _age;
        var fadeOut = remaining < FadeOutSeconds ? MathF.Max(0f, remaining / FadeOutSeconds) : 1f;
        return fadeIn * fadeOut;
    }

    private void DrawPersistentPill(SpriteBatch sb, SpriteFont font, float alpha)
    {
        var name = SpriteFontSafe.Filter(PersistentZoneName);
        var suffix = InSafeZone ? "Safe" : "PvP";
        const string separator = "  -  ";
        var nameW = UiText.Measure(font, name).X;
        var sepW = UiText.Measure(font, separator).X;
        var suffixW = UiText.Measure(font, suffix).X;
        var width = (int)(nameW + sepW + suffixW) + 56;
        var rect = new Rectangle(GameViewport.Width / 2 - width / 2, 8, width, FarmRpgUi.TitlePlateHeight);
        FarmRpgUi.DrawTitle(sb, rect, alpha);

        var x = rect.X + (rect.Width - (nameW + sepW + suffixW)) / 2f;
        var y = rect.Y + (rect.Height - font.LineSpacing) / 2f;
        UiText.Draw(sb, font, name, new Vector2(x, y), FarmRpgUi.Ink * alpha);
        UiText.Draw(sb, font, separator, new Vector2(x + nameW, y), FarmRpgUi.InkFaint * alpha);
        UiText.Draw(sb, font, suffix, new Vector2(x + nameW + sepW, y),
            (InSafeZone ? FarmRpgUi.Positive : FarmRpgUi.Danger) * alpha);
    }

    private void DrawBanner(SpriteBatch sb, SpriteFont font, float alpha)
    {
        var titleW = UiText.Measure(font, _title).X;
        var subW = string.IsNullOrEmpty(_subtitle) ? 0f : UiText.Measure(font, _subtitle).X;
        var width = Math.Clamp((int)MathF.Max(titleW, subW) + Pad * 2 + 48, 300, Math.Max(300, GameViewport.Width - 40));
        var height = Pad * 2 + font.LineSpacing + (string.IsNullOrEmpty(_subtitle) ? 0 : 14 + font.LineSpacing);
        var panel = new Rectangle(GameViewport.Width / 2 - width / 2, 16, width, height);

        FarmRpgUi.DrawCompactPanel(sb, panel, alpha);
        var titleArea = new Rectangle(panel.X, panel.Y + Pad, panel.Width, font.LineSpacing);
        UiText.DrawCentered(sb, font, _title, titleArea, TitleColor(_bannerKind) * alpha);

        if (string.IsNullOrEmpty(_subtitle)) return;
        var ruleW = Math.Min(180, panel.Width - Pad * 4);
        FarmRpgUi.DrawDivider(sb, panel.Center.X - ruleW / 2, titleArea.Bottom + 7, ruleW, alpha);
        var subArea = new Rectangle(panel.X, titleArea.Bottom + 14, panel.Width, font.LineSpacing);
        UiText.DrawCentered(sb, font, _subtitle, subArea, FarmRpgUi.InkMuted * alpha);
    }

    private static Color TitleColor(BannerKind kind) => kind switch
    {
        BannerKind.Wilderness => FarmRpgUi.Danger,
        BannerKind.Boss => new Color(112, 62, 111),
        _ => FarmRpgUi.Positive,
    };
}
