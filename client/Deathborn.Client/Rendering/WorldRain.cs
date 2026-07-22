using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Screen-space rain overlay: cloudy dim over the world plus tiled rain animation.</summary>
public static class WorldRain
{
    private const int FrameWidth = 32;
    private const int FrameHeight = 64;
    /// <summary>Sheet is 20 columns but cycles every 5 unique frames.</summary>
    private const int LoopFrames = 5;
    private const float FrameDuration = 0.07f;
    private const float TileScale = 2.5f;
    private const float ScrollPxPerSec = 48f;

    private static readonly Color CloudShadow = new(12, 16, 28, 120);
    private static readonly Color RainTint = new(210, 220, 255, 210);

    private static Texture2D? _sheet;
    private static float _animTimer;
    private static int _frame;
    private static float _scroll;

    public static bool Enabled { get; set; }

    public static void Load(ContentManager content) =>
        _sheet = content.Load<Texture2D>("Weather/rain");

    public static void Toggle() => Enabled = !Enabled;

    public static void Update(float dt)
    {
        if (!Enabled || _sheet == null) return;

        _animTimer += dt;
        while (_animTimer >= FrameDuration)
        {
            _animTimer -= FrameDuration;
            _frame = (_frame + 1) % LoopFrames;
        }

        _scroll = (_scroll + ScrollPxPerSec * dt) % (FrameHeight * TileScale);
    }

    /// <summary>Draw after the exterior world, before HUD, so rain sits over the map.</summary>
    public static void Draw(SpriteBatch sb)
    {
        if (!Enabled || _sheet == null) return;

        var viewW = GameViewport.Width;
        var viewH = GameViewport.Height;
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, viewW, viewH), CloudShadow);

        var tileW = (int)MathF.Ceiling(FrameWidth * TileScale);
        var tileH = (int)MathF.Ceiling(FrameHeight * TileScale);
        if (tileW <= 0 || tileH <= 0) return;

        var src = new Rectangle(_frame * FrameWidth, 0, FrameWidth, FrameHeight);
        var offsetY = (int)_scroll;

        for (var y = -tileH + offsetY; y < viewH; y += tileH)
        {
            for (var x = 0; x < viewW; x += tileW)
            {
                sb.Draw(
                    _sheet,
                    new Rectangle(x, y, tileW, tileH),
                    src,
                    RainTint);
            }
        }
    }
}
