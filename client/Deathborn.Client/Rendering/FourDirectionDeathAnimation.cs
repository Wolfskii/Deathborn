using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// One-shot 4-direction death (7 frames, 64x64 cells). Rows: down, left, right, up.
/// </summary>
public sealed class FourDirectionDeathAnimation
{
    public const int FrameCount = 7;
    private const int FrameSize = 64;
    private const float FrameDuration = 0.11f;

    private static readonly int[] DirectionRows = [0, 3, 2, 1]; // down, up, right, left

    private readonly Texture2D _texture;
    private float _timer;
    private int _frame;
    private FacingDirection _facing = FacingDirection.Down;

    public bool IsPlaying { get; private set; }
    public bool IsComplete { get; private set; }

    public FourDirectionDeathAnimation(Texture2D texture) => _texture = texture;

    public void Start(Vector2 facingDir)
    {
        if (facingDir.LengthSquared() > 0.01f)
            _facing = FourDirectionRunAnimation.ResolveDirection(facingDir);

        _frame = 0;
        _timer = 0;
        IsPlaying = true;
        IsComplete = false;
    }

    public void HoldCorpse(Vector2 facingDir)
    {
        if (facingDir.LengthSquared() > 0.01f)
            _facing = FourDirectionRunAnimation.ResolveDirection(facingDir);
        _frame = FrameCount - 1;
        _timer = 0;
        IsPlaying = false;
        IsComplete = true;
    }

    public void Update(float dt)
    {
        if (!IsPlaying || IsComplete) return;

        _timer += dt;
        while (_timer >= FrameDuration)
        {
            _timer -= FrameDuration;
            _frame++;
            if (_frame >= FrameCount - 1)
            {
                _frame = FrameCount - 1;
                IsPlaying = false;
                IsComplete = true;
                return;
            }
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale = 1f)
    {
        if (!IsPlaying && !IsComplete) return;

        var row = DirectionRows[(int)_facing];
        var src = new Rectangle(_frame * FrameSize, row * FrameSize, FrameSize, FrameSize);
        var origin = new Vector2(FrameSize / 2f, FrameSize - 6f);
        sb.Draw(_texture, screenPos, src, tint, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}
