using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// 4-direction idle cycle (32x32 frames, 12 frames per direction).
/// </summary>
public sealed class FourDirectionIdleAnimation
{
    private const int FrameWidth = SwordsmanSpriteSheet.BodyWidth;
    private const int FrameHeight = SwordsmanSpriteSheet.BodyHeight;
    private const int FrameCount = 12;

    private readonly Texture2D _texture;
    private float _timer;
    private int _frame;
    private FacingDirection _facing = FacingDirection.Down;

    public FourDirectionIdleAnimation(Texture2D texture) => _texture = texture;

    public void Update(float dt, Vector2 faceDir)
    {
        if (faceDir.LengthSquared() > 0.01f)
            _facing = FourDirectionRunAnimation.ResolveDirection(faceDir);

        _timer += dt;
        while (_timer >= SwordsmanSpriteSheet.FrameDuration)
        {
            _timer -= SwordsmanSpriteSheet.FrameDuration;
            _frame = (_frame + 1) % FrameCount;
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale = 1f)
    {
        var src = new Rectangle(
            SwordsmanSpriteSheet.FrameStartX + _frame * SwordsmanSpriteSheet.FrameStride,
            SwordsmanSpriteSheet.DirectionRowTops[(int)_facing],
            FrameWidth,
            FrameHeight);

        sb.Draw(_texture, screenPos, src, tint, 0f, SwordsmanSpriteSheet.BodyAnchor, scale, SpriteEffects.None, 0f);
    }
}
