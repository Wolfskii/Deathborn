namespace Deathborn.Client;

/// <summary>
/// Per-instance audio preferences in local app data (music and SFX are independent).
/// </summary>
public static class AudioSettings
{
    public const float DefaultMusicVolume = 0.55f;
    public const float DefaultSfxVolume = 0.75f;

    public static bool MusicMuted { get; private set; }
    public static float MusicVolume { get; private set; } = DefaultMusicVolume;
    public static bool SfxMuted { get; private set; }
    public static float SfxVolume { get; private set; } = DefaultSfxVolume;

    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deathborn",
        Config.DevInstance > 1 ? $"audio_settings_{Config.DevInstance}.cfg" : "audio_settings.cfg");

    public static void Load()
    {
        MusicMuted = false;
        MusicVolume = DefaultMusicVolume;
        SfxMuted = false;
        SfxVolume = DefaultSfxVolume;
        if (!File.Exists(Path)) return;

        foreach (var line in File.ReadAllLines(Path))
        {
            var i = line.IndexOf('=');
            if (i <= 0) continue;
            var key = line[..i].Trim();
            var val = line[(i + 1)..].Trim();
            switch (key.ToLowerInvariant())
            {
                case "muted":
                case "music_muted":
                    MusicMuted = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "volume":
                case "music_volume":
                    if (TryParseVolume(val, out var musicVol))
                        MusicVolume = musicVol;
                    break;
                case "sfx_muted":
                    SfxMuted = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "sfx_volume":
                    if (TryParseVolume(val, out var sfxVol))
                        SfxVolume = sfxVol;
                    break;
            }
        }
    }

    public static void SetMusic(bool muted, float volume, bool save = true)
    {
        MusicMuted = muted;
        MusicVolume = Math.Clamp(volume, 0f, 1f);
        if (save) Persist();
    }

    public static void SetSfx(bool muted, float volume, bool save = true)
    {
        SfxMuted = muted;
        SfxVolume = Math.Clamp(volume, 0f, 1f);
        if (save) Persist();
    }

    private static void Persist()
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        File.WriteAllLines(Path,
        [
            $"music_muted={MusicMuted.ToString().ToLowerInvariant()}",
            $"music_volume={MusicVolume.ToString(inv)}",
            $"sfx_muted={SfxMuted.ToString().ToLowerInvariant()}",
            $"sfx_volume={SfxVolume.ToString(inv)}",
        ]);
    }

    private static bool TryParseVolume(string val, out float volume)
    {
        if (float.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out volume))
        {
            volume = Math.Clamp(volume, 0f, 1f);
            return true;
        }

        volume = 0f;
        return false;
    }
}
