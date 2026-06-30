using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// One-shot 4-direction sword attack (48x48 frames, 8 frames per direction).
/// </summary>
public sealed class FourDirectionAttackAnimation
{
    private readonly Texture2D _texture;
    private float _timer;
    private int _frame;
    private FacingDirection _facing = FacingDirection.Down;

    public bool IsPlaying { get; private set; }
    public int Frame => _frame;

    public FourDirectionAttackAnimation(Texture2D texture) => _texture = texture;

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
        while (_timer >= SwordsmanSpriteSheet.AttackFrameDuration)
        {
            _timer -= SwordsmanSpriteSheet.AttackFrameDuration;
            _frame++;
            if (_frame >= SwordsmanSpriteSheet.FrameCount)
            {
                IsPlaying = false;
                _frame = 0;
                return;
            }
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale = 1f)
    {
        if (!IsPlaying) return;

        var srcX = SwordsmanSpriteSheet.FrameStartX + _frame * SwordsmanSpriteSheet.FrameStride;
        var srcW = SwordsmanSpriteSheet.AttackFrameWidth;
        var origin = SwordsmanSpriteSheet.BodyAnchor;

        if (_facing == FacingDirection.Left)
        {
            srcX -= SwordsmanSpriteSheet.AttackLeftSourcePad;
            srcW += SwordsmanSpriteSheet.AttackLeftSourcePad;
            origin.X += SwordsmanSpriteSheet.AttackLeftSourcePad;
        }

        var src = new Rectangle(
            srcX,
            SwordsmanSpriteSheet.DirectionRowTops[(int)_facing],
            srcW,
            SwordsmanSpriteSheet.AttackFrameHeight);

        sb.Draw(_texture, screenPos, src, tint, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}
