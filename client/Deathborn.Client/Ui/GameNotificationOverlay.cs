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

/// <summary>
/// Toasts stacked on the right, below the minimap. Repeats of the same message
/// merge into one card with a count instead of piling up, and at most a few show.
/// </summary>
public sealed class GameNotificationOverlay
{
    private sealed class Toast
    {
        public string Title = "";
        public string Subtitle = "";
        public NotificationKind Kind;
        public float Age;
        public float Duration;
        public int Count = 1;
    }

    private const int MaxVisible = 4;
    private const int Width = 280;
    private const int Pad = 12;
    private const int AccentWidth = 4;
    private const int Gap = 8;
    private const int RightMargin = 14;
    private const float FadeIn = 0.15f;
    private const float FadeOut = 0.6f;

    private readonly List<Toast> _toasts = [];

    /// <summary>Top of the toast stack: just below the minimap and its compass letters.</summary>
    private static int TopY => Config.MinimapTopMargin + (int)(Config.MinimapScreenRadius * 2) + 34;

    public void Push(string title, string? subtitle = null, NotificationKind kind = NotificationKind.Info, float duration = 4.5f)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        var safeTitle = SpriteFontSafe.Filter(title.Trim());
        if (safeTitle.Length == 0) return;
        var safeSubtitle = SpriteFontSafe.Filter(subtitle?.Trim());

        // Same message still on screen: refresh it and count the repeat.
        var existing = _toasts.Find(t => t.Title == safeTitle && t.Subtitle == safeSubtitle);
        if (existing != null)
        {
            existing.Count++;
            existing.Age = MathF.Min(existing.Age, FadeIn);
            existing.Duration = MathF.Max(existing.Duration, duration);
            _toasts.Remove(existing);
            _toasts.Insert(0, existing);
            return;
        }

        _toasts.Insert(0, new Toast { Title = safeTitle, Subtitle = safeSubtitle, Kind = kind, Duration = duration });
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

        var width = Math.Min(Width, GameViewport.Width - RightMargin * 2);
        var x = GameViewport.Width - RightMargin - width;
        var y = TopY;
        var textW = width - Pad * 2 - AccentWidth - 6;

        foreach (var toast in _toasts)
        {
            var alpha = Alpha(toast);
            var lines = UiText.Wrap(font, toast.Subtitle, textW);
            if (lines.Count > 2)
                lines = [lines[0], UiText.Truncate(font, lines[1] + " " + lines[2], textW)];
            var height = Pad * 2 + font.LineSpacing + lines.Count * (font.LineSpacing + 1) + (lines.Count > 0 ? 3 : 0);
            if (y + height > GameViewport.Height - 100)
                break;

            var card = new Rectangle(x, y, width, height);
            FarmRpgUi.DrawCompactPanel(sb, card, alpha);
            DrawPrimitives.FillRect(sb,
                new Rectangle(card.X + Pad, card.Y + Pad, AccentWidth, card.Height - Pad * 2),
                AccentColor(toast.Kind) * alpha);

            var textX = card.X + Pad + AccentWidth + 8;
            var title = toast.Count > 1 ? $"{toast.Title}  x{toast.Count}" : toast.Title;
            UiText.Draw(sb, font, UiText.Truncate(font, title, textW), new Vector2(textX, card.Y + Pad), FarmRpgUi.Ink * alpha);

            var ly = card.Y + Pad + font.LineSpacing + 3;
            foreach (var line in lines)
            {
                UiText.Draw(sb, font, line, new Vector2(textX, ly), FarmRpgUi.InkMuted * alpha);
                ly += font.LineSpacing + 1;
            }

            y += height + Gap;
        }
    }

    private static float Alpha(Toast toast)
    {
        var fadeIn = MathF.Min(1f, toast.Age / FadeIn);
        var remaining = toast.Duration - toast.Age;
        var fadeOut = remaining < FadeOut ? MathF.Max(0f, remaining / FadeOut) : 1f;
        return fadeIn * fadeOut;
    }

    private static Color AccentColor(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => FarmRpgUi.Positive,
        NotificationKind.Warning => FarmRpgUi.Warning,
        NotificationKind.Danger => FarmRpgUi.Danger,
        NotificationKind.Quest => new Color(122, 78, 140),
        _ => FarmRpgUi.InkMuted,
    };
}
