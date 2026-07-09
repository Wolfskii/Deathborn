using System.Net.Http;

namespace Deathborn.Client.Net;

/// <summary>
/// Resolves API/WebSocket base URLs: explicit override, then local dev server, then production.
/// </summary>
public static class ServerEndpoints
{
    public const string ProductionHost = "api.deathborn.wolfskii.dev";
    public const string DefaultDevPort = "8080";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static bool _resolved;

    public static string HttpBase { get; private set; } = "";
    public static string WsBase { get; private set; } = "";
    public static bool IsLocal { get; private set; }
    public static string DisplayName { get; private set; } = "";

    public static async Task EnsureResolvedAsync(CancellationToken ct = default)
    {
        if (_resolved) return;

        await Gate.WaitAsync(ct);
        try
        {
            if (_resolved) return;
            await ResolveCoreAsync(ct);
            _resolved = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task ResolveCoreAsync(CancellationToken ct)
    {
        var forced = Environment.GetEnvironmentVariable("DEATHBORN_SERVER_URL");
        if (!string.IsNullOrWhiteSpace(forced))
        {
            Apply(forced.Trim().TrimEnd('/'), label: forced.Trim(), isLocal: false);
            return;
        }

        var port = Environment.GetEnvironmentVariable("DEATHBORN_PORT")
            ?? Environment.GetEnvironmentVariable("PORT")
            ?? DefaultDevPort;
        var localHttp = $"http://127.0.0.1:{port}";

        if (await ProbeHealthAsync(localHttp, ct))
        {
            Apply(localHttp, label: $"local:{port}", isLocal: true);
            return;
        }

        Apply($"https://{ProductionHost}", label: ProductionHost, isLocal: false);
    }

    private static void Apply(string httpBase, string label, bool isLocal)
    {
        HttpBase = httpBase.TrimEnd('/');
        WsBase = ToWsBase(HttpBase);
        DisplayName = label;
        IsLocal = isLocal;
    }

    private static string ToWsBase(string httpBase)
    {
        var uri = new Uri(httpBase);
        var scheme = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        return $"{scheme}://{uri.Authority}/ws";
    }

    private static async Task<bool> ProbeHealthAsync(string httpBase, CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(750) };
            using var response = await client.GetAsync($"{httpBase.TrimEnd('/')}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
