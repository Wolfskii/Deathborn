using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Animated stone brazier from the Farm RPG pack (Ui/FarmRpg/brazier.png, installed by
/// scripts/install_farm_rpg_ui.py). The sheet is six 16 x 32 frames of the flame flickering.
/// </summary>
public sealed class FarmRpgBrazier
{
    private const int FrameWidth = 16;
    private const int FrameHeight = 32;
    private const int FrameCount = 6;
    private const double FrameSeconds = 0.1;
    private const float Scale = 5f;

    private static Texture2D? _sheet;

    private double _time;

    public static void Load(ContentManager content)
    {
        try
        {
            _sheet = content.Load<Texture2D>("Ui/FarmRpg/brazier");
        }
        catch (ContentLoadException)
        {
            _sheet = null;
        }
    }

    public void Update(GameTime gameTime) => _time += gameTime.ElapsedGameTime.TotalSeconds;

    /// <summary>Draws the brazier centred on <paramref name="centerX"/>, standing on <paramref name="bottomY"/>.</summary>
    public void Draw(SpriteBatch sb, int centerX, int bottomY)
    {
        if (_sheet == null) return;

        var frame = (int)(_time / FrameSeconds) % FrameCount;
        var source = new Rectangle(frame * FrameWidth, 0, FrameWidth, FrameHeight);
        var origin = new Vector2(FrameWidth / 2f, FrameHeight);
        sb.Draw(_sheet, new Vector2(centerX, bottomY), source, Color.White, 0f, origin, Scale, SpriteEffects.None, 0f);
    }
}
