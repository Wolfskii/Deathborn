using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Deathborn.Client.Net;

public sealed class ClientUpdateService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    public static bool IsDisabled =>
#if DEBUG
        true ||
#endif
        string.Equals(Environment.GetEnvironmentVariable("DEATHBORN_SKIP_UPDATE"), "1", StringComparison.Ordinal)
        || string.Equals(Environment.GetEnvironmentVariable("DEATHBORN_SKIP_UPDATE"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task<ClientUpdateOffer?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        if (IsDisabled)
            return null;

        await ServerEndpoints.EnsureResolvedAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{ServerEndpoints.HttpBase}/client/update");
        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (resp.StatusCode == HttpStatusCode.NoContent)
            return null;

        resp.EnsureSuccessStatusCode();
        var manifest = await resp.Content.ReadFromJsonAsync<ClientUpdateManifest>(cancellationToken: ct);
        if (manifest is not { Enabled: true } || string.IsNullOrWhiteSpace(manifest.Latest))
            return null;

        var rid = ClientPlatform.RuntimeId;
        if (!manifest.Platforms.TryGetValue(rid, out var artifact) || string.IsNullOrWhiteSpace(artifact.Url))
            return null;

        if (!GameVersion.IsOlderThan(manifest.Latest))
            return null;

        return new ClientUpdateOffer(manifest.Latest, manifest.Notes ?? "", rid, artifact);
    }

    public async Task<string> DownloadAsync(
        ClientUpdateOffer offer,
        IProgress<double>? progress,
        CancellationToken ct = default)
    {
        var ext = offer.Artifact.Kind switch
        {
            "installer" => ".exe",
            "dmg" => ".dmg",
            _ => ".bin",
        };
        var dir = Path.Combine(Path.GetTempPath(), "deathborn-updates");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"Deathborn-{offer.Latest}-{offer.RuntimeId}{ext}");

        using var resp = await Http.GetAsync(offer.Artifact.Url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength;
        await using var input = await resp.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(path);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
            if (total is > 0)
                progress?.Report(readTotal / (double)total.Value);
        }

        progress?.Report(1);
        return path;
    }

    public static void LaunchInstaller(string path, string kind)
    {
        var psi = new ProcessStartInfo(path) { UseShellExecute = true };
        Process.Start(psi);
    }
}

public sealed class ClientUpdateOffer
{
    public ClientUpdateOffer(string latest, string notes, string runtimeId, ClientUpdateArtifact artifact)
    {
        Latest = latest;
        Notes = notes;
        RuntimeId = runtimeId;
        Artifact = artifact;
    }

    public string Latest { get; }
    public string Notes { get; }
    public string RuntimeId { get; }
    public ClientUpdateArtifact Artifact { get; }
}

public sealed class ClientUpdateManifest
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("latest")]
    public string Latest { get; init; } = "";

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("platforms")]
    public Dictionary<string, ClientUpdateArtifact> Platforms { get; init; } = new();
}

public sealed class ClientUpdateArtifact
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "";

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }
}
