namespace Deathborn.Client.Gameplay;

public sealed class ActiveBuff
{
    public required string Id { get; init; }
    public float Remaining { get; set; }
    public float Duration { get; set; }
    public long MarkTargetId { get; set; }
}

/// <summary>Tracks active buffs on the local player for HUD display.</summary>
public sealed class BuffTracker
{
    private readonly List<ActiveBuff> _buffs = [];

    public IReadOnlyList<ActiveBuff> Active => _buffs;

    public void Apply(string buffId, float duration, long markTargetId = 0)
    {
        if (duration <= 0f)
        {
            Remove(buffId);
            return;
        }

        foreach (var b in _buffs)
        {
            if (b.Id != buffId) continue;
            b.Remaining = duration;
            b.Duration = duration;
            b.MarkTargetId = markTargetId;
            return;
        }

        _buffs.Add(new ActiveBuff
        {
            Id = buffId,
            Remaining = duration,
            Duration = duration,
            MarkTargetId = markTargetId,
        });
    }

    public void Remove(string buffId) => _buffs.RemoveAll(b => b.Id == buffId);

    public void Update(float dt)
    {
        for (var i = _buffs.Count - 1; i >= 0; i--)
        {
            _buffs[i].Remaining -= dt;
            if (_buffs[i].Remaining <= 0f)
                _buffs.RemoveAt(i);
        }
    }
}
