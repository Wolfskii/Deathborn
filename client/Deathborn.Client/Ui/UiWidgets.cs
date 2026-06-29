using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class TextField
{
    public static TextField? Active { get; private set; }

    public Rectangle Bounds;
    public string Text = "";
    public string Placeholder = "";
    public bool IsPassword;

    private bool _focused;
    private double _cursorBlink;

    public bool Focused
    {
        get => _focused;
        set
        {
            if (_focused == value) return;
            _focused = value;
            if (value)
                Active = this;
            else if (Active == this)
                Active = null;
        }
    }

    public void AppendCharacter(char c)
    {
        if (char.IsControl(c)) return;
        Text += c;
    }

    public void Update(GameTime gameTime, KeyboardState kb, KeyboardState prevKb)
    {
        if (!Focused) return;

        _cursorBlink += gameTime.ElapsedGameTime.TotalSeconds;

        if (WasPressed(kb, prevKb, Keys.Back) && Text.Length > 0)
            Text = Text[..^1];
    }

    private static bool WasPressed(KeyboardState kb, KeyboardState prevKb, Keys key) =>
        kb.IsKeyDown(key) && !prevKb.IsKeyDown(key);

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        DrawPrimitives.FillRect(sb, Bounds, Focused ? new Color(40, 44, 52) : new Color(28, 30, 36));
        DrawPrimitives.FillRect(sb, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 2), Focused ? new Color(120, 170, 255) : new Color(60, 64, 72));

        var display = Text.Length > 0 ? (IsPassword ? new string('*', Text.Length) : Text) : Placeholder;
        var col = Text.Length > 0 ? Color.White : new Color(120, 120, 130);
        sb.DrawString(font, display, new Vector2(Bounds.X + 8, Bounds.Y + 8), col);

        if (Focused && ((int)(_cursorBlink * 2) % 2 == 0))
        {
            var cursorX = Bounds.X + 8 + font.MeasureString(display).X + 2;
            DrawPrimitives.FillRect(sb, new Rectangle((int)cursorX, Bounds.Y + 6, 2, Bounds.Height - 12), Color.White);
        }
    }

}

public sealed class Button
{
    public Rectangle Bounds;
    public string Label = "";
    public bool Enabled = true;
    public bool Visible = true;

    public bool Contains(Point p) => Visible && Enabled && Bounds.Contains(p);

    public void Draw(SpriteBatch sb, SpriteFont font, bool hover)
    {
        if (!Visible) return;
        var bg = !Enabled ? new Color(50, 50, 55) : hover ? new Color(70, 90, 120) : new Color(45, 55, 70);
        DrawPrimitives.FillRect(sb, Bounds, bg);
        var size = font.MeasureString(Label);
        var pos = new Vector2(
            Bounds.X + (Bounds.Width - size.X) / 2,
            Bounds.Y + (Bounds.Height - size.Y) / 2);
        sb.DrawString(font, Label, pos, Enabled ? Color.White : new Color(140, 140, 150));
    }
}

public sealed class Checkbox
{
    public Rectangle BoxBounds;
    public string Label = "";
    public bool Checked;

    public bool ContainsBox(Point p) => BoxBounds.Contains(p);

    public void Draw(SpriteBatch sb, SpriteFont font, bool hover)
    {
        var bg = hover ? new Color(50, 55, 65) : new Color(35, 38, 45);
        DrawPrimitives.FillRect(sb, BoxBounds, bg);
        var border = Checked ? new Color(120, 170, 255) : new Color(80, 85, 95);
        DrawPrimitives.FillRect(sb, new Rectangle(BoxBounds.X, BoxBounds.Y, BoxBounds.Width, 2), border);
        DrawPrimitives.FillRect(sb, new Rectangle(BoxBounds.X, BoxBounds.Bottom - 2, BoxBounds.Width, 2), border);
        DrawPrimitives.FillRect(sb, new Rectangle(BoxBounds.X, BoxBounds.Y, 2, BoxBounds.Height), border);
        DrawPrimitives.FillRect(sb, new Rectangle(BoxBounds.Right - 2, BoxBounds.Y, 2, BoxBounds.Height), border);

        if (Checked)
        {
            var cx = BoxBounds.Center.X;
            var cy = BoxBounds.Center.Y;
            DrawPrimitives.DrawLine(sb, new Vector2(cx - 5, cy), new Vector2(cx - 1, cy + 4), Color.White, 2);
            DrawPrimitives.DrawLine(sb, new Vector2(cx - 1, cy + 4), new Vector2(cx + 6, cy - 5), Color.White, 2);
        }

        if (!string.IsNullOrEmpty(Label))
            sb.DrawString(font, Label, new Vector2(BoxBounds.Right + 8, BoxBounds.Y + 2), Color.White);
    }
}
