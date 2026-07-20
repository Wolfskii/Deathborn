using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG slime enemy — 32×32 horizontal strips (idle / walk / damage / dead).</summary>
public sealed class FarmRpgSlimeAnimation
{
    public const int FrameSize = 32;
    /// <summary>Draw origin at bottom-center of the cell (matches Farm RPG feet).</summary>
    public static readonly Vector2 FootAnchor = new(FrameSize * 0.5f, FrameSize - 2f);

    private readonly Texture2D _idle;
    private readonly Texture2D _walk;
    private readonly Texture2D _damage;
    private readonly Texture2D? _dead;
    private readonly int _idleFrames;
    private readonly int _walkFrames;
    private readonly int _damageFrames;
    private readonly int _deadFrames;
    private readonly float _footSortOffsetFromAnchor;

    private float _timer;
    private int _frame;
    private bool _moving;
    private bool _meleeActive;
    private float _meleeTimer;

    public FarmRpgSlimeAnimation(
        Texture2D idle,
        Texture2D walk,
        Texture2D damage,
        Texture2D? dead = null,
        float footSortOffsetFromAnchor = -6f)
    {
        _idle = idle;
        _walk = walk;
        _damage = damage;
        _dead = dead;
        _footSortOffsetFromAnchor = footSortOffsetFromAnchor;
        _idleFrames = Math.Max(1, idle.Width / FrameSize);
        _walkFrames = Math.Max(1, walk.Width / FrameSize);
        _damageFrames = Math.Max(1, damage.Width / FrameSize);
        _deadFrames = dead != null ? Math.Max(1, dead.Width / FrameSize) : 1;
    }

    public float GetFootSortY(Vector2 position, float worldScale) =>
        position.Y + _footSortOffsetFromAnchor * worldScale;

    public void BeginMeleeAttack()
    {
        _meleeActive = true;
        _meleeTimer = 0f;
        _frame = 0;
        _timer = 0f;
    }

    public void Update(float dt, Vector2 facing, bool moving, float animSpeed = 1f)
    {
        if (_meleeActive)
        {
            var meleeFrameDuration = 0.09f / MathF.Max(0.1f, animSpeed);
            _meleeTimer += dt;
            _frame = Math.Min(_damageFrames - 1, (int)(_meleeTimer / meleeFrameDuration));
            if (_meleeTimer >= meleeFrameDuration * _damageFrames)
                _meleeActive = false;
            return;
        }

        _moving = moving;
        var texFrames = moving ? _walkFrames : _idleFrames;
        var frameDuration = (moving ? 0.11f : 0.16f) / MathF.Max(0.1f, animSpeed);
        _timer += dt;
        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            _frame = (_frame + 1) % texFrames;
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale, Vector2 facing)
    {
        Texture2D tex;
        int frames;
        int frame;

        if (_meleeActive)
        {
            tex = _damage;
            frames = _damageFrames;
            frame = _frame;
        }
        else if (_moving)
        {
            tex = _walk;
            frames = _walkFrames;
            frame = _frame;
        }
        else
        {
            tex = _idle;
            frames = _idleFrames;
            frame = _frame;
        }

        frame = Math.Clamp(frame, 0, frames - 1);
        var effects = facing.X < -0.15f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var src = new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize);
        if (src.Right > tex.Width)
            src.Width = Math.Max(1, tex.Width - src.X);
        sb.Draw(tex, screenPos, src, tint, 0f, FootAnchor, scale, effects, 0f);
    }

    public FarmRpgSlimeAnimation Clone() =>
        new(_idle, _walk, _damage, _dead, _footSortOffsetFromAnchor);
}

public readonly record struct FarmRpgSlimeVisuals(
    float DisplayScale,
    float Radius,
    float HitCenterY,
    float HitHalfW,
    float HitHalfH,
    float HeadTopOffset);

/// <summary>Loads Farm RPG slime variants: sprite id <c>slime_{color}_{size}</c>.</summary>
public static class FarmRpgSlimeSprites
{
    public static readonly string[] Colors = ["blue", "black", "golden", "green", "pink", "purple"];
    public static readonly string[] Sizes = ["small", "normal", "big"];

    private static readonly Dictionary<string, FarmRpgSlimeAnimation> Animations = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static bool IsLoaded => _loaded;

    public static bool IsSlimeSpriteId(string? spriteId) =>
        !string.IsNullOrEmpty(spriteId)
        && spriteId.StartsWith("slime_", StringComparison.OrdinalIgnoreCase);

    public static void Load(ContentManager content)
    {
        Animations.Clear();
        foreach (var color in Colors)
        {
            foreach (var size in Sizes)
                Register(content, color, size);
        }
        _loaded = Animations.Count > 0;
    }

    public static FarmRpgSlimeAnimation? Get(string spriteId)
    {
        if (string.IsNullOrEmpty(spriteId)) return null;
        return Animations.TryGetValue(spriteId, out var anim) ? anim : null;
    }

    public static float GetFootSortY(string spriteId, Vector2 position, float worldScale)
    {
        if (Animations.TryGetValue(spriteId, out var anim))
            return anim.GetFootSortY(position, worldScale);
        return position.Y;
    }

    public static bool TryGetVisuals(string spriteId, out FarmRpgSlimeVisuals visuals)
    {
        visuals = default;
        if (!TryParse(spriteId, out _, out var size))
            return false;
        visuals = VisualsFor(size);
        return true;
    }

    public static bool TryGetBodyHitMetrics(string spriteId, out TinyRpgBodyHitMetrics metrics)
    {
        metrics = default;
        if (!TryGetVisuals(spriteId, out var v))
            return false;
        metrics = new TinyRpgBodyHitMetrics(v.HitCenterY, v.HitHalfW, v.HitHalfH);
        return true;
    }

    public static float GetHeadTopOffsetFromAnchor(string spriteId) =>
        TryGetVisuals(spriteId, out var v) ? v.HeadTopOffset : -18f;

    public static bool TryParse(string spriteId, out string color, out string size)
    {
        color = "";
        size = "";
        if (string.IsNullOrEmpty(spriteId)) return false;
        // slime_{color}_{size}
        var parts = spriteId.Split('_');
        if (parts.Length != 3 || !parts[0].Equals("slime", StringComparison.OrdinalIgnoreCase))
            return false;
        color = parts[1].ToLowerInvariant();
        size = parts[2].ToLowerInvariant();
        return Array.IndexOf(Colors, color) >= 0 && Array.IndexOf(Sizes, size) >= 0;
    }

    public static FarmRpgSlimeVisuals VisualsFor(string size) => size switch
    {
        "small" => new(1.55f, 9f, -9f, 6f, 5f, -16f),
        "big" => new(2.75f, 16f, -14f, 11f, 8f, -24f),
        _ => new(2.05f, 12f, -12f, 8f, 6f, -20f), // normal
    };

    private static void Register(ContentManager content, string color, string size)
    {
        var folder = $"{color}_{size}";
        var id = $"slime_{color}_{size}";
        var basePath = $"Characters/FarmRpg/Enemies/slimes/{folder}";
        try
        {
            var idle = content.Load<Texture2D>($"{basePath}/idle");
            var walk = content.Load<Texture2D>($"{basePath}/walk");
            var damage = content.Load<Texture2D>($"{basePath}/damage");
            Texture2D? dead = null;
            try { dead = content.Load<Texture2D>($"{basePath}/dead"); } catch { /* optional */ }
            Animations[id] = new FarmRpgSlimeAnimation(idle, walk, damage, dead);
        }
        catch
        {
            // Asset may not be built yet.
        }
    }
}
