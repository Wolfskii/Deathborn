using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>100×100 horizontal strip animation from the Tiny RPG Character pack.</summary>
public sealed class TinyRpgStripAnimation
{
    public const int FrameSize = 100;
    public static readonly Vector2 FootAnchor = new(FrameSize * 0.5f, FrameSize - 6f);

    private readonly Texture2D _walk;
    private readonly Texture2D? _idle;
    private readonly int _walkFrames;
    private readonly int _idleFrames;
    private float _timer;
    private int _frame;

    public TinyRpgStripAnimation(Texture2D walk, Texture2D? idle = null)
    {
        _walk = walk;
        _idle = idle;
        _walkFrames = Math.Max(1, walk.Width / FrameSize);
        _idleFrames = idle != null ? Math.Max(1, idle.Width / FrameSize) : _walkFrames;
    }

    public void Update(float dt, Vector2 facing, bool moving, float animSpeed = 1f)
    {
        if (!moving)
        {
            _timer = 0;
            _frame = 0;
            return;
        }

        var frameDuration = 0.12f / MathF.Max(0.1f, animSpeed);
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
        var frames = _walkFrames;
        var frame = _frame;
        var effects = SpriteEffects.None;

        if (facing.X < -0.15f)
            effects = SpriteEffects.FlipHorizontally;
        else if (facing.Y < -0.15f)
        {
            // Approximate north-facing with idle sheet when available.
            if (_idle != null)
            {
                tex = _idle;
                frames = _idleFrames;
                frame = 0;
            }
        }

        var src = new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize);
        sb.Draw(tex, screenPos, src, tint, 0f, FootAnchor, scale, effects, 0f);
    }
}

public static class TinyRpgCharacterSprites
{
    private static readonly Dictionary<string, TinyRpgStripAnimation> Animations = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static bool IsLoaded => _loaded;

    public static void Load(ContentManager content)
    {
        Animations.Clear();
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

    private static void Register(ContentManager content, string id)
    {
        var walkPath = $"Characters/Rpg/{Capitalize(id)}_Walk";
        var idlePath = $"Characters/Rpg/{Capitalize(id)}_Idle";
        try
        {
            var walk = content.Load<Texture2D>(walkPath);
            Texture2D? idle = null;
            try { idle = content.Load<Texture2D>(idlePath); } catch { /* optional */ }
            Animations[id] = new TinyRpgStripAnimation(walk, idle);
        }
        catch
        {
            // Asset may not be in content pipeline yet.
        }
    }

    private static string Capitalize(string id) =>
        char.ToUpperInvariant(id[0]) + id[1..];
}
