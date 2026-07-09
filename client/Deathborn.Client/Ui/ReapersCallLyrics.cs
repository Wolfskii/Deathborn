using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Karaoke-style lyrics for "The Reaper's Call" on the login screen.</summary>
public sealed class ReapersCallLyrics
{
    private static readonly Color LyricYellow = new(255, 230, 64);
    private static readonly Color LyricOutline = Color.Black;
    private const float TextScale = 1.14f;
    private const float LineSpacing = 34f;
    private const float FadeSeconds = 1.35f;
    private const float IntroDelay = 2.2f;
    private const float OutroBuffer = 2.5f;

    private readonly LyricCue[] _cues =
    [
        new("The fire fades before the dawn.", 1.05f),
        new("Silent echoes carry on.", 1.0f),
        new("Every path is carved in stone.", 1.0f),
        new("Every soul walks here alone.", 1.05f),
        new("", 0.45f),
        new("Shadows whisper through the trees.", 1.05f),
        new("Calling names upon the breeze.", 1.0f),
        new("Steel will break and kingdoms fall.", 1.05f),
        new("Death has waited for us all.", 1.1f),
        new("", 0.5f),
        new("No hand will lift you from the ground.", 1.1f),
        new("No light remains when hope is drowned.", 1.1f),
        new("", 0.55f),
        new("Born into death.", 0.8f),
        new("Skill decides the end.", 0.85f),
        new("", 0.5f),
        new("One more step into the night.", 1.0f),
        new("One more breath before the end.", 1.05f),
        new("", 0.5f),
        new("Born into death.", 0.8f),
        new("Skill decides the end.", 0.85f),
        new("", 0.55f),
        new("Leave your fear beyond the gate.", 1.05f),
        new("The Reaper waits with endless patience.", 1.15f),
        new("", 0.45f),
        new("Every scar becomes a name.", 1.0f),
        new("Every victory feeds the flame.", 1.05f),
        new("", 0.4f),
        new("Gold will rust and crowns will fade.", 1.05f),
        new("Only legends still remain.", 1.1f),
        new("", 0.45f),
        new("Trust is brittle.", 0.85f),
        new("Steel remembers.", 0.85f),
        new("Every stranger could become your end.", 1.15f),
        new("", 0.5f),
        new("The world keeps nothing.", 0.95f),
        new("The world forgives nothing.", 0.95f),
        new("", 0.45f),
        new("Only those remembered by their deeds remain.", 1.2f),
        new("", 0.55f),
        new("Born into death.", 0.8f),
        new("Skill decides the end.", 0.85f),
        new("", 0.5f),
        new("Write your name across the darkness.", 1.1f),
        new("Before the silence speaks again.", 1.1f),
        new("", 0.5f),
        new("Born into death.", 0.8f),
        new("Skill decides the end.", 0.85f),
        new("", 0.55f),
        new("When the Reaper calls your name,", 1.05f),
        new("Another soul begins the journey.", 1.2f),
        new("", 0.6f),
    ];

    private double[] _starts = [];
    private double _songDuration;
    private double _lastSongTime;
    private int _activeIndex = -1;

    public void Reset(Song song)
    {
        _songDuration = Math.Max(30, song.Duration.TotalSeconds - OutroBuffer);
        BuildTimeline();
        _lastSongTime = 0;
        _activeIndex = -1;
    }

    public void Update()
    {
        if (_starts.Length == 0 || !MusicPlayer.IsPlaying)
            return;

        var t = MusicPlayer.PlayPosition.TotalSeconds;
        if (t + 0.5 < _lastSongTime)
            _activeIndex = -1;

        _lastSongTime = t;

        if (t < IntroDelay)
        {
            _activeIndex = -1;
            return;
        }

        var usable = Math.Max(10, _songDuration - IntroDelay);
        var lyricTime = PositiveModulo(t - IntroDelay, usable);
        _activeIndex = FindActiveIndex(lyricTime);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, int viewportWidth, int viewportHeight)
    {
        if (_starts.Length == 0 || _activeIndex < 0)
            return;

        var cx = viewportWidth / 2f;
        var baseY = viewportHeight - 108f;
        var elapsed = ElapsedInCurrentCue();
        var current = _cues[_activeIndex];

        if (string.IsNullOrEmpty(current.Text))
        {
            var pausePrev = PreviousLyricIndex(_activeIndex);
            if (pausePrev >= 0)
            {
                var fade = 1f - MathHelper.Clamp((float)(elapsed / FadeSeconds), 0f, 1f);
                if (fade > 0.02f)
                    DrawLine(sb, font, _cues[pausePrev].Text, new Vector2(cx, baseY), fade);
            }

            return;
        }

        DrawLine(sb, font, current.Text, new Vector2(cx, baseY), 1f);

        var prevIndex = PreviousLyricIndex(_activeIndex);
        if (prevIndex >= 0)
        {
            var fade = 1f - MathHelper.Clamp((float)(elapsed / FadeSeconds), 0f, 1f);
            if (fade > 0.02f)
                DrawLine(sb, font, _cues[prevIndex].Text, new Vector2(cx, baseY - LineSpacing), fade);
        }
    }

    private void DrawLine(SpriteBatch sb, SpriteFont font, string text, Vector2 center, float alpha)
    {
        var size = SpriteFontSafe.MeasureString(font, text) * TextScale;
        var pos = new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f);
        SpriteFontSafe.DrawOutlined(sb, font, text, pos, LyricYellow, LyricOutline,
            TextScale, outlinePx: 2f, alpha: alpha);
    }

    private double ElapsedInCurrentCue()
    {
        if (_activeIndex < 0 || _activeIndex >= _starts.Length)
            return FadeSeconds;

        var usable = Math.Max(10, _songDuration - IntroDelay);
        var t = PositiveModulo(MusicPlayer.PlayPosition.TotalSeconds - IntroDelay, usable);
        return t - _starts[_activeIndex];
    }

    private int FindActiveIndex(double lyricTime)
    {
        for (var i = _starts.Length - 1; i >= 0; i--)
        {
            if (lyricTime >= _starts[i])
                return i;
        }

        return -1;
    }

    private int PreviousLyricIndex(int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(_cues[i].Text))
                return i;
        }

        return -1;
    }

    private void BuildTimeline()
    {
        var totalWeight = _cues.Sum(c => c.Weight);
        var usable = Math.Max(10, _songDuration - IntroDelay);
        _starts = new double[_cues.Length];

        var t = 0.0;
        for (var i = 0; i < _cues.Length; i++)
        {
            _starts[i] = t;
            t += usable * (_cues[i].Weight / totalWeight);
        }
    }

    private static double PositiveModulo(double value, double modulus)
    {
        if (modulus <= 0) return 0;
        var m = value % modulus;
        return m < 0 ? m + modulus : m;
    }

    private readonly record struct LyricCue(string Text, float Weight);
}
