namespace Deathborn.Client;

/// <summary>
/// Per-instance audio preferences in local app data.
/// </summary>
public static class AudioSettings
{
    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deathborn",
        Config.DevInstance > 1 ? $"audio_settings_{Config.DevInstance}.cfg" : "audio_settings.cfg");

    public static bool LoadMuted()
    {
        if (!File.Exists(Path)) return false;

        foreach (var line in File.ReadAllLines(Path))
        {
            var i = line.IndexOf('=');
            if (i <= 0) continue;
            if (!line[..i].Trim().Equals("muted", StringComparison.OrdinalIgnoreCase)) continue;
            return line[(i + 1)..].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static void SaveMuted(bool muted)
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);
        File.WriteAllLines(Path, [$"muted={muted.ToString().ToLowerInvariant()}"]);
    }
}
