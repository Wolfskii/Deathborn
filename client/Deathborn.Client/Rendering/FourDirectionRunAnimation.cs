using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// 4-direction run cycle from Craftpix-style sheets (32x32 frames, 8 frames per direction).
/// </summary>
public sealed class FourDirectionRunAnimation
{
    private const int FrameWidth = SwordsmanSpriteSheet.BodyWidth;
    private const int FrameHeight = SwordsmanSpriteSheet.BodyHeight;

    private readonly Texture2D _texture;
    private float _timer;
    private int _frame;
    private FacingDirection _facing = FacingDirection.Down;

    public FourDirectionRunAnimation(Texture2D texture) => _texture = texture;

    public void Update(float dt, Vector2 moveDir, bool isMoving)
    {
        if (moveDir.LengthSquared() > 0.01f)
            _facing = ResolveDirection(moveDir);

        if (!isMoving)
        {
            _timer = 0;
            _frame = 0;
            return;
        }

        _timer += dt;
        while (_timer >= SwordsmanSpriteSheet.FrameDuration)
        {
            _timer -= SwordsmanSpriteSheet.FrameDuration;
            _frame = (_frame + 1) % SwordsmanSpriteSheet.FrameCount;
        }
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, Color tint, float scale = 1f)
    {
        var src = new Rectangle(
            SwordsmanSpriteSheet.FrameStartX + _frame * SwordsmanSpriteSheet.FrameStride,
            SwordsmanSpriteSheet.DirectionRowTops[(int)_facing],
            FrameWidth,
            FrameHeight);

        var origin = SwordsmanSpriteSheet.BodyAnchor;
        sb.Draw(_texture, screenPos, src, tint, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    public static FacingDirection ResolveDirection(Vector2 dir)
    {
        if (Math.Abs(dir.X) > Math.Abs(dir.Y))
            return dir.X >= 0 ? FacingDirection.Right : FacingDirection.Left;
        return dir.Y >= 0 ? FacingDirection.Down : FacingDirection.Up;
    }
}
