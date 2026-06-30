namespace Deathborn.Client.Gameplay;

public static class HotbarEntry
{
    public const string IdKey = "id";
    public const string CooldownKey = "cooldown";

    public static float GetCooldown(Dictionary<string, object>? entry)
    {
        if (entry == null || !entry.TryGetValue(CooldownKey, out var value)) return 0f;
        return value switch
        {
            float f => MathF.Max(0f, f),
            double d => MathF.Max(0f, (float)d),
            int i => MathF.Max(0f, i),
            _ => 0f,
        };
    }
}
