using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Place and remove furniture inside your house (H to toggle). Tab / Shift+Tab or the mouse
/// wheel pick a piece, left click places it at the cursor, right click removes the piece under it.
/// </summary>
public sealed class HousingDecorateOverlay
{
    /// <summary>Placement snaps to half a tile so rows of furniture line up.</summary>
    public const float SnapStep = 8f;

    private static readonly string[] FurnitureTypes = FurnitureSprites.PlaceableTypes;

    private int _selectedIndex;

    public bool IsActive { get; private set; }

    public string SelectedType => FurnitureTypes[_selectedIndex];

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;

    public void Toggle()
    {
        if (IsActive) Deactivate();
        else Activate();
    }

    public static Vector2 Snap(Vector2 world) =>
        new(MathF.Round(world.X / SnapStep) * SnapStep, MathF.Round(world.Y / SnapStep) * SnapStep);

    public bool Update(KeyboardState kb, KeyboardState prevKb, MouseState mouse, MouseState prevMouse, bool inputBlocked)
    {
        if (!IsActive) return false;

        if (InputKeys.EscapePressed(kb, prevKb))
        {
            Deactivate();
            return true;
        }

        if (inputBlocked) return true;

        if (kb.IsKeyDown(Keys.Tab) && !prevKb.IsKeyDown(Keys.Tab))
        {
            var back = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
            Step(back ? -1 : 1);
        }

        var wheel = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (wheel != 0)
            Step(wheel > 0 ? -1 : 1);

        return true;
    }

    private void Step(int delta) =>
        _selectedIndex = (_selectedIndex + delta + FurnitureTypes.Length) % FurnitureTypes.Length;

    public bool TryConsumePlaceClick(MouseState mouse, MouseState prevMouse, bool inputBlocked)
    {
        if (!IsActive || inputBlocked) return false;
        return mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
    }

    public bool TryConsumeRemoveClick(MouseState mouse, MouseState prevMouse, bool inputBlocked)
    {
        if (!IsActive || inputBlocked) return false;
        return mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!IsActive) return;

        var title = SpriteFontSafe.Filter($"Decorate: {FurnitureSprites.DisplayName(SelectedType)}");
        const string controls = "Tab/Wheel: piece  |  Left click: place  |  Right click: remove  |  H/Esc: done";

        // Strip of the neighbouring pieces with the selected one in the middle.
        const int visible = 7;
        const int cell = 44;
        var stripW = visible * cell;
        var controlsSize = SpriteFontSafe.MeasureString(font, controls) * 0.78f;
        var panelW = Math.Min((int)MathF.Max(stripW + 40, controlsSize.X + 40), Math.Max(260, GameViewport.Width - 24));
        const int panelH = 150;
        var panel = new Rectangle(
            GameViewport.Width / 2 - panelW / 2,
            Math.Max(8, GameViewport.Height - panelH - 96),
            panelW,
            panelH);
        FarmRpgUi.DrawWindowPanel(sb, panel, 0.96f);

        var titlePanel = new Rectangle(panel.X + 10, panel.Y + 8, panel.Width - 20, 30);
        FarmRpgUi.DrawTitle(sb, titlePanel);
        DrawCenteredText(sb, font, title, titlePanel, FarmRpgUi.Ink, 0.95f);

        var stripX = panel.Center.X - stripW / 2;
        var stripY = titlePanel.Bottom + 8;
        for (var i = 0; i < visible; i++)
        {
            var offset = i - visible / 2;
            var type = FurnitureTypes[(_selectedIndex + offset + FurnitureTypes.Length * 4) % FurnitureTypes.Length];
            var slot = new Rectangle(stripX + i * cell + 2, stripY, cell - 4, cell - 4);
            FarmRpgUi.DrawInsetPanel(sb, slot);
            if (offset == 0)
                DrawPrimitives.DrawRectOutline(sb, slot, new Color(255, 230, 120), 2f);

            // Fit the sprite into the slot.
            var size = FurnitureSprites.Size(type);
            var scale = MathF.Min(1.6f, (cell - 12) / (float)Math.Max(size.X, size.Y));
            var anchor = FurnitureSprites.IsFloorPiece(type)
                ? new Vector2(slot.Center.X, slot.Center.Y)
                : new Vector2(slot.Center.X, slot.Center.Y + size.Y * scale * 0.5f);
            var tint = offset == 0 ? Color.White : Color.White * 0.55f;
            FurnitureSprites.TryDraw(sb, type, anchor, scale, Environment.TickCount64 / 1000f, tint);
        }

        var controlsPanel = new Rectangle(panel.X + 14, stripY + cell + 4, panel.Width - 28, 26);
        FarmRpgUi.DrawInsetPanel(sb, controlsPanel);
        DrawCenteredText(sb, font, controls, controlsPanel, FarmRpgUi.InkMuted, 0.78f);
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
