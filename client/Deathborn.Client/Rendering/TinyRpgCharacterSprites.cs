using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>100×100 horizontal strip animation from the Tiny RPG Character pack.</summary>
public sealed class TinyRpgStripAnimation
{
    public const int FrameSize = 100;
    public static readonly Vector2 FootAnchor = new(FrameSize * 0.5f, FrameSize - 6f);
    private const float DamageFlashDuration = 0.14f;

    /// <summary>World Y offset from draw anchor to lowest opaque body pixel (excludes shadow below anchor).</summary>
    private readonly float _footSortOffsetFromAnchor;

    private readonly Texture2D _walk;
    private readonly Texture2D? _idle;
    private readonly Texture2D _walkFlash;
    private readonly Texture2D? _idleFlash;
    private readonly int _walkFrames;
    private readonly int _idleFrames;
    private float _timer;
    private int _frame;
    private bool _moving;
    private bool _meleeActive;
    private float _meleeTimer;
    private int _meleeFrame;
    private readonly bool _alwaysAnimate;
    private bool _damageFlashActive;
    private float _damageFlashTimer;

    public TinyRpgStripAnimation(
        Texture2D walk,
        Texture2D walkFlash,
        Texture2D? idle = null,
        Texture2D? idleFlash = null,
        float footSortOffsetFromAnchor = -38f,
        bool alwaysAnimate = false)
    {
        _walk = walk;
        _walkFlash = walkFlash;
        _idle = idle;
        _idleFlash = idleFlash;
        _footSortOffsetFromAnchor = footSortOffsetFromAnchor;
        _alwaysAnimate = alwaysAnimate;
        _walkFrames = Math.Max(1, walk.Width / FrameSize);
        _idleFrames = idle != null ? Math.Max(1, idle.Width / FrameSize) : _walkFrames;
    }

    public float GetFootSortY(Vector2 position, float worldScale) =>
        position.Y + _footSortOffsetFromAnchor * worldScale;

    public void BeginMeleeAttack()
    {
        _meleeActive = true;
        _meleeTimer = 0f;
        _meleeFrame = 0;
    }

    public bool IsMeleeActive => _meleeActive;

    public void BeginDamageFlash()
    {
        _damageFlashActive = true;
        _damageFlashTimer = 0f;
    }

    public void Update(float dt, Vector2 facing, bool moving, float animSpeed = 1f)
    {
        if (_damageFlashActive)
        {
            _damageFlashTimer += dt;
            if (_damageFlashTimer >= DamageFlashDuration)
                _damageFlashActive = false;
        }

        if (_meleeActive)
        {
            _meleeTimer += dt;
            _meleeFrame = Math.Min(2, (int)(_meleeTimer / 0.08f));
            if (_meleeTimer >= 0.45f)
                _meleeActive = false;
            // Keep flapping under melee for flying sprites.
            if (!_alwaysAnimate)
                return;
        }

        _moving = moving || _alwaysAnimate;

        if (!_moving)
        {
            _timer = 0;
            _frame = 0;
            return;
        }

        var frameDuration = (_alwaysAnimate ? 0.09f : 0.12f) / MathF.Max(0.1f, animSpeed);
        _timer += dt;
        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            _frame = (_frame + 1) % _walkFrames;
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale, Vector2 facing)
    {
        var tex = _walk;
        var flash = _walkFlash;
        var frames = _walkFrames;
        var frame = _meleeActive && !_alwaysAnimate ? _meleeFrame : _frame;
        var effects = SpriteEffects.None;

        if (facing.X < -0.15f)
            effects = SpriteEffects.FlipHorizontally;
        else if (!_alwaysAnimate && !_moving && !_meleeActive && facing.Y < -0.15f && _idle != null)
        {
            // Idle sheet only when standing still and facing north.
            tex = _idle;
            flash = _idleFlash ?? _walkFlash;
            frames = _idleFrames;
            frame = 0;
        }

        frame = Math.Clamp(frame, 0, frames - 1);
        var src = new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize);
        if (src.Right > tex.Width)
            src.Width = Math.Max(1, tex.Width - src.X);
        if (src.Bottom > tex.Height)
            src.Height = Math.Max(1, tex.Height - src.Y);

        // Hit react for packs without a damage clip: opaque white silhouette (no fade).
        if (_damageFlashActive)
        {
            sb.Draw(flash, screenPos, src, Color.White, 0f, FootAnchor, scale, effects, 0f);
            return;
        }

        sb.Draw(tex, screenPos, src, tint, 0f, FootAnchor, scale, effects, 0f);
    }

    public TinyRpgStripAnimation Clone() =>
        new(_walk, _walkFlash, _idle, _idleFlash, _footSortOffsetFromAnchor, _alwaysAnimate);
}

/// <summary>Body AABB in unscaled art px relative to the draw anchor (50, 94).</summary>
public readonly record struct TinyRpgBodyHitMetrics(float CenterOffsetFromAnchorY, float HalfWidth, float HalfHeight);

