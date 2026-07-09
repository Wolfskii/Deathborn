namespace Deathborn.Client.Net;

public static class GameVersion
{
    public static string Current => ProtocolCompat.ClientRelease;

    public static bool IsOlderThan(string latest, string? current = null)
        => Compare(Parse(current ?? Current), Parse(latest)) < 0;

    private static (int Major, int Minor, int Patch) Parse(string raw)
    {
        var core = raw.Trim();
        var dash = core.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
            core = core[..dash];

        var parts = core.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (
            parts.Length > 0 && int.TryParse(parts[0], out var major) ? major : 0,
            parts.Length > 1 && int.TryParse(parts[1], out var minor) ? minor : 0,
            parts.Length > 2 && int.TryParse(parts[2], out var patch) ? patch : 0);
    }

    private static int Compare((int Major, int Minor, int Patch) a, (int Major, int Minor, int Patch) b)
    {
        if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
        if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
        return a.Patch.CompareTo(b.Patch);
    }
}
