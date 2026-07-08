using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Esc menu with music volume, mute, and game info.</summary>
public sealed class EscMenuOverlay
{
    private const int PanelW = 400;
    private const int BasePanelH = 430;

    private Rectangle _panel;
    private Rectangle _sliderTrack;
    private Rectangle _muteBox;
    private Rectangle _characterButton;
    private Rectangle _spellBookButton;
    private Rectangle _inventoryButton;
    private Rectangle _skillsButton;
    private Rectangle _buildHouseButton;
    private Rectangle _titleRibbon;
    private bool _draggingVolume;
    private MouseState _prevMouse;
    private IReadOnlyList<string>? _infoLines;
    private bool _buildHouseEnabled = true;

    public bool IsOpen { get; private set; }
    public Action? OnOpenCharacter;
    public Action? OnOpenSpellBook;
    public Action? OnOpenInventory;
    public Action? OnOpenSkills;
    public Action? OnBuildHouse;

    public void SetBuildHouseEnabled(bool enabled) => _buildHouseEnabled = enabled;

    public void Open()
    {
        IsOpen = true;
        _draggingVolume = false;
    }

    public void Close()
    {
        IsOpen = false;
        _draggingVolume = false;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsOpen) return;

        var mouse = Mouse.GetState();

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            if (_muteBox.Contains(mouse.Position))
                MusicPlayer.SetMuted(!MusicPlayer.IsMuted);

            if (_characterButton.Contains(mouse.Position)) { OnOpenCharacter?.Invoke(); Close(); }
            if (_spellBookButton.Contains(mouse.Position)) { OnOpenSpellBook?.Invoke(); Close(); }
            if (_inventoryButton.Contains(mouse.Position)) { OnOpenInventory?.Invoke(); Close(); }
            if (_skillsButton.Contains(mouse.Position)) { OnOpenSkills?.Invoke(); Close(); }
            if (_buildHouseEnabled && _buildHouseButton.Contains(mouse.Position)) { OnBuildHouse?.Invoke(); Close(); }

