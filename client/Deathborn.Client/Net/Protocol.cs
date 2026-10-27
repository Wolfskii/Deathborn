using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deathborn.Client.Net;

public sealed class CharacterAppearanceData
{
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = "male";

    [JsonPropertyName("skinTone")]
    public string SkinTone { get; set; } = "fair";

    [JsonPropertyName("eyeColor")]
    public string EyeColor { get; set; } = "brown";

    [JsonPropertyName("hairStyleId")]
    public string HairStyleId { get; set; } = "farm-hair-josh-brown";

    [JsonPropertyName("skinHue")]
    public int SkinHue { get; set; }

    [JsonPropertyName("skinSaturation")]
    public int SkinSaturation { get; set; }

    [JsonPropertyName("skinBrightness")]
    public int SkinBrightness { get; set; } = 100;

    [JsonPropertyName("eyeHue")]
    public int EyeHue { get; set; }

    [JsonPropertyName("eyeSaturation")]
    public int EyeSaturation { get; set; }

    [JsonPropertyName("eyeBrightness")]
    public int EyeBrightness { get; set; } = 100;

    [JsonPropertyName("hairHue")]
    public int HairHue { get; set; }

    [JsonPropertyName("hairSaturation")]
    public int HairSaturation { get; set; }

    [JsonPropertyName("hairBrightness")]
    public int HairBrightness { get; set; } = 100;
}

public sealed class Envelope
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

public sealed class TokenResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class WelcomeData
{
    [JsonPropertyName("nodes")]
    public List<ResourceNodeState>? Nodes { get; set; }

    [JsonPropertyName("zones")]
    public List<ZoneInfoDto>? Zones { get; set; }

    [JsonPropertyName("characterId")]
    public long CharacterId { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("race")]
    public string Race { get; set; } = "human";

    [JsonPropertyName("appearance")]
    public CharacterAppearanceData Appearance { get; set; } = new();

    [JsonPropertyName("skills")]
    public Dictionary<string, long>? Skills { get; set; }

    [JsonPropertyName("totalXp")]
    public long TotalXp { get; set; }

    [JsonPropertyName("inventory")]
    public List<InventoryItemState>? Inventory { get; set; }

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }

    [JsonPropertyName("stamina")]
    public double Stamina { get; set; }

    [JsonPropertyName("mana")]
    public double Mana { get; set; }
}

public sealed class InventoryItemState
{
    [JsonPropertyName("slot")]
    public int Slot { get; set; } = -1;

    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("houseId")]
    public long HouseId { get; set; }
}

public sealed class InventoryData
{
    [JsonPropertyName("items")]
    public List<InventoryItemState> Items { get; set; } = [];
}

public sealed class FishResultData
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("weightKg")]
    public double WeightKg { get; set; }

    [JsonPropertyName("baitUsed")]
    public bool BaitUsed { get; set; }
}

public sealed class CookResultData
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public sealed class WorldItemDropState
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("houseId")]
    public long HouseId { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class WorldItemRemovedData
{
    [JsonPropertyName("dropId")]
    public long DropId { get; set; }
}

public sealed class SkillXpGainData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("skillId")]
    public string SkillId { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("xp")]
    public long Xp { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("leveledUp")]
    public bool LeveledUp { get; set; }

    [JsonPropertyName("totalXp")]
    public long TotalXp { get; set; }

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }
}

public sealed class SnapshotData
{
    [JsonPropertyName("tick")]
    public ulong Tick { get; set; }

    [JsonPropertyName("players")]
    public List<PlayerState> Players { get; set; } = [];

    [JsonPropertyName("npcs")]
    public List<NpcState>? Npcs { get; set; }

    [JsonPropertyName("houses")]
    public List<HouseState>? Houses { get; set; }

    [JsonPropertyName("worldItems")]
    public List<WorldItemDropState>? WorldItems { get; set; }

    [JsonPropertyName("worldEvent")]
    public WorldEventState? WorldEvent { get; set; }
}

public sealed class FurnitureItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class HouseState
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("ownerId")]
    public long OwnerId { get; set; }

    [JsonPropertyName("ownerName")]
    public string OwnerName { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("furniture")]
    public List<FurnitureItem>? Furniture { get; set; }

    [JsonPropertyName("crops")]
    public List<FarmCropState>? Crops { get; set; }

    [JsonPropertyName("animals")]
    public List<FarmAnimalState>? Animals { get; set; }
}

public sealed class FarmCropState
{
    [JsonPropertyName("tx")]
    public int Tx { get; set; }

    [JsonPropertyName("ty")]
    public int Ty { get; set; }

    [JsonPropertyName("crop")]
    public string Crop { get; set; } = "";

