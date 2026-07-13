using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Animated login banner from <c>Banner V2 - animated.png</c>.
/// Sheet is 1536×1024: 4×4 grid of 384×256 frames (16 animation poses).
/// </summary>
public sealed class AnimatedBannerSprite
{
    private const int Columns = 4;
    private const int Rows = 4;
    private const float FrameDuration = 0.15f;

    private Texture2D? _sheet;
    private int _frameWidth;
    private int _frameHeight;
    private float _time;

    public float Aspect => _frameHeight > 0 ? _frameWidth / (float)_frameHeight : 1f;

    public void SetTexture(Texture2D sheet)
    {
        _sheet = sheet;
        _frameWidth = sheet.Width / Columns;
        _frameHeight = sheet.Height / Rows;
    }

    public void Update(GameTime gameTime) =>
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

    public void Draw(SpriteBatch sb, Rectangle dest)
    {
        if (_sheet is null || _frameWidth <= 0 || _frameHeight <= 0) return;
        if (dest.Width <= 0 || dest.Height <= 0) return;

        var frameCount = Columns * Rows;
        var frame = ((int)(_time / FrameDuration) % frameCount + frameCount) % frameCount;
        var col = frame % Columns;
        var row = frame / Columns;
        var src = new Rectangle(col * _frameWidth, row * _frameHeight, _frameWidth, _frameHeight);

        sb.Draw(_sheet, destinationRectangle: dest, sourceRectangle: src, color: Color.White);
    }
}