            if (_sliderTrack.Contains(mouse.Position))
                SetVolumeFromMouse(mouse.X);
        }

        if (mouse.LeftButton == ButtonState.Pressed && _sliderTrack.Contains(mouse.Position))
            _draggingVolume = true;
        else if (mouse.LeftButton == ButtonState.Released)
            _draggingVolume = false;

        if (_draggingVolume)
            SetVolumeFromMouse(mouse.X);

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, IReadOnlyList<string>? infoLines = null)
    {
        if (!IsOpen) return;

        _infoLines = infoLines;
        Layout(font);

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height),
            new Color(0, 0, 0, 0.55f));

        if (TinySwordsUi.IsLoaded)
            DrawThemed(sb, font);
        else
            DrawLegacy(sb, font);
    }

    private void DrawThemed(SpriteBatch sb, SpriteFont font)
    {
        TinySwordsUi.DrawPanel(sb, _panel, TinySwordsUi.PanelKind.Wood);
        TinySwordsUi.DrawRibbon(sb, _titleRibbon, TinySwordsUi.RibbonKind.Gold, pointed: true);

        var title = "Menu";
        var titleSize = font.MeasureString(title);
        var titleArea = TinySwordsUi.MeasureRibbonTextArea(_titleRibbon);
        sb.DrawString(font, title,
            new Vector2(titleArea.X + (titleArea.Width - titleSize.X) / 2f, titleArea.Y + 3),
            new Color(255, 245, 210));

        var musicLabel = "Music volume";
        sb.DrawString(font, musicLabel, new Vector2(_panel.X + 28, _panel.Y + 78), new Color(50, 38, 28));

        TinySwordsUi.DrawBar(sb, _sliderTrack, MusicPlayer.DisplayVolume, big: false, new Color(120, 190, 120));
        var pct = $"{(int)(MusicPlayer.DisplayVolume * 100)}%";
        sb.DrawString(font, pct, new Vector2(_sliderTrack.Right + 10, _sliderTrack.Y + 1), new Color(60, 48, 36));

        var mute = new Checkbox { Label = "Mute music", Checked = MusicPlayer.IsMuted, BoxBounds = _muteBox };
        mute.Draw(sb, font, _muteBox.Contains(Mouse.GetState().Position));

        var mousePos = Mouse.GetState().Position;
        DrawMenuButton(sb, font, _characterButton, "Character (C)", _characterButton.Contains(mousePos));
        DrawMenuButton(sb, font, _spellBookButton, "Spell Book (K)", _spellBookButton.Contains(mousePos));
        DrawMenuButton(sb, font, _inventoryButton, "Inventory (I)", _inventoryButton.Contains(mousePos));
        DrawMenuButton(sb, font, _skillsButton, "Skills (L)", _skillsButton.Contains(mousePos));
        DrawMenuButton(sb, font, _buildHouseButton,
            _buildHouseEnabled ? "Build House" : "Build House (already built)",
            _buildHouseEnabled && _buildHouseButton.Contains(mousePos), !_buildHouseEnabled);

        if (_infoLines is { Count: > 0 })
        {
            var infoY = _buildHouseButton.Bottom + 18;
            sb.DrawString(font, "Info", new Vector2(_panel.X + 28, infoY), new Color(90, 60, 35));
            infoY += font.LineSpacing + 4;
            foreach (var line in _infoLines)
            {
                sb.DrawString(font, line, new Vector2(_panel.X + 28, infoY), new Color(70, 55, 40));
                infoY += font.LineSpacing;
            }
        }

        var hint = "Esc to close  |  F12 toggles HUD";
        var cx = _panel.X + _panel.Width / 2;
        sb.DrawString(font, hint, new Vector2(cx - font.MeasureString(hint).X / 2f, _panel.Bottom - 30),
            new Color(100, 80, 60));
    }

    private void DrawLegacy(SpriteBatch sb, SpriteFont font)
    {
        var cx = _panel.X + _panel.Width / 2;
        DrawPrimitives.FillRect(sb, _panel, new Color(28, 24, 18));
        sb.DrawString(font, "Menu", new Vector2(cx - font.MeasureString("Menu").X / 2f, _panel.Y + 14), Color.Gold);
        sb.DrawString(font, "Esc to close", new Vector2(cx - 40, _panel.Bottom - 28), Color.Gray);
    }

    private void Layout(SpriteFont font)
    {
        var cx = GameViewport.Width / 2;
        var cy = GameViewport.Height / 2;
        var infoCount = _infoLines?.Count ?? 0;
        var panelH = BasePanelH + (infoCount > 0 ? 36 + infoCount * (int)font.LineSpacing : 0);

        _panel = new Rectangle(cx - PanelW / 2, cy - panelH / 2, PanelW, panelH);
        _titleRibbon = new Rectangle(_panel.X + 20, _panel.Y + 16, _panel.Width - 40, 38);
        _sliderTrack = new Rectangle(_panel.X + 28, _panel.Y + 104, PanelW - 120, 22);
        _muteBox = new Rectangle(_panel.X + 28, _panel.Y + 140, 20, 20);
        _characterButton = new Rectangle(_panel.X + 28, _panel.Y + 176, PanelW - 56, 36);
        _spellBookButton = new Rectangle(_panel.X + 28, _panel.Y + 220, PanelW - 56, 36);
        _inventoryButton = new Rectangle(_panel.X + 28, _panel.Y + 264, PanelW - 56, 36);
        _skillsButton = new Rectangle(_panel.X + 28, _panel.Y + 308, PanelW - 56, 36);
        _buildHouseButton = new Rectangle(_panel.X + 28, _panel.Y + 352, PanelW - 56, 36);
    }

    private void SetVolumeFromMouse(int mouseX)
    {
        var t = (mouseX - _sliderTrack.X) / (float)_sliderTrack.Width;
        MusicPlayer.SetVolume(Math.Clamp(t, 0f, 1f));
    }

    private static void DrawMenuButton(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover, bool disabled = false)
    {
        if (TinySwordsUi.IsLoaded)
        {
            TinySwordsUi.DrawButton(sb, rect, TinySwordsUi.ButtonKind.Blue, pressed: hover && !disabled, disabled ? 0.55f : 1f);
            var size = font.MeasureString(label);
            sb.DrawString(font, label,
                new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f),
                disabled ? new Color(120, 115, 110) : new Color(255, 250, 235));
            return;
        }

        DrawPrimitives.FillRect(sb, rect, disabled ? new Color(32, 28, 24) : hover ? new Color(68, 56, 38) : new Color(48, 40, 30));
        var sz = font.MeasureString(label);
        sb.DrawString(font, label, new Vector2(rect.X + (rect.Width - sz.X) / 2f, rect.Y + (rect.Height - sz.Y) / 2f), Color.White);
    }
}