    [JsonPropertyName("stage")]
    public int Stage { get; set; }

    [JsonPropertyName("watered")]
    public bool Watered { get; set; }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }

    [JsonPropertyName("maxStage")]
    public int MaxStage { get; set; }

    /// <summary>Planted, still growing, and the soil is dry: it will not grow until watered.</summary>
    [JsonPropertyName("thirsty")]
    public bool Thirsty { get; set; }

    /// <summary>Thirsty for a long time (server FarmWiltAfter). Watering revives it.</summary>
    [JsonPropertyName("wilted")]
    public bool Wilted { get; set; }

    /// <summary>Progress through the current growth stage, 0..1.</summary>
    [JsonPropertyName("growth")]
    public double Growth { get; set; }
}

public sealed class FarmAnimalState
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("productReady")]
    public bool ProductReady { get; set; }
}

public sealed class HouseBuiltData
{
    [JsonPropertyName("house")]
    public HouseState House { get; set; } = new();
}

public sealed class HouseRemovedData
{
    [JsonPropertyName("houseId")]
    public long HouseId { get; set; }

    [JsonPropertyName("ownerId")]
    public long OwnerId { get; set; }
}

public sealed class HouseUpdatedData
{
    [JsonPropertyName("house")]
    public HouseState House { get; set; } = new();
}

public sealed class NpcState
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("defId")]
    public string DefId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("disposition")]
    public string? Disposition { get; set; }

    [JsonPropertyName("spriteId")]
    public string? SpriteId { get; set; }

    [JsonPropertyName("isBoss")]
    public bool IsBoss { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("dirX")]
    public double DirX { get; set; }

    [JsonPropertyName("dirY")]
    public double DirY { get; set; }
}

public sealed class WorldEventState
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pvpOff")]
    public bool PvPOff { get; set; }

    [JsonPropertyName("bossCount")]
    public int BossCount { get; set; }
}

public sealed class NpcHitData
{
    [JsonPropertyName("attackerId")]
    public long AttackerId { get; set; }

    [JsonPropertyName("targetNpcId")]
    public long TargetNpcId { get; set; }

    [JsonPropertyName("damage")]
    public int Damage { get; set; }

    [JsonPropertyName("ability")]
    public string Ability { get; set; } = "";

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }
}

public sealed class BossSpawnData
{
    [JsonPropertyName("npcId")]
    public long NpcId { get; set; }

    [JsonPropertyName("defId")]
    public string DefId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class BossDeathData
{
    [JsonPropertyName("npcId")]
    public long NpcId { get; set; }

    [JsonPropertyName("defId")]
    public string DefId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class WorldEventData
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pvpOff")]
    public bool PvPOff { get; set; }

    [JsonPropertyName("bossCount")]
    public int BossCount { get; set; }
}

public sealed class BossActionData
{
    [JsonPropertyName("npcId")]
    public long NpcId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class PlayerState
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }

    [JsonPropertyName("insideHouseId")]
    public long InsideHouseId { get; set; }

    [JsonPropertyName("headCosmetic")]
    public string? HeadCosmetic { get; set; }

    [JsonPropertyName("race")]
    public string Race { get; set; } = "human";

    [JsonPropertyName("appearance")]
    public CharacterAppearanceData Appearance { get; set; } = new();
}

public sealed class MessageData
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

public sealed class ProjectileSpawnData
{
    [JsonPropertyName("ownerId")]
    public long OwnerId { get; set; }

    [JsonPropertyName("spellId")]
    public string? SpellId { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("dirX")]
    public double DirX { get; set; }

    [JsonPropertyName("dirY")]
    public double DirY { get; set; }
}

public sealed class SpellEffectSpawnData
{
    [JsonPropertyName("ownerId")]
    public long OwnerId { get; set; }

    [JsonPropertyName("spellId")]
    public string SpellId { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("dirX")]
    public double DirX { get; set; }

    [JsonPropertyName("dirY")]
    public double DirY { get; set; }
}

public sealed class PlayerActionData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("dirX")]
    public double DirX { get; set; }

    [JsonPropertyName("dirY")]
    public double DirY { get; set; }

    [JsonPropertyName("targetId")]
    public string? TargetId { get; set; }
}

public sealed class ChatMessageData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public sealed class ChatTypingData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("typing")]
    public bool Typing { get; set; }
}

public sealed class AbilityHitSendData
{
    [JsonPropertyName("targetId")]
    public long TargetId { get; set; }

    [JsonPropertyName("damage")]
    public int Damage { get; set; }

    [JsonPropertyName("ability")]
    public string Ability { get; set; } = "";
}

public sealed class PlayerHitData
{
    [JsonPropertyName("attackerId")]
    public long AttackerId { get; set; }

