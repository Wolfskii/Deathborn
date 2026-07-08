using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public enum NotificationKind
{
    Info,
    Success,
    Warning,
    Danger,
    Quest,
}

/// <summary>Stacked toast notifications (top-right) using Tiny Swords ribbons and parchment panels.</summary>
public sealed class GameNotificationOverlay
{
    private sealed class Toast
    {
        public string Title = "";
        public string Subtitle = "";
        public NotificationKind Kind;
        public float Age;
        public float Duration = 4.5f;
    }

    private readonly List<Toast> _toasts = [];
    private const int MaxVisible = 5;
    private const int Margin = 14;
    private const int Gap = 8;

    public void Push(string title, string? subtitle = null, NotificationKind kind = NotificationKind.Info, float duration = 4.5f)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        var safeTitle = SpriteFontSafe.Filter(title.Trim());
        if (safeTitle.Length == 0) return;
        _toasts.Insert(0, new Toast
        {
            Title = safeTitle,
            Subtitle = SpriteFontSafe.Filter(subtitle?.Trim()),
            Kind = kind,
            Duration = duration,
        });
        while (_toasts.Count > MaxVisible)
            _toasts.RemoveAt(_toasts.Count - 1);
    }

    public void Update(float dt)
    {
        for (var i = _toasts.Count - 1; i >= 0; i--)
        {
            _toasts[i].Age += dt;
            if (_toasts[i].Age >= _toasts[i].Duration)
                _toasts.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (_toasts.Count == 0 || !TinySwordsUi.IsLoaded) return;

        var x = GameViewport.Width - Margin;
        var y = Margin + 52;

        foreach (var toast in _toasts)
        {
            var alpha = ToastAlpha(toast);
            if (alpha <= 0.01f) continue;

            var (ribbonKind, pointed) = StyleFor(toast.Kind);
            var titleSize = SpriteFontSafe.MeasureString(font, toast.Title);
            var subSize = string.IsNullOrEmpty(toast.Subtitle) ? Vector2.Zero : SpriteFontSafe.MeasureString(font, toast.Subtitle);
            var bodyW = (int)MathF.Max(titleSize.X, subSize.X) + 36;
            var bodyH = (int)(titleSize.Y + (subSize.Y > 0 ? subSize.Y + 6 : 0) + 28);
            var ribbonH = 34;
            var totalW = Math.Clamp(bodyW, 200, 360);
            var totalH = ribbonH + bodyH - 8;
            var panel = new Rectangle(x - totalW, y, totalW, totalH);

            TinySwordsUi.DrawPanel(sb, panel, TinySwordsUi.PanelKind.Banner, alpha * 0.95f);
            var ribbon = new Rectangle(panel.X + 8, panel.Y + 6, panel.Width - 16, ribbonH);
            TinySwordsUi.DrawRibbon(sb, ribbon, ribbonKind, pointed, alpha);

            var titleColor = TitleColor(toast.Kind) * alpha;
            var textArea = TinySwordsUi.MeasureRibbonTextArea(ribbon, 10);
            SpriteFontSafe.DrawString(sb, font, toast.Title,
                new Vector2(textArea.X, textArea.Y + 2), titleColor, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);

            if (!string.IsNullOrEmpty(toast.Subtitle))
            {
                var subY = panel.Y + ribbonH + 8;
                SpriteFontSafe.DrawString(sb, font, toast.Subtitle,
                    new Vector2(panel.X + 18, subY), new Color(235, 228, 210) * alpha, 0f, Vector2.Zero, 0.82f,
                    SpriteEffects.None, 0f);
            }

            y += totalH + Gap;
        }
    }

    private static float ToastAlpha(Toast toast)
    {
        var t = toast.Age / toast.Duration;
        var fadeIn = MathF.Min(1f, toast.Age / 0.2f);
        var fadeOut = t > 0.82f ? (1f - t) / 0.18f : 1f;
        return fadeIn * fadeOut;
    }

    private static (TinySwordsUi.RibbonKind kind, bool pointed) StyleFor(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => (TinySwordsUi.RibbonKind.Teal, false),
        NotificationKind.Warning => (TinySwordsUi.RibbonKind.Gold, true),
        NotificationKind.Danger => (TinySwordsUi.RibbonKind.Red, true),
        NotificationKind.Quest => (TinySwordsUi.RibbonKind.Purple, true),
        _ => (TinySwordsUi.RibbonKind.Steel, false),
    };

    private static Color TitleColor(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => new Color(210, 255, 220),
        NotificationKind.Warning => new Color(255, 240, 180),
        NotificationKind.Danger => new Color(255, 210, 200),
        NotificationKind.Quest => new Color(230, 210, 255),
        _ => new Color(230, 235, 245),
    };
}
