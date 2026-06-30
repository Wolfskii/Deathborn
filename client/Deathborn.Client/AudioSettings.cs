namespace Deathborn.Client;

/// <summary>
/// Per-instance audio preferences in local app data.
/// </summary>
public static class AudioSettings
{
    private const float DefaultVolume = 0.55f;

    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deathborn",
        Config.DevInstance > 1 ? $"audio_settings_{Config.DevInstance}.cfg" : "audio_settings.cfg");

    public static bool LoadMuted()
    {
        TryRead(out var muted, out _);
        return muted;
    }

    public static float LoadVolume()
    {
        TryRead(out _, out var volume);
        return volume;
    }

    public static void Save(bool muted, float volume)
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);
        File.WriteAllLines(Path,
        [
            $"muted={muted.ToString().ToLowerInvariant()}",
            $"volume={volume.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        ]);
    }

    private static void TryRead(out bool muted, out float volume)
    {
        muted = false;
        volume = DefaultVolume;
        if (!File.Exists(Path)) return;

        foreach (var line in File.ReadAllLines(Path))
        {
            var i = line.IndexOf('=');
            if (i <= 0) continue;
            var key = line[..i].Trim();
            var val = line[(i + 1)..].Trim();
            if (key.Equals("muted", StringComparison.OrdinalIgnoreCase))
                muted = val.Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("volume", StringComparison.OrdinalIgnoreCase)
                     && float.TryParse(val, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out var v))
                volume = Math.Clamp(v, 0f, 1f);
        }
    }
}
