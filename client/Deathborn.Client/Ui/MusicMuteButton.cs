using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class MusicMuteButton
{
    private const int Size = 36;
    private const int Margin = 10;

    private static readonly Color Icon = new(210, 170, 80);
    private static readonly Color IconHover = new(240, 210, 130);
    private static readonly Color Bg = new(0, 0, 0, 140);
    private static readonly Color BgHover = new(28, 24, 18, 200);

    private MouseState _prevMouse;

    private Rectangle Bounds => new(Config.Width - Margin - Size, Margin, Size, Size);

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var hover = Bounds.Contains(mouse.Position);

        if (hover && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            MusicPlayer.SetMuted(!MusicPlayer.IsMuted);

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb)
    {
        var mouse = Mouse.GetState();
        var hover = Bounds.Contains(mouse.Position);
        var bounds = Bounds;

        DrawPrimitives.FillRect(sb, bounds, hover ? BgHover : Bg);
        DrawBorder(sb, bounds, hover ? IconHover : Icon, 1);

        var iconColor = hover ? IconHover : Icon;
        DrawNoteIcon(sb, bounds, iconColor);

        if (MusicPlayer.IsMuted)
        {
            DrawPrimitives.DrawLine(
                sb,
                new Vector2(bounds.X + 6, bounds.Y + 6),
                new Vector2(bounds.Right - 6, bounds.Bottom - 6),
                Color.White,
                2.5f);
        }
    }

    private static void DrawNoteIcon(SpriteBatch sb, Rectangle bounds, Color color)
    {
        var cx = bounds.Center.X;
        var cy = bounds.Center.Y;
        var head = new Vector2(cx - 5, cy + 5);
        var stemTop = new Vector2(head.X + 5, cy - 9);

        DrawPrimitives.FillCircle(sb, head, 5f, color);
        DrawPrimitives.DrawLine(sb, new Vector2(head.X + 4, head.Y - 2), stemTop, color, 2.5f);
        DrawPrimitives.DrawLine(sb, stemTop, new Vector2(stemTop.X + 10, stemTop.Y + 5), color, 2.5f);
        DrawPrimitives.DrawLine(sb, new Vector2(stemTop.X, stemTop.Y + 2), new Vector2(stemTop.X + 10, stemTop.Y + 7), color, 2f);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
