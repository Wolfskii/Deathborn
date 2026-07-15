using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Deathborn.Client.Audio;

/// <summary>
/// Pooled sound-effect playback with pitch/volume variation for repeated one-shots.
/// Respects the same mute/volume settings as <see cref="MusicPlayer"/>.
/// </summary>
public static class SfxPlayer
{
    private const int PoolSize = 6;

    private static ContentManager? _content;
    private static readonly Dictionary<string, CachedSound> Cache = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        _content = content;
        Preload(GameSfx.SwordSwing);
    }

    public static void Preload(string path) => GetOrLoad(path);

    /// <param name="path">Content path without extension.</param>
    /// <param name="volumeScale">Multiplier before master volume (0–1).</param>
    /// <param name="pitchPresets">Rotating pitch offsets; small random jitter is added each play.</param>
    public static void Play(
        string path,
        float volumeScale = 1f,
        ReadOnlySpan<float> pitchPresets = default)
    {
        if (MusicPlayer.IsMuted || _content is null) return;

        var sound = GetOrLoad(path);
        var instance = sound.RentInstance();
        instance.Volume = Math.Clamp(volumeScale, 0f, 1f) * MusicPlayer.Volume;
        instance.Pitch = NextPitch(sound, pitchPresets);
        instance.Play();
    }

    public static void PlaySwordSwing() =>
        Play(GameSfx.SwordSwing, 0.9f, SwordSwingPitches);

    private static readonly float[] SwordSwingPitches = [-0.10f, -0.04f, 0f, 0.05f, 0.10f];

    private static float NextPitch(CachedSound sound, ReadOnlySpan<float> presets)
    {
        if (presets.Length == 0)
            presets = SwordSwingPitches;

        var basePitch = presets[sound.PitchIndex % presets.Length];
        sound.PitchIndex++;

        // Slight jitter so rapid identical swings still differ a little.
        var jitter = (Random.Shared.NextSingle() - 0.5f) * 0.06f;
        var pitch = basePitch + jitter;

        // Avoid repeating the exact same pitch twice in a row when possible.
        if (MathF.Abs(pitch - sound.LastPitch) < 0.015f)
            pitch += pitch >= sound.LastPitch ? 0.04f : -0.04f;

        pitch = Math.Clamp(pitch, -0.35f, 0.35f);
        sound.LastPitch = pitch;
        return pitch;
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
        public int PitchIndex;
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
