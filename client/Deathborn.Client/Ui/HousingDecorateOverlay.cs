using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Place furniture inside your house interior (H to toggle).</summary>
public sealed class HousingDecorateOverlay
{
    private static readonly string[] FurnitureTypes = ["bed", "table", "chair", "chest", "fireplace", "rug"];

    private int _selectedIndex;
    private MouseState _prevMouse;

    public bool IsActive { get; private set; }

    public string SelectedType => FurnitureTypes[_selectedIndex];

    public void Activate()
    {
        IsActive = true;
        _selectedIndex = 0;
    }

    public void Deactivate() => IsActive = false;

    public void Toggle()
    {
        if (IsActive) Deactivate();
        else Activate();
    }

    public bool Update(KeyboardState kb, KeyboardState prevKb, MouseState mouse, bool inputBlocked)
    {
        if (!IsActive) return false;

        if (InputKeys.EscapePressed(kb, prevKb))
        {
            Deactivate();
            return true;
        }

        if (inputBlocked) return true;

        if (kb.IsKeyDown(Keys.Tab) && !prevKb.IsKeyDown(Keys.Tab))
            _selectedIndex = (_selectedIndex + 1) % FurnitureTypes.Length;

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            return true;

        _prevMouse = mouse;
        return true;
    }

    public bool TryConsumePlaceClick(MouseState mouse, MouseState prevMouse, bool inputBlocked)
    {
        if (!IsActive || inputBlocked) return false;
        if (mouse.LeftButton != ButtonState.Pressed || prevMouse.LeftButton != ButtonState.Released)
            return false;
        return true;
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!IsActive) return;

        var label = $"Decorate: {SelectedType}  |  Tab: next  |  Click: place  |  H/Esc: exit";
        var size = font.MeasureString(label);
        var pad = 10;
        var panel = new Rectangle(
            GameViewport.Width / 2 - (int)size.X / 2 - pad,
            GameViewport.Height - 56,
            (int)size.X + pad * 2,
            (int)size.Y + pad * 2);
        DrawPrimitives.FillRect(sb, panel, new Color(18, 16, 14, 220));
        DrawBorder(sb, panel, new Color(200, 165, 90), 1);
        sb.DrawString(font, label, new Vector2(panel.X + pad, panel.Y + pad), Color.White);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