    [JsonPropertyName("targetId")]
    public long TargetId { get; set; }

    [JsonPropertyName("damage")]
    public int Damage { get; set; }

    [JsonPropertyName("ability")]
    public string Ability { get; set; } = "";

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }
}

public sealed class PlayerDeathData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("killerId")]
    public long KillerId { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("dirX")]
    public double DirX { get; set; }

    [JsonPropertyName("dirY")]
    public double DirY { get; set; }
}

public sealed class YouDiedData
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("dirX")]
    public double DirX { get; set; }

    [JsonPropertyName("dirY")]
    public double DirY { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public sealed class HistoryResponse
{
    [JsonPropertyName("lives")]
    public List<HistoryLife> Lives { get; set; } = [];
}

public sealed class HistoryLife
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("killerName")]
    public string KillerName { get; set; } = "";

    [JsonPropertyName("epitaph")]
    public string Epitaph { get; set; } = "";

    [JsonPropertyName("totalXp")]
    public long TotalXp { get; set; }

    [JsonPropertyName("survivalSeconds")]
    public long SurvivalSeconds { get; set; }

    [JsonPropertyName("pvpKills")]
    public int PvpKills { get; set; }

    [JsonPropertyName("wealth")]
    public long Wealth { get; set; }

    [JsonPropertyName("diedAt")]
    public DateTime DiedAt { get; set; }
}

public sealed class HighscoreResponse
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("entries")]
    public List<HighscoreEntry> Entries { get; set; } = [];
}

public sealed class HighscoreEntry
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public long Value { get; set; }
}


/// <summary>A named region from the server's zones.json. Position and size are world pixels.</summary>
public sealed class ZoneInfoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("safe")]
    public bool Safe { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("halfW")]
    public double HalfW { get; set; }

    [JsonPropertyName("halfH")]
    public double HalfH { get; set; }
}

/// <summary>A gatherable tree or rock. Positions are world pixels; Depleted is updated by node_state.</summary>
public sealed class ResourceNodeState
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("depleted")]
    public bool Depleted { get; set; }
}

public sealed class GatherResultData
{
    [JsonPropertyName("nodeId")]
    public long NodeId { get; set; }

    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("skill")]
    public string Skill { get; set; } = "";

    [JsonPropertyName("xp")]
    public long Xp { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("leveledUp")]
    public bool LeveledUp { get; set; }
}

public sealed class NodeStateData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("depleted")]
    public bool Depleted { get; set; }
}

public sealed class PlayerHealData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("ability")]
    public string Ability { get; set; } = "";

    [JsonPropertyName("hp")]
    public double Hp { get; set; }

    [JsonPropertyName("hpMax")]
    public double HpMax { get; set; }
}

public sealed class PlayerBuffData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("buffId")]
    public string BuffId { get; set; } = "";

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    /// <summary>Time left. When 0 on apply, clients use Duration as remaining.</summary>
    [JsonPropertyName("remaining")]
    public double Remaining { get; set; }

    [JsonPropertyName("markTargetId")]
    public long MarkTargetId { get; set; }
}

public sealed class FriendEntryState
{
    [JsonPropertyName("accountId")]
    public long AccountId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("characterId")]
    public long CharacterId { get; set; }

    [JsonPropertyName("online")]
    public bool Online { get; set; }

    [JsonPropertyName("pendingIn")]
    public bool PendingIn { get; set; }

    [JsonPropertyName("pendingOut")]
    public bool PendingOut { get; set; }

    public bool IsConfirmedFriend => !PendingIn && !PendingOut;
}

public sealed class FriendsData
{
    [JsonPropertyName("friends")]
    public List<FriendEntryState> Friends { get; set; } = [];
}

public sealed class PmData
{
    [JsonPropertyName("fromCharacterId")]
    public long FromCharacterId { get; set; }

    [JsonPropertyName("fromName")]
    public string FromName { get; set; } = "";

    [JsonPropertyName("fromAccountId")]
    public long FromAccountId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("outgoing")]
    public bool Outgoing { get; set; }
}

/// <summary>Response from GET /fallen: the most recent deaths, newest first.</summary>
public sealed class FallenResponse
{
    [JsonPropertyName("deaths")]
    public List<FallenDeath> Deaths { get; set; } = [];
}

public sealed class FallenDeath
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("killerName")]
    public string KillerName { get; set; } = "";

    [JsonPropertyName("epitaph")]
    public string Epitaph { get; set; } = "";

    [JsonPropertyName("survivalSeconds")]
    public long SurvivalSeconds { get; set; }

    [JsonPropertyName("diedAt")]
    public DateTime DiedAt { get; set; }
}
