namespace Deathborn.Client;

public static class Config
{
    /// <summary>1 or 2 when spawned by <c>task dev:all</c>; 0 for a single client.</summary>
    public static int DevInstance { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("DEATHBORN_INSTANCE"), out var n) && n > 0 ? n : 0;

    public const string HttpBase = "http://127.0.0.1:8080";
    public const string WsBase = "ws://127.0.0.1:8080/ws";
    public const int DefaultWidth = 1280;
    public const int DefaultHeight = 720;
    /// <summary>World camera scale at <see cref="DefaultWidth"/>×<see cref="DefaultHeight"/>; higher = closer.</summary>
    public const float WorldZoomBase = 1.85f;
    public const float WorldZoomMin = 1.0f;
    public const float WorldZoomMax = 2.6f;
    public const float InputSendInterval = 0.05f;
    public const float PlayerLerpSpeed = 12f;
    public const float InteractRange = 72f;

    public const float WorldTileSize = 16f;
    public const float FireballSpeed = 340f;
    public const float FireballMaxRange = 520f;
    public const float FireballRadius = 10f;
    public const float FireballBurstDuration = 0.35f;
    public const float FireballCooldown = 3f;
    public const float FireballCastLockDuration = 0.35f;

    public const int SlashDamage = 5;
    public const int FireballDamage = 25;
    public const int HealAmount = 25;
    public const int BandageAmount = 15;
    public const float HitBlinkDuration = 2.5f;
    public const float HitBlinkInterval = 0.12f;

    public const float ChatBubbleDuration = 10f;
    public const int ChatMaxLength = 120;
    /// <summary>World Y offset from feet to torso for projectile spawn.</summary>
    public const float CastTorsoOffsetY = -11f;
    public const float CastSpawnDistance = 8f;

    public const float MinimapWorldRadius = 400f;
    public const float MinimapScreenRadius = 58f;
    public const int MinimapMargin = 14;
}
