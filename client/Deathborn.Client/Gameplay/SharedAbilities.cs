using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Authoritative combat numbers from <c>shared/abilities.json</c> (same file the server embeds).
/// Prefer these over hard-coded duplicates in <see cref="Config"/>.
/// </summary>
public static class SharedAbilities
{
    private static readonly Dictionary<string, AbilityDef> ById = Load();

    public static int Damage(string id) => ById.TryGetValue(id, out var d) ? d.Combat?.Damage ?? 0 : 0;

    public static float HitRange(string id) => ById.TryGetValue(id, out var d) ? d.Combat?.HitRange ?? 0f : 0f;

    public static int Heal(string id) => ById.TryGetValue(id, out var d) ? d.Combat?.Heal ?? 0 : 0;

    public static bool TryGetHoT(string id, out int totalHeal, out int ticks, out float intervalSec)
    {
        totalHeal = 0;
        ticks = 0;
        intervalSec = 0f;
        if (!ById.TryGetValue(id, out var d) || d.HoT == null)
            return false;
        totalHeal = d.HoT.TotalHeal;
        ticks = d.HoT.Ticks;
        intervalSec = d.HoT.IntervalSec;
        return ticks > 0;
    }

    private static Dictionary<string, AbilityDef> Load()
    {
        var asm = typeof(SharedAbilities).Assembly;
        using var stream = asm.GetManifestResourceStream("Deathborn.Client.Gameplay.abilities.json")
            ?? throw new InvalidOperationException(
                "Embedded shared/abilities.json missing — check Deathborn.Client.csproj EmbeddedResource.");
        var file = JsonSerializer.Deserialize<AbilityFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Invalid shared/abilities.json");
        if (file.Version < 1 || file.Abilities == null || file.Abilities.Count == 0)
            throw new InvalidOperationException("shared/abilities.json has no abilities");

        var map = new Dictionary<string, AbilityDef>(StringComparer.Ordinal);
        foreach (var def in file.Abilities)
        {
            if (string.IsNullOrEmpty(def.Id))
                throw new InvalidOperationException("abilities.json entry missing id");
            if (!map.TryAdd(def.Id, def))
                throw new InvalidOperationException($"abilities.json duplicate id '{def.Id}'");
        }
        return map;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class AbilityFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("abilities")]
        public List<AbilityDef>? Abilities { get; set; }
    }

    private sealed class AbilityDef
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("combat")]
        public CombatSpec? Combat { get; set; }

        [JsonPropertyName("hot")]
        public HoTSpec? HoT { get; set; }
    }

    private sealed class CombatSpec
    {
        [JsonPropertyName("damage")]
        public int Damage { get; set; }

        [JsonPropertyName("hitRange")]
        public float HitRange { get; set; }

        [JsonPropertyName("heal")]
        public int Heal { get; set; }
    }

    private sealed class HoTSpec
    {
        [JsonPropertyName("totalHeal")]
        public int TotalHeal { get; set; }

        [JsonPropertyName("ticks")]
        public int Ticks { get; set; }

        [JsonPropertyName("intervalSec")]
        public float IntervalSec { get; set; }
    }
}
