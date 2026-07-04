using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// 4-direction idle cycle (32x32 frames; up row has only 4 frames at the start).
/// </summary>
public sealed class FourDirectionIdleAnimation
{
    private const int FrameWidth = SwordsmanSpriteSheet.BodyWidth;
    private const int FrameHeight = SwordsmanSpriteSheet.BodyHeight;

    private readonly Texture2D _texture;
    private float _timer;
    private int _frame;
    private FacingDirection _facing = FacingDirection.Down;

    public FourDirectionIdleAnimation(Texture2D texture) => _texture = texture;

    public void Update(float dt, Vector2 faceDir)
    {
        if (faceDir.LengthSquared() > 0.01f)
        {
            var next = FourDirectionRunAnimation.ResolveDirection(faceDir);
            if (next != _facing)
            {
                _facing = next;
                _frame = 0;
                _timer = 0;
            }
        }

        _timer += dt;
        var frameCount = SwordsmanSpriteSheet.IdleFrameCounts[(int)_facing];
        while (_timer >= SwordsmanSpriteSheet.FrameDuration)
        {
            _timer -= SwordsmanSpriteSheet.FrameDuration;
            _frame = (_frame + 1) % frameCount;
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale = 1f)
    {
        var facing = (int)_facing;
        var src = new Rectangle(
            SwordsmanSpriteSheet.FrameStartX
                + (SwordsmanSpriteSheet.IdleFrameOffsets[facing] + _frame) * SwordsmanSpriteSheet.FrameStride,
            SwordsmanSpriteSheet.DirectionRowTops[facing],
            FrameWidth,
            FrameHeight);

        sb.Draw(_texture, screenPos, src, tint, 0f, SwordsmanSpriteSheet.BodyAnchor, scale, SpriteEffects.None, 0f);
    }

    public void DrawOutline(SpriteBatch sb, Vector2 screenPos, Color outline, float scale, float thickness = 1f)
    {
        var facing = (int)_facing;
        var src = new Rectangle(
            SwordsmanSpriteSheet.FrameStartX
                + (SwordsmanSpriteSheet.IdleFrameOffsets[facing] + _frame) * SwordsmanSpriteSheet.FrameStride,
            SwordsmanSpriteSheet.DirectionRowTops[facing],
            FrameWidth,
            FrameHeight);
        SpriteOutlineDraw.Draw(sb, _texture, screenPos, src, SwordsmanSpriteSheet.BodyAnchor, outline, scale, thickness);
    }
}
