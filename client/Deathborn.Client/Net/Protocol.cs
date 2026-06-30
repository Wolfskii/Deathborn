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
}

public sealed class SnapshotData
{
    [JsonPropertyName("tick")]
    public ulong Tick { get; set; }

    [JsonPropertyName("players")]
    public List<PlayerState> Players { get; set; } = [];
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
