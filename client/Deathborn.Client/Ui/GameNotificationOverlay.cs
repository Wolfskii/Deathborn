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

/// <summary>Stacked Farm RPG toast notifications in the top-right corner.</summary>
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
        if (_toasts.Count == 0) return;

        var x = GameViewport.Width - Margin;
        var y = Margin + 52;

        foreach (var toast in _toasts)
        {
            var alpha = ToastAlpha(toast);
            if (alpha <= 0.01f) continue;

            var titleSize = SpriteFontSafe.MeasureString(font, toast.Title);
            var subSize = string.IsNullOrEmpty(toast.Subtitle) ? Vector2.Zero : SpriteFontSafe.MeasureString(font, toast.Subtitle);
            var bodyW = (int)MathF.Max(titleSize.X, subSize.X * 0.86f) + 40;
            var maxW = Math.Max(180, Math.Min(360, GameViewport.Width - Margin * 2));
            var totalW = Math.Min(Math.Max(bodyW, Math.Min(200, maxW)), maxW);
            var totalH = string.IsNullOrEmpty(toast.Subtitle) ? 58 : 88;
            if (y + totalH > GameViewport.Height - Margin)
                break;

            var panel = new Rectangle(x - totalW, y, totalW, totalH);

            FarmRpgUi.DrawWindowPanel(sb, panel, alpha * 0.96f);
            var titlePanel = new Rectangle(panel.X + 8, panel.Y + 7, panel.Width - 16, 34);
            FarmRpgUi.DrawTitle(sb, titlePanel, alpha);
            DrawFittedText(sb, font, toast.Title, titlePanel, TitleColor(toast.Kind) * alpha, 0.92f);

            if (!string.IsNullOrEmpty(toast.Subtitle))
            {
                var subtitlePanel = new Rectangle(panel.X + 12, titlePanel.Bottom + 5, panel.Width - 24, 34);
                FarmRpgUi.DrawInsetPanel(sb, subtitlePanel, alpha);
                DrawFittedText(sb, font, toast.Subtitle, subtitlePanel, FarmRpgUi.InkMuted * alpha, 0.82f);
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

    private static Color TitleColor(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => new Color(52, 108, 66),
        NotificationKind.Warning => new Color(137, 86, 36),
        NotificationKind.Danger => FarmRpgUi.Rust,
        NotificationKind.Quest => new Color(104, 62, 112),
        _ => FarmRpgUi.Ink,
    };

    private static void DrawFittedText(
        SpriteBatch sb, SpriteFont font, string text, Rectangle area, Color color, float preferredScale)
    {
        var size = SpriteFontSafe.MeasureString(font, text);
        var scale = MathF.Min(preferredScale, Math.Max(1, area.Width - 20) / MathF.Max(1f, size.X));
        var pos = new Vector2(
            area.X + (area.Width - size.X * scale) * 0.5f,
            area.Y + (area.Height - size.Y * scale) * 0.5f);
        SpriteFontSafe.DrawString(sb, font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
