using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Spotify-style live lyrics synced to the active track via a companion .srt
/// next to the song under Content/Audio/Songs/.
/// </summary>
public sealed class LiveLyricsSystem
{
    private static readonly Color LyricYellow = new(255, 230, 64);
    private static readonly Color LyricOutline = Color.Black;
    private const float TextScale = 1.14f;
    private const float LineSpacing = 34f;
    private const float FadeSeconds = 1.35f;
    /// <summary>If the silence before the next cue is shorter than this, hold the current line until the next starts.</summary>
    private const double GapExtendSeconds = 1.0;

    private static readonly Regex TimestampLine = new(
        @"^(\d{2}):(\d{2}):(\d{2})[,.](\d{1,3})\s*-->\s*(\d{2}):(\d{2}):(\d{2})[,.](\d{1,3})",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, LyricCue[]> CueCache = new(StringComparer.OrdinalIgnoreCase);

    private LyricCue[] _cues = [];
    private string? _boundSongPath;
    private int _activeIndex = -1;
    private double _lastSongTime;

    public bool HasCues => _cues.Length > 0;
    public string? BoundSongPath => _boundSongPath;

    /// <param name="songContentPath">Content path without extension, e.g. <c>Audio/Songs/My Track</c>.</param>
    public void Bind(string songContentPath)
    {
        _boundSongPath = songContentPath;
        _cues = GetOrLoadCues(songContentPath);
        _lastSongTime = 0;
        _activeIndex = -1;
    }

    public void Clear()
    {
        _boundSongPath = null;
        _cues = [];
        _lastSongTime = 0;
        _activeIndex = -1;
    }

    public void Update()
    {
        if (_cues.Length == 0 || MusicPlayer.IsMuted || !MusicPlayer.IsPlaying)
        {
            _activeIndex = -1;
            return;
        }

        var t = MusicPlayer.PlayPosition.TotalSeconds;
        if (t + 0.5 < _lastSongTime)
            _activeIndex = -1;
        _lastSongTime = t;
        _activeIndex = FindActiveIndex(t);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, int viewportWidth, int viewportHeight)
    {
        if (MusicPlayer.IsMuted || _cues.Length == 0 || _activeIndex < 0)
            return;

        var cx = viewportWidth / 2f;
        var baseY = viewportHeight - 108f;
        var current = _cues[_activeIndex];
        var elapsed = MusicPlayer.PlayPosition.TotalSeconds - current.Start;

        DrawLine(sb, font, current.Text, new Vector2(cx, baseY), 1f);

        var prevIndex = _activeIndex - 1;
        if (prevIndex >= 0)
        {
            var fade = 1f - MathHelper.Clamp((float)(elapsed / FadeSeconds), 0f, 1f);
            if (fade > 0.02f)
                DrawLine(sb, font, _cues[prevIndex].Text, new Vector2(cx, baseY - LineSpacing), fade);
        }
    }

    private static void DrawLine(SpriteBatch sb, SpriteFont font, string text, Vector2 center, float alpha)
    {
        var size = SpriteFontSafe.MeasureString(font, text) * TextScale;
        var pos = new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f);
        SpriteFontSafe.DrawOutlined(sb, font, text, pos, LyricYellow, LyricOutline,
            TextScale, outlinePx: 2f, alpha: alpha);
    }

    private int FindActiveIndex(double t)
    {
        for (var i = _cues.Length - 1; i >= 0; i--)
        {
            var c = _cues[i];
            if (t >= c.Start && t < c.End)
                return i;
        }

        return -1;
    }

    private static LyricCue[] GetOrLoadCues(string songContentPath)
    {
        if (CueCache.TryGetValue(songContentPath, out var cached))
            return cached;

        var path = ResolveSrtPath(songContentPath);
        if (path is null || !File.Exists(path))
        {
            CueCache[songContentPath] = [];
            return [];
        }

        var raw = ParseSrt(File.ReadAllText(path));
        if (raw.Count == 0)
        {
            CueCache[songContentPath] = [];
            return [];
        }

        // Hold short gaps (< 1s) until the next line so the lyric strip does not go blank.
        for (var i = 0; i < raw.Count - 1; i++)
        {
            var gap = raw[i + 1].Start - raw[i].End;
            if (gap > 0 && gap < GapExtendSeconds)
                raw[i] = raw[i] with { End = raw[i + 1].Start };
        }

        var cues = raw.ToArray();
        CueCache[songContentPath] = cues;
        return cues;
    }

    /// <summary>
    /// Finds <c>{songFileName}.srt</c> beside the song under Content/Audio/Songs
    /// (apostrophe / punctuation differences are tolerated).
    /// </summary>
    private static string? ResolveSrtPath(string songContentPath)
    {
        var songFile = Path.GetFileName(songContentPath.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(songFile))
            return null;

        var targetKey = NormalizeName(songFile);
        var songsDirs = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Content", "Audio", "Songs"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "Audio", "Songs")),
        };

        foreach (var dir in songsDirs)
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*.srt"))
            {
                if (NormalizeName(Path.GetFileNameWithoutExtension(file)) == targetKey)
                    return file;
            }
        }

        return null;
    }

    /// <summary>Lowercase + strip punctuation so curly/straight apostrophes still match.</summary>
    private static string NormalizeName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString().Trim();
    }

    private static List<LyricCue> ParseSrt(string text)
    {
        var cues = new List<LyricCue>();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                i++;
                continue;
            }

            if (int.TryParse(line, out _))
            {
                i++;
                if (i >= lines.Length) break;
                line = lines[i].Trim();
            }

            var m = TimestampLine.Match(line);
            if (!m.Success)
            {
                i++;
                continue;
            }

            var start = ParseTimestamp(m, 1);
            var end = ParseTimestamp(m, 5);
            i++;

            var textLines = new List<string>();
            while (i < lines.Length && lines[i].Trim().Length > 0 && !TimestampLine.IsMatch(lines[i].Trim())
                   && !int.TryParse(lines[i].Trim(), out _))
            {
                textLines.Add(lines[i].Trim());
                i++;
            }

            var body = string.Join(" ", textLines).Trim();
            if (body.Length > 0 && end > start)
                cues.Add(new LyricCue(body, start, end));
        }

        return cues;
    }

    private static double ParseTimestamp(Match m, int groupOffset)
    {
        var h = int.Parse(m.Groups[groupOffset].Value, CultureInfo.InvariantCulture);
        var min = int.Parse(m.Groups[groupOffset + 1].Value, CultureInfo.InvariantCulture);
        var sec = int.Parse(m.Groups[groupOffset + 2].Value, CultureInfo.InvariantCulture);
        var frac = m.Groups[groupOffset + 3].Value.PadRight(3, '0')[..3];
        var ms = int.Parse(frac, CultureInfo.InvariantCulture);
        return h * 3600 + min * 60 + sec + ms / 1000.0;
    }

    private readonly record struct LyricCue(string Text, double Start, double End);
}
