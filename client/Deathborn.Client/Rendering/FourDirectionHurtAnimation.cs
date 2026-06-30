using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// One-shot 4-direction hurt reaction (5 frames, 64x64 cells). Rows: down, left, right, up.
/// </summary>
public sealed class FourDirectionHurtAnimation
{
    public const int FrameCount = 5;
    private const int FrameSize = 64;
    private const float FrameDuration = 0.07f;

    private static readonly int[] DirectionRows = [0, 3, 2, 1]; // down, up, right, left

    private readonly Texture2D _texture;
    private float _timer;
    private int _frame;
    private FacingDirection _facing = FacingDirection.Down;

    public bool IsPlaying { get; private set; }

    public FourDirectionHurtAnimation(Texture2D texture) => _texture = texture;

    public void Start(Vector2 facingDir)
    {
        if (facingDir.LengthSquared() > 0.01f)
            _facing = FourDirectionRunAnimation.ResolveDirection(facingDir);

        _frame = 0;
        _timer = 0;
        IsPlaying = true;
    }

    public void Update(float dt)
    {
        if (!IsPlaying) return;

        _timer += dt;
        while (_timer >= FrameDuration)
        {
            _timer -= FrameDuration;
            _frame++;
            if (_frame >= FrameCount)
            {
                IsPlaying = false;
                _frame = FrameCount - 1;
                return;
            }
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale = 1f)
    {
        if (!IsPlaying) return;

        var row = DirectionRows[(int)_facing];
        var src = new Rectangle(_frame * FrameSize, row * FrameSize, FrameSize, FrameSize);
        var origin = new Vector2(FrameSize / 2f, FrameSize - 6f);
        sb.Draw(_texture, screenPos, src, tint, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}
