namespace Deathborn.Client;

/// <summary>
/// Optional "remember me" login persistence (email + password) in local app data.
/// </summary>
public static class SavedLogin
{
    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deathborn",
        Config.DevInstance > 0 ? $"saved_login_{Config.DevInstance}.cfg" : "saved_login.cfg");

    public static void Save(bool remember, string email, string password)
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);

        if (!remember)
        {
            if (File.Exists(Path)) File.Delete(Path);
            return;
        }

        File.WriteAllLines(Path,
        [
            "remember=true",
            "email=" + email.Trim().ToLowerInvariant(),
            "password=" + password,
        ]);
    }

    public static (string Email, string Password)? LoadIfRemembered()
    {
        if (!File.Exists(Path)) return null;

        var lines = File.ReadAllLines(Path);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var i = line.IndexOf('=');
            if (i <= 0) continue;
            map[line[..i].Trim()] = line[(i + 1)..];
        }

        if (!map.TryGetValue("remember", out var remember) || remember != "true")
            return null;
        if (!map.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
            return null;
        if (!map.TryGetValue("password", out var password))
            return null;

        return (email, password);
    }

    public static bool HasRemembered() => LoadIfRemembered() != null;
}
