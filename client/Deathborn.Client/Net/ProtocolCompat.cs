using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deathborn.Client.Net;

/// <summary>
/// Client/server protocol compatibility (from shared/protocol.json).
/// Bump <see cref="Protocol"/> only when wire format or API breaks; cosmetic client updates can keep the same protocol.
/// </summary>
public static class ProtocolCompat
{
    private static readonly ProtocolSpec Spec = LoadSpec();

    public static int Protocol => Spec.Protocol;
    public static int MinClient => Spec.MinClientProtocol;
    public static int MaxClient => Spec.MaxClientProtocol;
    public static string ClientRelease { get; } = typeof(ProtocolCompat).Assembly.GetName().Version?.ToString(3) ?? "dev";

    public static string HttpHeader => "X-Deathborn-Protocol";

    public static (bool Ok, string Message) CheckAgainstServer(ServerVersionInfo server)
    {
        if (Protocol < server.MinClient)
        {
            return (false,
                $"Outdated client! Please update Deathborn (client {ClientRelease}, protocol {Protocol}; " +
                $"server {server.Release} accepts protocols {server.MinClient}–{server.MaxClient}).");
        }

        if (Protocol > server.MaxClient)
        {
            return (false,
                $"Outdated server! Cannot join with this client (client {ClientRelease}, protocol {Protocol}; " +
                $"server {server.Release} is protocol {server.Protocol}, accepts {server.MinClient}–{server.MaxClient}).");
        }

        return (true, "");
    }

    public static string FormatMismatch(VersionMismatchResponse mismatch) =>
        string.IsNullOrWhiteSpace(mismatch.Message) ? mismatch.Error ?? "Incompatible version." : mismatch.Message;

    private static ProtocolSpec LoadSpec()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Deathborn.Client.Protocol.protocol.json")
            ?? throw new InvalidOperationException("Embedded protocol.json missing");
        return JsonSerializer.Deserialize<ProtocolSpec>(stream)
            ?? throw new InvalidOperationException("Invalid embedded protocol.json");
    }

    private sealed class ProtocolSpec
    {
        [JsonPropertyName("protocol")]
        public int Protocol { get; init; }

        [JsonPropertyName("minClientProtocol")]
        public int MinClientProtocol { get; init; }

        [JsonPropertyName("maxClientProtocol")]
        public int MaxClientProtocol { get; init; }
    }
}

public sealed class ServerVersionInfo
{
    [JsonPropertyName("release")]
    public string Release { get; init; } = "";

    [JsonPropertyName("protocol")]
    public int Protocol { get; init; }

    [JsonPropertyName("minClient")]
    public int MinClient { get; init; }

    [JsonPropertyName("maxClient")]
    public int MaxClient { get; init; }
}

public sealed class VersionMismatchResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("clientProtocol")]
    public int ClientProtocol { get; init; }

    [JsonPropertyName("serverProtocol")]
    public int ServerProtocol { get; init; }
}
