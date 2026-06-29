namespace Deathborn.Client;

public static class Config
{
    /// <summary>1 or 2 when spawned by <c>task dev:all</c>; 0 for a single client.</summary>
    public static int DevInstance { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("DEATHBORN_INSTANCE"), out var n) && n > 0 ? n : 0;

    public const string HttpBase = "http://127.0.0.1:8080";
    public const string WsBase = "ws://127.0.0.1:8080/ws";
    public const int Width = 1280;
    public const int Height = 720;
    public const float InputSendInterval = 0.05f;
    public const float PlayerLerpSpeed = 12f;
    public const float InteractRange = 72f;
}
