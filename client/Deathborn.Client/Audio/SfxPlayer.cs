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

    public static bool IsMuted => AudioSettings.SfxMuted;
    public static float Volume => AudioSettings.SfxVolume;
    public static float DisplayVolume => IsMuted ? 0f : Volume;

    public static void ApplySavedSettings()
    {
        // Values live in AudioSettings; nothing else to sync yet.
    }

    public static void Load(ContentManager content)
    {
        _content = content;
        Preload(GameSfx.SwordSwing);
    }

    public static void Preload(string path) => GetOrLoad(path);

    /// <param name="path">Content path without extension.</param>
    /// <param name="volumeScale">Multiplier before SFX master volume (0–1).</param>
    /// <param name="pitchPresets">Rotating pitch offsets; small random jitter is added each play.</param>
    public static void Play(
        string path,
        float volumeScale = 1f,
        ReadOnlySpan<float> pitchPresets = default,
        ReadOnlySpan<float> volumePresets = default)
    {
        if (AudioSettings.SfxMuted || _content is null) return;

        var sound = GetOrLoad(path);
        var instance = sound.RentInstance();
        instance.Pitch = NextPitch(sound, pitchPresets);
        instance.Volume = NextVolume(sound, volumeScale, volumePresets);
        instance.Play();
    }

    public static void PlaySwordSwing() =>
        Play(GameSfx.SwordSwing, 1f, SwordSwingPitches, SwordSwingVolumes);

    // ~±2 semitones in MonoGame pitch units (multiplier = 2^pitch).
    private static readonly float[] SwordSwingPitches = [-0.28f, -0.14f, 0f, 0.14f, 0.28f];
    private static readonly float[] SwordSwingVolumes = [0.88f, 0.96f, 1f, 1.05f, 0.92f];

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
