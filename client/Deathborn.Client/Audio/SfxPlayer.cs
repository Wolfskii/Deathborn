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
        Preload(GameSfx.Fireball);
        Preload(GameSfx.FireballImpact);
        Preload(GameSfx.IceShard);
        Preload(GameSfx.Heal);
        Preload(GameSfx.HolySpell);
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

    public static void Preload(string path) => GetOrLoad(path);

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

        var sound = GetOrLoad(path);
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

    private static float NextPitch(CachedSound sound, ReadOnlySpan<float> presets)
    {
        if (presets.Length == 0)
            presets = SwordSwingPitches;

        var basePitch = presets[sound.VariationIndex % presets.Length];
        var jitter = (Random.Shared.NextSingle() - 0.5f) * 0.08f;
        var pitch = basePitch + jitter;

        if (MathF.Abs(pitch - sound.LastPitch) < 0.05f)
            pitch += pitch >= sound.LastPitch ? 0.10f : -0.10f;

        pitch = Math.Clamp(pitch, -0.45f, 0.45f);
        sound.LastPitch = pitch;
        return pitch;
    }

    private static float NextVolume(CachedSound sound, float volumeScale, ReadOnlySpan<float> presets)
    {
        if (presets.Length == 0)
            return Math.Clamp(volumeScale, 0f, 1f) * AudioSettings.SfxVolume;

        var preset = presets[sound.VariationIndex % presets.Length];
        sound.VariationIndex++;
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

        cached = new CachedSound(pool);
        Cache[path] = cached;
        return cached;
    }

    private sealed class TimedStop(SoundEffectInstance instance, float remaining)
    {
        public SoundEffectInstance Instance { get; } = instance;
        public float Remaining { get; set; } = remaining;
    }

    private sealed class CachedSound(SoundEffectInstance[] pool)
    {
        public SoundEffectInstance[] Pool { get; } = pool;
        public int VariationIndex;
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
