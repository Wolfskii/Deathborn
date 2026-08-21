using Deathborn.Client.Gameplay;
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
    /// <summary>Exterior terrain transform scale; non-terrain world art compensates to retain its screen size.</summary>
    public const float ExteriorTerrainFocusScale = 2f;
    public const float InputSendInterval = 0.05f;
    public const float PlayerLerpSpeed = 16f;
    public const float LocalReconcileSpeed = 14f;
    public const float LocalSnapDistance = 72f;

    /// <summary>Base art and world tile size used by the overworld bins.</summary>
    public const float LegacyTileSize = 16f;
    /// <summary>Must match <c>swarovia_mainland_collision.bin</c> tile size.</summary>
    public const float WorldTileSize = 16f;
    private const float Ws = WorldTileSize / LegacyTileSize;

    public const float InteractRange = 72f * Ws;

    /// <summary>Walk speed in px/s — matches server default movement speed.</summary>
    public const float WalkSpeed = 120f;
    public const float RunSpeed = 195f;
    public const float MinStaminaToRun = 4f;
    /// <summary>Keep sprinting until stamina drops below this (avoids run/walk anim flicker).</summary>
    public const float MinStaminaToKeepRunning = 1f;
    public const float RunStaminaDrainPerSecond = 16f;

    public const float HpRegenPerSecond = 1.25f;
    public const float ManaRegenPerSecond = 5f;
    public const float StaminaRegenPerSecond = 10f;
    public const float StaminaRegenWhileRunningPerSecond = 2f;

    /// <summary>Run animation plays faster; walk uses the run sheet at lower speed.</summary>
    public const float WalkAnimSpeed = 0.72f;
    public const float RunAnimSpeed = 1.35f;

    public const float FireballSpeed = 340f * Ws;
    /// <summary>Projectile / hit range from <c>shared/abilities.json</c>.</summary>
    public static float FireballMaxRange => SharedAbilities.HitRange("fireball");
    public const float FireballRadius = 10f * Ws;
    public const float FireballBurstDuration = 0.35f;
    public const float FireballCooldown = 3f;
    public const float FireballCastLockDuration = 0.35f;

    public const float IceShardSpeed = 420f * Ws;
    public static float IceShardMaxRange => SharedAbilities.HitRange("ice_shard");
    public const float IceShardRadius = 8f * Ws;
    public const float IceShardBurstDuration = 0.3f;
    public const float IceShardCooldown = 2.5f;
    public const float IceShardCastLockDuration = 0.28f;

    public const float PoisonBoltSpeed = 380f * Ws;
    public static float PoisonBoltMaxRange => SharedAbilities.HitRange("poison_bolt");
    public const float PoisonBoltRadius = 8f * Ws;
    public const float PoisonBoltBurstDuration = 0.3f;
    public const float PoisonBoltCooldown = 2.8f;
    public const float PoisonBoltCastLockDuration = 0.3f;

    public static float ArcBoltRange => SharedAbilities.HitRange("arc_bolt");
    public const float ArcBoltCooldown = 2f;
    public const float ArcBoltCastLockDuration = 0.25f;

    public const float PoisonCloudCooldown = 5f;
    public const float PoisonCloudCastLockDuration = 0.4f;
    public const float BloodBoltCooldown = 4f;
    public const float BloodBoltCastLockDuration = 0.3f;

    public static int BandageTotalHeal =>
        SharedAbilities.TryGetHoT("bandage", out var total, out _, out _) ? total : 25;
    public static float BandageDuration =>
        SharedAbilities.TryGetHoT("bandage", out _, out var ticks, out var interval)
            ? ticks * interval
            : 5f;
    public const float BandageCooldown = 10f;

    public static int SlashDamage => SharedAbilities.Damage("slash");
    public static int FireballDamage => SharedAbilities.Damage("fireball");
    public static int IceShardDamage => SharedAbilities.Damage("ice_shard");
    public static int PoisonBoltDamage => SharedAbilities.Damage("poison_bolt");
    public static int ArcBoltDamage => SharedAbilities.Damage("arc_bolt");
    public static int PoisonCloudDamage => SharedAbilities.Damage("poison_cloud");
    public static int BloodBoltDamage => SharedAbilities.Damage("blood_bolt");

    public static int ShieldBashDamage => SharedAbilities.Damage("shield_bash");
    public static int WhirlwindDamage => SharedAbilities.Damage("whirlwind");
    public static int WarriorDashDamage => SharedAbilities.Damage("warrior_dash");
    public static int SecondWindHeal => SharedAbilities.Heal("second_wind");

    public const float ShieldBashCooldown = 4f;
    public const float ShieldBashCastLock = 0.35f;
    public static float ShieldBashRange => SharedAbilities.HitRange("shield_bash");

    public const float WhirlwindCooldown = 6f;
    public const float WhirlwindDuration = 3f;
    public static float WhirlwindRadius => SharedAbilities.HitRange("whirlwind");

    public const float WarriorDashCooldown = 5f;
    /// <summary>Charge travel distance — <c>shared/abilities.json</c> warrior_dash hitRange.</summary>
    public static float WarriorDashDistance => SharedAbilities.HitRange("warrior_dash");
    public const float WarriorDashDuration = 0.22f;

    public const float BattleShoutCooldown = 12f;
    /// <summary>Buff length in seconds (8 minutes).</summary>
    public const float BattleShoutDuration = 8f * 60f;

    public const float IronSkinCooldown = 14f;
    /// <summary>Buff length in seconds (10 minutes).</summary>
    public const float IronSkinDuration = 10f * 60f;

    public const float HunterMarkCooldown = 8f;
    /// <summary>Buff length in seconds (15 minutes).</summary>
    public const float HunterMarkDuration = 15f * 60f;
    public static float HunterMarkRange => SharedAbilities.HitRange("hunter_mark");

    public const float SecondWindCooldown = 15f;

    public const float BuffBarDefaultX = 14f;
    public const float BuffBarDefaultY = 88f;

    public const float ChatBubbleDuration = 10f;
    public const int ChatMaxLength = 120;
    /// <summary>World Y offset from feet to torso for projectile spawn.</summary>
    public const float CastTorsoOffsetY = -5.5f * Ws;
    public const float CastSpawnDistance = 4f * Ws;

    public const float MinimapWorldRadius = 400f * Ws;
    public const float MinimapScreenRadius = 58f;
    public const int MinimapMargin = 14;
    public const int MinimapTopMargin = 40;
    public const int MinimapRightMargin = 28;
}
