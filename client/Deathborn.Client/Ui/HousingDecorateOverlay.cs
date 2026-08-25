using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Place furniture inside your house interior (H to toggle).</summary>
public sealed class HousingDecorateOverlay
{
    private static readonly string[] FurnitureTypes = ["bed", "table", "chair", "chest", "fireplace", "kitchen", "rug"];

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

        var title = SpriteFontSafe.Filter($"Decorate: {SelectedType}");
        const string controls = "Tab: next  |  Click: place  |  H/Esc: exit";
        var titleSize = SpriteFontSafe.MeasureString(font, title);
        var controlsSize = SpriteFontSafe.MeasureString(font, controls) * 0.82f;
        var contentW = (int)MathF.Max(titleSize.X, controlsSize.X);
        var panelW = Math.Min(Math.Max(260, contentW + 48), Math.Max(260, GameViewport.Width - 24));
        const int panelH = 82;
        var panel = new Rectangle(
            GameViewport.Width / 2 - panelW / 2,
            Math.Max(8, GameViewport.Height - panelH - 12),
            panelW,
            panelH);
        FarmRpgUi.DrawWindowPanel(sb, panel, 0.96f);

        var titlePanel = new Rectangle(panel.X + 10, panel.Y + 8, panel.Width - 20, 30);
        FarmRpgUi.DrawTitle(sb, titlePanel);
        DrawCenteredText(sb, font, title, titlePanel, FarmRpgUi.Ink, 0.95f);

        var controlsPanel = new Rectangle(panel.X + 14, titlePanel.Bottom + 6, panel.Width - 28, 28);
        FarmRpgUi.DrawInsetPanel(sb, controlsPanel);
        DrawCenteredText(sb, font, controls, controlsPanel, FarmRpgUi.InkMuted, 0.82f);
    }

    private static void DrawCenteredText(
        SpriteBatch sb, SpriteFont font, string text, Rectangle area, Color color, float preferredScale)
    {
        var size = SpriteFontSafe.MeasureString(font, text);
        var scale = MathF.Min(preferredScale, Math.Max(1, area.Width - 12) / MathF.Max(1f, size.X));
        var pos = new Vector2(
            area.X + (area.Width - size.X * scale) * 0.5f,
            area.Y + (area.Height - size.Y * scale) * 0.5f);
        SpriteFontSafe.DrawString(sb, font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
