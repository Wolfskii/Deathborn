using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Deathborn.Client.Audio;

/// <summary>
/// Pooled sound-effect playback with pitch/volume variation for repeated one-shots.
/// Uses SFX mute/volume from <see cref="AudioSettings"/> (independent of music).
/// </summary>
public static class SfxPlayer
{
    private const int PoolSize = 6;

    private static ContentManager? _content;
    private static readonly Dictionary<string, CachedSound> Cache = new(StringComparer.Ordinal);
    private static readonly List<TimedStop> TimedStops = [];

    public static bool IsMuted => AudioSettings.SfxMuted;
    public static float Volume => AudioSettings.SfxVolume;
    public static float DisplayVolume => IsMuted ? 0f : Volume;

    public static void ApplySavedSettings()
    {
        // Values live in AudioSettings; nothing else to sync yet.
    }

    public static void ClearCache()
    {
        foreach (var entry in Cache.Values)
        {
            foreach (var instance in entry.Pool)
                instance.Dispose();
        }
        Cache.Clear();
        TimedStops.Clear();
    }

    public static void Load(ContentManager content)
    {
        _content = content;
        ClearCache();
        Preload(GameSfx.SwordSwing);
        Preload(GameSfx.BattleShout);
        Preload(GameSfx.Fireball);
        Preload(GameSfx.FireballImpact);
        Preload(GameSfx.IceShard);
        Preload(GameSfx.Heal);
        Preload(GameSfx.HolySpell);
        Preload(GameSfx.BushRustle);
        Preload(GameSfx.DoorOpen);
        Preload(GameSfx.DoorClose);
    }

    public static void Update(float dt)
    {
        for (var i = TimedStops.Count - 1; i >= 0; i--)
        {
            var entry = TimedStops[i];
            entry.Remaining -= dt;
            if (entry.Remaining > 0f)
            {
                TimedStops[i] = entry;
                continue;
            }

            if (entry.Instance.State == SoundState.Playing)
                entry.Instance.Stop();
            TimedStops.RemoveAt(i);
        }
    }

    public static void Preload(string path)
    {
        try
        {
            GetOrLoad(path);
        }
        catch
        {
            // Missing / unbuilt content — skip; Play will no-op for this path.
        }
    }

    /// <param name="path">Content path without extension.</param>
    /// <param name="volumeScale">Multiplier before SFX master volume (0–1).</param>
    /// <param name="pitchPresets">Rotating pitch offsets; small random jitter is added each play.</param>
    /// <param name="maxDuration">Optional cap — stops playback after this many seconds (skips silent tails).</param>
    public static void Play(
        string path,
        float volumeScale = 1f,
        ReadOnlySpan<float> pitchPresets = default,
        ReadOnlySpan<float> volumePresets = default,
        float? maxDuration = null)
    {
        if (AudioSettings.SfxMuted || _content is null) return;

        CachedSound sound;
        try
        {
            sound = GetOrLoad(path);
        }
        catch
        {
            return;
        }

        var instance = sound.RentInstance();
        instance.Pitch = NextPitch(sound, pitchPresets);
        instance.Volume = NextVolume(sound, volumeScale, volumePresets);
        instance.Play();

        if (maxDuration is > 0f)
            TimedStops.Add(new TimedStop(instance, maxDuration.Value));
    }

    public static void PlaySwordSwing() =>
        Play(GameSfx.SwordSwing, 1f, SwordSwingPitches, SwordSwingVolumes);

    public static void PlayFireball() =>
        Play(GameSfx.Fireball, 0.95f, FireballPitches, FireballVolumes);

    public static void PlayFireballImpact() =>
        Play(GameSfx.FireballImpact, 0.92f, ImpactPitches, ImpactVolumes);

    public static void PlayIceShard() =>
        Play(GameSfx.IceShard, 0.95f, IceShardPitches, IceShardVolumes, GameSfx.IceShardMaxDuration);

    public static void PlayHeal() =>
        Play(GameSfx.Heal, 0.9f, HealPitches, HealVolumes, GameSfx.HealMaxDuration);

    public static void PlayHolySpell() =>
        Play(GameSfx.HolySpell, 0.88f, HolyPitches, HolyVolumes);

    public static void PlayBattleShout() =>
        Play(GameSfx.BattleShout, 0.95f, BattleShoutPitches, BattleShoutVolumes, GameSfx.BattleShoutMaxDuration);

    public static void PlayBushRustle(float volumeScale = 1f, float? maxDuration = null) =>
        Play(
            GameSfx.BushRustle,
            0.82f * volumeScale,
            BushRustlePitches,
            BushRustleVolumes,
            maxDuration ?? GameSfx.BushRustleMaxDuration);

    public static void StopBushRustle() => Stop(GameSfx.BushRustle);

    /// <summary>Play door-open and return clip duration in seconds (for transition timing).</summary>
    public static float PlayDoorOpen()
    {
        Play(GameSfx.DoorOpen, 1f, DoorPitches, DoorVolumes);
        return GetDuration(GameSfx.DoorOpen);
    }

    /// <summary>Play door-close after the view has switched.</summary>
    public static void PlayDoorClose() =>
        Play(GameSfx.DoorClose, 1f, DoorPitches, DoorVolumes);

    private static readonly float[] DoorPitches = [0f];
    private static readonly float[] DoorVolumes = [1f];

    public static float GetDuration(string path)
    {
        try
        {
            var sound = GetOrLoad(path);
            return sound.DurationSeconds;
        }
        catch
        {
            return 0.45f;
        }
    }

