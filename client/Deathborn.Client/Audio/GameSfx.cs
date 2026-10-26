namespace Deathborn.Client.Audio;

/// <summary>Content paths for one-shot sound effects (no file extension).</summary>
public static class GameSfx
{
    public const string SwordSwing = "Audio/Sfx/Battle/sword-swing-sfx";
    public const string BattleShout = "Audio/Sfx/Battle/battle-shout-sfx";
    public const string Fireball = "Audio/Sfx/Battle/fireball-sfx";
    public const string FireballImpact = "Audio/Sfx/Battle/fireball-impact-sfx";
    public const string IceShard = "Audio/Sfx/Battle/ice-shard-sfx";
    public const string Heal = "Audio/Sfx/Heal/heal-sfx";
    public const string HolySpell = "Audio/Sfx/Heal/holy-spell-sfx";

    public const string BushRustle = "Audio/Sfx/World/bush-rustling-sfx";
    public const string DoorOpen = "Audio/Sfx/World/opening-door-sfx";
    public const string DoorClose = "Audio/Sfx/World/close-door-sfx";

    // Fishing ("Fisheefects" by Memoraphile @ You're Perfect Studio, CC0; scripts/install_fishing_sfx.py).
    public const string FishingCast = "Audio/Sfx/Fishing/fishing-cast-splash";
    public const string FishingBite = "Audio/Sfx/Fishing/fishing-bite";
    public const string FishingHook = "Audio/Sfx/Fishing/fishing-hook";
    public const string FishingReelLoop = "Audio/Sfx/Fishing/fishing-reel-loop";
    public const string FishingCatch = "Audio/Sfx/Fishing/fishing-catch";
    public const string FishingEscape = "Audio/Sfx/Fishing/fishing-escape";

    /// <summary>Source files have long silent tails — stop playback early.</summary>
    public const float HealMaxDuration = 2f;
    public const float IceShardMaxDuration = 3f;
    public const float BattleShoutMaxDuration = 1.6f;
    public const float BushRustleMaxDuration = 1.4f;
    /// <summary>Step-in without walking — shorter tail so it does not linger.</summary>
    public const float BushRustleEnterMaxDuration = 0.55f;
}
