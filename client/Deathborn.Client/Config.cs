using Deathborn.Client.Net;

namespace Deathborn.Client;

public static class Config
{
    /// <summary>1+ when spawned by <c>task dev:all</c>; 0 for a single client.</summary>
    public static int DevInstance { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("DEATHBORN_INSTANCE"), out var n) && n > 0 ? n : 0;

    /// <summary>
    /// When true, a process restart (e.g. <c>task dev:all</c> rebuild) skips the login screen and
    /// reconnects with saved credentials for this dev instance.
    /// </summary>
    public static bool DevAutoRestore { get; } =
        DevInstance > 0
        || string.Equals(Environment.GetEnvironmentVariable("DEATHBORN_SKIP_UPDATE"), "1", StringComparison.Ordinal)
        || string.Equals(Environment.GetEnvironmentVariable("DEATHBORN_SKIP_UPDATE"), "true", StringComparison.OrdinalIgnoreCase);

    public static string HttpBase => ServerEndpoints.HttpBase;
    public static string WsBase => ServerEndpoints.WsBase;
    public const int DefaultWidth = 1280;
    public const int DefaultHeight = 800;
    /// <summary>World camera scale at <see cref="DefaultWidth"/>×<see cref="DefaultHeight"/>; higher = closer.</summary>
    public const float WorldZoomBase = 1.85f;
    public const float WorldZoomMin = 1.0f;
    public const float WorldZoomMax = 2.6f;
    public const float InputSendInterval = 0.05f;
    public const float PlayerLerpSpeed = 16f;
    public const float LocalReconcileSpeed = 14f;
    public const float LocalSnapDistance = 72f;

    /// <summary>Legacy design tile size; world bins may use a larger value.</summary>
    public const float LegacyTileSize = 16f;
    /// <summary>Must match <c>realik_collision.bin</c> tile size.</summary>
    public const float WorldTileSize = 32f;
    private const float Ws = WorldTileSize / LegacyTileSize;

    public const float InteractRange = 72f * Ws;

    /// <summary>Walk speed in px/s — matches server default movement speed.</summary>
    public const float WalkSpeed = 120f;
    public const float RunSpeed = 195f;
    public const float MinStaminaToRun = 4f;
    public const float RunStaminaDrainPerSecond = 16f;

    public const float HpRegenPerSecond = 1.25f;
    public const float ManaRegenPerSecond = 5f;
    public const float StaminaRegenPerSecond = 10f;
    public const float StaminaRegenWhileRunningPerSecond = 2f;

    /// <summary>Run animation plays faster; walk uses the run sheet at lower speed.</summary>
    public const float WalkAnimSpeed = 0.72f;
    public const float RunAnimSpeed = 1.35f;

    public const float FireballSpeed = 340f * Ws;
    public const float FireballMaxRange = 520f * Ws;
    public const float FireballRadius = 10f * Ws;
    public const float FireballBurstDuration = 0.35f;
    public const float FireballCooldown = 3f;
    public const float FireballCastLockDuration = 0.35f;

    public const float IceShardSpeed = 420f * Ws;
    public const float IceShardMaxRange = 480f * Ws;
    public const float IceShardRadius = 8f * Ws;
    public const float IceShardBurstDuration = 0.3f;
    public const float IceShardCooldown = 2.5f;
    public const float IceShardCastLockDuration = 0.28f;

    public const float ArcBoltRange = 120f * Ws;
    public const float ArcBoltCooldown = 2f;
    public const float ArcBoltCastLockDuration = 0.25f;

    public const float PoisonCloudCooldown = 5f;
    public const float PoisonCloudCastLockDuration = 0.4f;
    public const float BloodBoltCooldown = 4f;
    public const float BloodBoltCastLockDuration = 0.3f;

    public const int BandageTotalHeal = 25;
    public const float BandageDuration = 5f;
    public const float BandageCooldown = 10f;

    public const int SlashDamage = 5;
    public const int FireballDamage = 25;
    public const int IceShardDamage = 18;
    public const int ArcBoltDamage = 14;
    public const int PoisonCloudDamage = 6;
    public const int BloodBoltDamage = 22;

    public const int ShieldBashDamage = 12;
    public const int WhirlwindDamage = 8;
    public const int WarriorDashDamage = 10;
    public const int SecondWindHeal = 15;

    public const float ShieldBashCooldown = 4f;
    public const float ShieldBashCastLock = 0.35f;
    public const float ShieldBashRange = 56f * Ws;

    public const float WhirlwindCooldown = 6f;
    public const float WhirlwindDuration = 3f;
    public const float WhirlwindRadius = 38f * Ws;

    public const float WarriorDashCooldown = 5f;
    public const float WarriorDashDistance = 100f * Ws;
    public const float WarriorDashDuration = 0.22f;

    public const float BattleShoutCooldown = 12f;
    public const float BattleShoutDuration = 8f;

    public const float IronSkinCooldown = 14f;
    public const float IronSkinDuration = 10f;

    public const float HunterMarkCooldown = 8f;
    public const float HunterMarkDuration = 900f;
    public const float HunterMarkRange = 200f * Ws;

    public const float SecondWindCooldown = 15f;

    public const float BuffBarDefaultX = 14f;
    public const float BuffBarDefaultY = 88f;

    public const float ChatBubbleDuration = 10f;
    public const int ChatMaxLength = 120;
    /// <summary>World Y offset from feet to torso for projectile spawn.</summary>
    public const float CastTorsoOffsetY = -11f * Ws;
    public const float CastSpawnDistance = 8f * Ws;

    public const float MinimapWorldRadius = 400f * Ws;
    public const float MinimapScreenRadius = 58f;
    public const int MinimapMargin = 14;
    public const int MinimapTopMargin = 40;
    public const int MinimapRightMargin = 28;
}