public static class TinyRpgCharacterSprites
{
    private static readonly Dictionary<string, TinyRpgStripAnimation> Animations = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Texture2D> OwnedFlashTextures = [];
    /// <summary>Measured from walk frame 0 opaque pixels (anchor = bottom-center of 100×100 cell).</summary>
    private static readonly Dictionary<string, TinyRpgBodyHitMetrics> BodyHitMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["skeleton"] = new(-45f, 13f, 8f),
        ["slime"] = new(-43f, 12f, 6f),
        ["orc"] = new(-45f, 11f, 8f),
        ["bat"] = new(-30f, 14f, 28f),
        ["soldier"] = new(-45f, 9f, 11f),
        ["priest"] = new(-45f, 9f, 11f),
        ["wizard"] = new(-45f, 9f, 11f),
        ["archer"] = new(-45f, 9f, 11f),
    };
    private static readonly Dictionary<string, float> FootSortOffsetFromAnchor = new(StringComparer.OrdinalIgnoreCase)
    {
        ["soldier"] = -35f,
        ["priest"] = -35f,
        ["wizard"] = -35f,
        ["archer"] = -35f,
        ["bat"] = -45f,
    };
    private const float DefaultFootSortOffsetFromAnchor = -38f;
    private static readonly Dictionary<string, float> HeadTopOffsetFromAnchor = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bat"] = -55f,
        ["soldier"] = -55f,
        ["priest"] = -55f,
        ["wizard"] = -55f,
        ["archer"] = -55f,
        ["slime"] = -48f,
    };
    private const float DefaultHeadTopOffsetFromAnchor = -52f;
    private static bool _loaded;
    private static int _bindVersion;

    public static bool IsLoaded => _loaded;

    /// <summary>Increments on each <see cref="Load"/> so entity clones can detect stale textures after device reset.</summary>
    public static int BindVersion => _bindVersion;

    public static void Load(ContentManager content)
    {
        DisposeOwnedFlashTextures();
        Animations.Clear();
        _bindVersion++;
        Register(content, "skeleton");
        Register(content, "slime");
        Register(content, "orc");
        Register(content, "bat");
        Register(content, "soldier");
        Register(content, "priest");
        Register(content, "wizard");
        Register(content, "archer");
        _loaded = Animations.Count > 0;
    }

    public static TinyRpgStripAnimation? Get(string spriteId)
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

    /// <summary>Screen/world Y offset from foot anchor to top of sprite art.</summary>
    public static float GetHeadTopOffsetFromAnchor(string spriteId) =>
        HeadTopOffsetFromAnchor.TryGetValue(spriteId, out var offset)
            ? offset
            : DefaultHeadTopOffsetFromAnchor;

    public static bool TryGetBodyHitMetrics(string spriteId, out TinyRpgBodyHitMetrics metrics) =>
        BodyHitMetrics.TryGetValue(spriteId, out metrics);

    private static void Register(ContentManager content, string id)
    {
        var walkPath = $"Characters/Rpg/{Capitalize(id)}_Walk";
        var idlePath = $"Characters/Rpg/{Capitalize(id)}_Idle";
        try
        {
            var walk = content.Load<Texture2D>(walkPath);
            Texture2D? idle = null;
            try { idle = content.Load<Texture2D>(idlePath); } catch { /* optional */ }
            var walkFlash = CreateSolidWhiteCopy(walk);
            Texture2D? idleFlash = idle != null ? CreateSolidWhiteCopy(idle) : null;
            var footOffset = FootSortOffsetFromAnchor.TryGetValue(id, out var offset)
                ? offset
                : DefaultFootSortOffsetFromAnchor;
            var alwaysAnimate = id.Equals("bat", StringComparison.OrdinalIgnoreCase);
            Animations[id] = new TinyRpgStripAnimation(walk, walkFlash, idle, idleFlash, footOffset, alwaysAnimate);
        }
        catch
        {
            // Asset may not be in content pipeline yet.
        }
    }

    /// <summary>
    /// Premultiplied opaque-white silhouette of <paramref name="src"/> (keeps alpha, forces RGB white).
    /// </summary>
    private static Texture2D CreateSolidWhiteCopy(Texture2D src)
    {
        var data = new Color[src.Width * src.Height];
        src.GetData(data);
        for (var i = 0; i < data.Length; i++)
        {
            var a = data[i].A;
            data[i] = new Color(a, a, a, a);
        }

        var copy = new Texture2D(src.GraphicsDevice, src.Width, src.Height);
        copy.SetData(data);
        OwnedFlashTextures.Add(copy);
        return copy;
    }

    private static void DisposeOwnedFlashTextures()
    {
        foreach (var tex in OwnedFlashTextures)
            tex.Dispose();
        OwnedFlashTextures.Clear();
    }

    private static string Capitalize(string id) =>
        char.ToUpperInvariant(id[0]) + id[1..];
}
