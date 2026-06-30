namespace Deathborn.Client.Gameplay;

/// <summary>Client-side skill XP state synced from the server.</summary>
public sealed class PlayerSkills
{
    private readonly Dictionary<string, long> _xp = new(StringComparer.Ordinal);

    public PlayerSkills()
    {
        foreach (var id in SkillDefinitions.All)
            _xp[id] = 0;
    }

    public IReadOnlyDictionary<string, long> Xp => _xp;

    public long TotalXp { get; private set; }

    public int TotalLevel
    {
        get
        {
            var sum = 0;
            foreach (var id in SkillDefinitions.All)
                sum += SkillDefinitions.LevelForXp(_xp[id]);
            return sum;
        }
    }

    public int Level(string skillId) => SkillDefinitions.LevelForXp(GetXp(skillId));

    public long GetXp(string skillId) => _xp.GetValueOrDefault(skillId);

    public void ApplySnapshot(IReadOnlyDictionary<string, long>? skills, long totalXp)
    {
        if (skills != null)
        {
            foreach (var id in SkillDefinitions.All)
                _xp[id] = skills.GetValueOrDefault(id);
        }
        TotalXp = totalXp;
    }

    public void ApplyGain(string skillId, long xp, long totalXp)
    {
        if (!string.IsNullOrEmpty(skillId))
            _xp[skillId] = xp;
        TotalXp = totalXp;
    }
}
