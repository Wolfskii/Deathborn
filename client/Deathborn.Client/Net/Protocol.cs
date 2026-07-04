using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deathborn.Client.Net;

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
    [JsonPropertyName("characterId")]
    public long CharacterId { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("skills")]
    public Dictionary<string, long>? Skills { get; set; }

    [JsonPropertyName("totalXp")]
    public long TotalXp { get; set; }

    [JsonPropertyName("inventory")]
    public List<InventoryItemState>? Inventory { get; set; }
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
