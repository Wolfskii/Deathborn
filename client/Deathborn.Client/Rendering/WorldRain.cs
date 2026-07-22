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
    private const float FrameDuration = 0.16f;
    private const float TileScale = 1.15f;
    /// <summary>Stride larger than tile size leaves gaps between drop clusters.</summary>
    private const float TileStrideMul = 1.85f;
    private const float ScrollPxPerSec = 22f;

    private static readonly Color CloudShadow = new(12, 16, 28, 120);
    private static readonly Color RainTint = new(210, 220, 255, 180);

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

        var period = FrameHeight * TileScale * TileStrideMul;
        _scroll = (_scroll + ScrollPxPerSec * dt) % period;
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

        var strideX = Math.Max(tileW + 1, (int)MathF.Ceiling(tileW * TileStrideMul));
        var strideY = Math.Max(tileH + 1, (int)MathF.Ceiling(tileH * TileStrideMul));

        var src = new Rectangle(_frame * FrameWidth, 0, FrameWidth, FrameHeight);
        var offsetY = (int)_scroll;

        for (var y = -strideY + offsetY; y < viewH; y += strideY)
        {
            for (var x = 0; x < viewW; x += strideX)
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