    public static void Stop(string path)
    {
        if (!Cache.TryGetValue(path, out var sound))
            return;

        foreach (var instance in sound.Pool)
        {
            if (instance.State == SoundState.Playing)
                instance.Stop();
        }

        for (var i = TimedStops.Count - 1; i >= 0; i--)
        {
            var stopped = TimedStops[i].Instance;
            foreach (var poolInst in sound.Pool)
            {
                if (!ReferenceEquals(stopped, poolInst))
                    continue;
                TimedStops.RemoveAt(i);
                break;
            }
        }
    }

    // ~±2 semitones in MonoGame pitch units (multiplier = 2^pitch).
    private static readonly float[] SwordSwingPitches = [-0.28f, -0.14f, 0f, 0.14f, 0.28f];
    private static readonly float[] SwordSwingVolumes = [0.88f, 0.96f, 1f, 1.05f, 0.92f];
    private static readonly float[] FireballPitches = [-0.12f, 0f, 0.08f, 0.16f];
    private static readonly float[] FireballVolumes = [0.9f, 1f, 0.95f, 1.05f];
    private static readonly float[] ImpactPitches = [-0.1f, 0f, 0.06f];
    private static readonly float[] ImpactVolumes = [0.92f, 1f, 0.96f];
    private static readonly float[] IceShardPitches = [-0.14f, -0.04f, 0.06f, 0.12f];
    private static readonly float[] IceShardVolumes = [0.9f, 1f, 0.94f, 1.02f];
    private static readonly float[] HealPitches = [-0.08f, 0f, 0.1f];
    private static readonly float[] HealVolumes = [0.88f, 1f, 0.94f];
    private static readonly float[] HolyPitches = [-0.06f, 0f, 0.08f];
    private static readonly float[] HolyVolumes = [0.9f, 1f, 0.96f];
    private static readonly float[] BattleShoutPitches = [-0.12f, -0.04f, 0.04f, 0.12f];
    private static readonly float[] BattleShoutVolumes = [0.9f, 1f, 0.95f, 1.05f];
    private static readonly float[] BushRustlePitches = [-0.18f, -0.06f, 0.06f, 0.14f];
    private static readonly float[] BushRustleVolumes = [0.85f, 0.95f, 1f, 0.9f];

    private static float NextPitch(CachedSound sound, ReadOnlySpan<float> presets)
    {
        if (presets.Length == 0)
            presets = SwordSwingPitches;

        var min = presets[0];
        var max = presets[0];
        for (var i = 1; i < presets.Length; i++)
        {
            min = MathF.Min(min, presets[i]);
            max = MathF.Max(max, presets[i]);
        }

        // Random pitch in the preset range (not a sequential climb through the table).
        var pitch = min + Random.Shared.NextSingle() * (max - min);
        pitch += (Random.Shared.NextSingle() - 0.5f) * 0.04f;

        // Nudge away from the previous play so rapid hits don't sound identical.
        if (!float.IsNaN(sound.LastPitch) && MathF.Abs(pitch - sound.LastPitch) < 0.06f)
        {
            var roomUp = max - sound.LastPitch;
            var roomDown = sound.LastPitch - min;
            if (roomUp >= roomDown && roomUp > 0.06f)
                pitch = sound.LastPitch + 0.08f + Random.Shared.NextSingle() * MathF.Min(0.12f, roomUp);
            else if (roomDown > 0.06f)
                pitch = sound.LastPitch - 0.08f - Random.Shared.NextSingle() * MathF.Min(0.12f, roomDown);
            else
                pitch = sound.LastPitch + (Random.Shared.NextSingle() < 0.5f ? -0.1f : 0.1f);
        }

        pitch = Math.Clamp(pitch, -0.45f, 0.45f);
        sound.LastPitch = pitch;
        return pitch;
    }

    private static float NextVolume(CachedSound sound, float volumeScale, ReadOnlySpan<float> presets)
    {
        if (presets.Length == 0)
            return Math.Clamp(volumeScale, 0f, 1f) * AudioSettings.SfxVolume;

        var preset = presets[Random.Shared.Next(presets.Length)];
        return Math.Clamp(volumeScale * preset, 0f, 1f) * AudioSettings.SfxVolume;
    }

    private static CachedSound GetOrLoad(string path)
    {
        if (Cache.TryGetValue(path, out var cached))
            return cached;

        if (_content is null)
            throw new InvalidOperationException("SfxPlayer.Load must be called before playing sounds.");

        var effect = _content.Load<SoundEffect>(path);
        var pool = new SoundEffectInstance[PoolSize];
        for (var i = 0; i < PoolSize; i++)
            pool[i] = effect.CreateInstance();

        cached = new CachedSound(pool, (float)effect.Duration.TotalSeconds);
        Cache[path] = cached;
        return cached;
    }

    private sealed class TimedStop(SoundEffectInstance instance, float remaining)
    {
        public SoundEffectInstance Instance { get; } = instance;
        public float Remaining { get; set; } = remaining;
    }

    private sealed class CachedSound(SoundEffectInstance[] pool, float durationSeconds)
    {
        public SoundEffectInstance[] Pool { get; } = pool;
        public float DurationSeconds { get; } = MathF.Max(0.05f, durationSeconds);
        public float LastPitch = float.NaN;
        private int _next;

        public SoundEffectInstance RentInstance()
        {
            var instance = Pool[_next];
            _next = (_next + 1) % Pool.Length;
            if (instance.State == SoundState.Playing)
                instance.Stop();
            return instance;
        }
    }
}
