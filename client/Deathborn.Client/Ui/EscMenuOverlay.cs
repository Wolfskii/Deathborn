using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Esc menu with music volume, mute, and game info.</summary>
public sealed class EscMenuOverlay
{
    private const int MinPanelW = 320;
    private const int MinPanelH = 440;
    private const int MaxPanelW = 520;
    private const int MaxPanelH = 600;
    private const float PanelAspect = 0.82f; // width / height
    private const int ViewportMargin = 20;
    private const int ContentPadX = 28;
    private const int PanelInnerPad = 20;
    private const int RibbonTop = 16;
    private const int RibbonH = 36;
    private const int ContentTopGap = 14;
    private const int FooterPad = 14;
    private const int MaxContentW = 450;
    private const int MaxButtonW = 420;
    private const int SliderPctReserve = 56;
    private const int ScrollbarW = 12;
    private const int ScrollbarGap = 6;
    private const int ScrollStep = 28;
    private const string FooterHint = "Esc to close  |  F12 toggles HUD";

    private Rectangle _panel;
    private Rectangle _titleRibbon;
    private Rectangle _contentArea;
    private Rectangle _footerArea;
    private Rectangle _sliderTrack;
    private Rectangle _muteBox;
    private Rectangle _characterButton;
    private Rectangle _spellBookButton;
    private Rectangle _inventoryButton;
    private Rectangle _skillsButton;
    private Rectangle _buildHouseButton;
    private Rectangle _destroyHouseButton;
    private Rectangle _logoutButton;
    private Rectangle _newLifeButton;
    private Rectangle _scrollbarTrack;
    private Rectangle _scrollbarThumb;

    private bool _draggingVolume;
    private bool _draggingScrollbar;
    private int _scrollbarDragOffset;
    private int _scrollY;
    private int _maxScroll;
    private int _contentHeight;
    private bool _showScrollbar;
    private int _contentColumnX;
    private int _contentColumnW;
    private int _infoSectionY;
    private MouseState _prevMouse;
    private IReadOnlyList<string>? _infoLines;
    private bool _buildHouseEnabled = true;
    private bool _destroyHouseEnabled;
    private readonly List<string> _footerLines = [];

    public bool IsOpen { get; private set; }
    public Action? OnOpenCharacter;
    public Action? OnOpenSpellBook;
    public Action? OnOpenInventory;
    public Action? OnOpenSkills;
    public Action? OnBuildHouse;
    public Action? OnDestroyHouse;
    public Action? OnLogout;
    public Action? OnNewLife;

    private bool _deathMenuMode;
    private bool _showNewLifeButton;

    public void SetBuildHouseEnabled(bool enabled) => _buildHouseEnabled = enabled;

    public void SetDestroyHouseEnabled(bool enabled) => _destroyHouseEnabled = enabled;

    /// <summary>Death / spirit mode — trim live-world actions and offer a new character.</summary>
    public void SetDeathMenuMode(bool enabled, bool showNewLife = true)
    {
        _deathMenuMode = enabled;
        _showNewLifeButton = enabled && showNewLife;
    }

    public void Open()
    {
        IsOpen = true;
        _draggingVolume = false;
        _draggingScrollbar = false;
        _scrollY = 0;
    }

    public void Close()
    {
        IsOpen = false;
        _draggingVolume = false;
        _draggingScrollbar = false;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsOpen) return;

        var mouse = Mouse.GetState();

        if (_maxScroll > 0)
        {
            var wheel = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (wheel != 0 && (_panel.Contains(mouse.Position) || _contentArea.Contains(mouse.Position)))
                _scrollY = Math.Clamp(_scrollY - wheel / 120 * ScrollStep, 0, _maxScroll);

            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                if (_scrollbarThumb.Contains(mouse.Position))
                {
                    _draggingScrollbar = true;
                    _scrollbarDragOffset = mouse.Y - _scrollbarThumb.Y;
                }
                else if (_scrollbarTrack.Contains(mouse.Position))
                {
                    JumpScrollbarTo(mouse.Y);
                }
            }

            if (_draggingScrollbar)
            {
                if (mouse.LeftButton == ButtonState.Released)
                    _draggingScrollbar = false;
                else
                    JumpScrollbarTo(mouse.Y - _scrollbarDragOffset + _scrollbarThumb.Height / 2);
            }
        }

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            if (HitVisible(_muteBox) && _muteBox.Contains(mouse.Position))
                MusicPlayer.SetMuted(!MusicPlayer.IsMuted);

            if (HitVisible(_characterButton) && _characterButton.Contains(mouse.Position)) { OnOpenCharacter?.Invoke(); Close(); }
            if (HitVisible(_spellBookButton) && _spellBookButton.Contains(mouse.Position)) { OnOpenSpellBook?.Invoke(); Close(); }
            if (HitVisible(_inventoryButton) && _inventoryButton.Contains(mouse.Position)) { OnOpenInventory?.Invoke(); Close(); }
            if (HitVisible(_skillsButton) && _skillsButton.Contains(mouse.Position)) { OnOpenSkills?.Invoke(); Close(); }
            if (_buildHouseEnabled && !_deathMenuMode && HitVisible(_buildHouseButton) && _buildHouseButton.Contains(mouse.Position))
            {
                OnBuildHouse?.Invoke();
                Close();
            }

            if (_destroyHouseEnabled && !_deathMenuMode && HitVisible(_destroyHouseButton) && _destroyHouseButton.Contains(mouse.Position))
            {
                OnDestroyHouse?.Invoke();
                Close();
            }

            if (_showNewLifeButton && HitVisible(_newLifeButton) && _newLifeButton.Contains(mouse.Position))
            {
                OnNewLife?.Invoke();
                Close();
            }

            if (HitVisible(_logoutButton) && _logoutButton.Contains(mouse.Position))
            {
                OnLogout?.Invoke();
                Close();
            }

            if (HitVisible(_sliderTrack) && _sliderTrack.Contains(mouse.Position))
                SetVolumeFromMouse(mouse.X);
        }

        if (mouse.LeftButton == ButtonState.Pressed && HitVisible(_sliderTrack) && _sliderTrack.Contains(mouse.Position))
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

        if (FarmRpgUi.IsLoaded)
            DrawThemed(sb, font);
        else
            DrawLegacy(sb, font);
    }

    private void DrawThemed(SpriteBatch sb, SpriteFont font)
    {
        FarmRpgUi.DrawWindowPanel(sb, _panel);
        FarmRpgUi.DrawTitle(sb, _titleRibbon);

        var title = "Menu";
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title,
            new Vector2(_titleRibbon.X + (_titleRibbon.Width - titleSize.X) / 2f,
                _titleRibbon.Y + (_titleRibbon.Height - titleSize.Y) / 2f),
            FarmRpgUi.Ink);

        var musicLabel = "Music volume";
        DrawContentString(sb, font, musicLabel, _contentColumnX, 0, FarmRpgUi.Ink);

        if (Visible(_sliderTrack))
            DrawVolumeSlider(sb, _sliderTrack, MusicPlayer.DisplayVolume);

        var pct = $"{(int)(MusicPlayer.DisplayVolume * 100)}%";
        var pctY = _sliderTrack.Y + (_sliderTrack.Height - font.LineSpacing) / 2f;
        if (pctY + font.LineSpacing >= _contentArea.Y && pctY <= _contentArea.Bottom)
            sb.DrawString(font, pct, new Vector2(_sliderTrack.Right + 10, pctY), FarmRpgUi.InkMuted);

        if (Visible(_muteBox))
            DrawMuteCheckbox(sb, font, _muteBox, _muteBox.Contains(Mouse.GetState().Position));

        var mousePos = Mouse.GetState().Position;
        DrawMenuButtonIfVisible(sb, font, _characterButton, "Character (C)", !_deathMenuMode && _characterButton.Contains(mousePos));
        DrawMenuButtonIfVisible(sb, font, _spellBookButton, "Spell Book (K)", !_deathMenuMode && _spellBookButton.Contains(mousePos));
        DrawMenuButtonIfVisible(sb, font, _inventoryButton, "Inventory (I)", !_deathMenuMode && _inventoryButton.Contains(mousePos));
        DrawMenuButtonIfVisible(sb, font, _skillsButton, "Skills (L)", !_deathMenuMode && _skillsButton.Contains(mousePos));
        if (_destroyHouseEnabled)
        {
            DrawMenuButtonIfVisible(sb, font, _destroyHouseButton, "Destroy House",
                !_deathMenuMode && _destroyHouseButton.Contains(mousePos), _deathMenuMode);
        }
        else
        {
            DrawMenuButtonIfVisible(sb, font, _buildHouseButton,
                _buildHouseEnabled ? "Build House" : "Build House (already built)",
                _buildHouseEnabled && !_deathMenuMode && _buildHouseButton.Contains(mousePos), !_buildHouseEnabled || _deathMenuMode);
        }
        DrawMenuButtonIfVisible(sb, font, _newLifeButton, "Begin anew", _newLifeButton.Contains(mousePos));
        DrawMenuButtonIfVisible(sb, font, _logoutButton, "Log out", _logoutButton.Contains(mousePos));

        if (_infoLines is { Count: > 0 })
        {
            DrawContentString(sb, font, "Info", _contentColumnX, _infoSectionY, FarmRpgUi.Ink);
            var infoY = _infoSectionY + font.LineSpacing + 4;
            foreach (var line in _infoLines)
            {
                DrawContentString(sb, font, line, _contentColumnX, infoY, FarmRpgUi.InkMuted);
                infoY += font.LineSpacing;
            }
        }

        DrawFooter(sb, font);

        if (_showScrollbar)
            DrawScrollbar(sb);
    }

    private void DrawLegacy(SpriteBatch sb, SpriteFont font)
    {
        var cx = _panel.X + _panel.Width / 2;
        DrawPrimitives.FillRect(sb, _panel, new Color(28, 24, 18));
        sb.DrawString(font, "Menu", new Vector2(cx - font.MeasureString("Menu").X / 2f, _panel.Y + 14), Color.Gold);
        DrawFooter(sb, font);
    }

    private void DrawFooter(SpriteBatch sb, SpriteFont font)
    {
        var y = _footerArea.Y;
        foreach (var line in _footerLines)
        {
            var size = font.MeasureString(line);
            sb.DrawString(font, line,
                new Vector2(_footerArea.X + (_footerArea.Width - size.X) / 2f, y),
                FarmRpgUi.InkMuted);
            y += font.LineSpacing;
        }
    }

    private void DrawScrollbar(SpriteBatch sb)
    {
        FarmRpgUi.DrawScrollbar(sb, _scrollbarTrack, _scrollbarThumb);
    }

    private void Layout(SpriteFont font)
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        var maxW = Math.Max(1, vw - ViewportMargin * 2);
        var maxH = Math.Max(1, vh - ViewportMargin * 2);

        var panelH = Math.Min(maxH, Math.Min(MaxPanelH, (int)(vh * 0.86f)));
        var panelW = (int)(panelH * PanelAspect);
        if (panelW > maxW)
        {
            panelW = Math.Min(maxW, MaxPanelW);
            panelH = (int)(panelW / PanelAspect);
        }
        panelW = Math.Clamp(panelW, Math.Min(MinPanelW, maxW), Math.Min(MaxPanelW, maxW));
        panelH = Math.Clamp(panelH, Math.Min(MinPanelH, maxH), Math.Min(MaxPanelH, maxH));

        _panel = new Rectangle(vw / 2 - panelW / 2, vh / 2 - panelH / 2, panelW, panelH);
        _titleRibbon = new Rectangle(_panel.X + PanelInnerPad, _panel.Y + RibbonTop, _panel.Width - PanelInnerPad * 2, RibbonH);

        var innerW = _panel.Width - ContentPadX * 2;
        _contentColumnW = Math.Min(innerW, MaxContentW);
        _contentColumnX = _panel.X + ContentPadX + (innerW - _contentColumnW) / 2;

        var footerTextW = _contentColumnW;
        _footerLines.Clear();
        _footerLines.AddRange(WrapText(font, FooterHint, footerTextW));
        var footerH = _footerLines.Count * font.LineSpacing;
        _footerArea = new Rectangle(_contentColumnX, _panel.Bottom - footerH - FooterPad, footerTextW, footerH);

        var contentTop = _titleRibbon.Bottom + ContentTopGap;
        var contentH = Math.Max(1, _footerArea.Y - FooterPad - contentTop);

        const int sliderH = 24;
        const int buttonH = 36;
        const int buttonGap = 8;
        const int sectionGap = 16;

        _contentHeight = MeasureContentHeight(font, sectionGap);
        _showScrollbar = _contentHeight > contentH;

        var scrollReserve = _showScrollbar ? ScrollbarW + ScrollbarGap : 0;
        var contentW = innerW - scrollReserve;
        _contentColumnW = Math.Min(contentW, MaxContentW);
        _contentColumnX = _panel.X + ContentPadX + (contentW - _contentColumnW) / 2;

        var buttonW = Math.Min(_contentColumnW, MaxButtonW);
        var buttonX = _contentColumnX + (_contentColumnW - buttonW) / 2;
        var sliderW = Math.Max(120, _contentColumnW - SliderPctReserve);
        var sliderX = _contentColumnX;

        _contentArea = new Rectangle(_panel.X + ContentPadX, contentTop, contentW, contentH);

        LayoutContentItems(font, sliderX, sliderW, sliderH, buttonX, buttonW, buttonH, buttonGap, sectionGap);
        _maxScroll = Math.Max(0, _contentHeight - contentH);
        _scrollY = Math.Clamp(_scrollY, 0, _maxScroll);

        if (_showScrollbar)
        {
            _scrollbarTrack = new Rectangle(_contentArea.Right + ScrollbarGap, _contentArea.Y, ScrollbarW, _contentArea.Height);
            var thumbH = Math.Max(28, (int)(_contentArea.Height * (contentH / (float)_contentHeight)));
            var travel = Math.Max(1, _scrollbarTrack.Height - thumbH);
            var thumbY = _scrollbarTrack.Y + (_maxScroll == 0 ? 0 : (int)(_scrollY / (float)_maxScroll * travel));
            _scrollbarThumb = new Rectangle(_scrollbarTrack.X + 2, thumbY, ScrollbarW - 4, thumbH);
        }
        else
        {
            _scrollbarTrack = Rectangle.Empty;
            _scrollbarThumb = Rectangle.Empty;
        }
    }

    private int MeasureContentHeight(SpriteFont font, int sectionGap)
    {
        const int sliderH = 24;
        const int buttonH = 36;
        const int buttonGap = 8;
        var y = font.LineSpacing + 6 + sliderH + sectionGap + 20 + sectionGap;
        if (!_deathMenuMode)
            y += (buttonH + buttonGap) * 3;
        if (_showNewLifeButton)
            y += buttonH + buttonGap;
        y += buttonH + sectionGap;
        if (_infoLines is { Count: > 0 })
            y += font.LineSpacing + 4 + _infoLines.Count * font.LineSpacing + sectionGap;
        return y;
    }

    private void LayoutContentItems(
        SpriteFont font, int sliderX, int sliderW, int sliderH, int buttonX, int buttonW, int buttonH, int buttonGap, int sectionGap)
    {
        var y = 0;
        y += font.LineSpacing + 6;
        _sliderTrack = ContentRect(sliderX, y, sliderW, sliderH);
        y += sliderH + sectionGap;
        _muteBox = ContentRect(_contentColumnX, y, 20, 20);
        y += 20 + sectionGap;
        if (!_deathMenuMode)
        {
            var columnGap = Math.Min(10, Math.Max(6, buttonW / 30));
            var halfW = Math.Max(1, (buttonW - columnGap) / 2);
            _characterButton = ContentRect(buttonX, y, halfW, buttonH);
            _spellBookButton = ContentRect(buttonX + halfW + columnGap, y, buttonW - halfW - columnGap, buttonH);
            y += buttonH + buttonGap;
            _inventoryButton = ContentRect(buttonX, y, halfW, buttonH);
            _skillsButton = ContentRect(buttonX + halfW + columnGap, y, buttonW - halfW - columnGap, buttonH);
            y += buttonH + buttonGap;
            if (_destroyHouseEnabled)
            {
                _destroyHouseButton = ContentRect(buttonX, y, buttonW, buttonH);
                _buildHouseButton = Rectangle.Empty;
            }
            else
            {
                _buildHouseButton = ContentRect(buttonX, y, buttonW, buttonH);
                _destroyHouseButton = Rectangle.Empty;
            }
            y += buttonH + buttonGap;
        }
        else
        {
            _characterButton = Rectangle.Empty;
            _spellBookButton = Rectangle.Empty;
            _inventoryButton = Rectangle.Empty;
            _skillsButton = Rectangle.Empty;
            _buildHouseButton = Rectangle.Empty;
            _destroyHouseButton = Rectangle.Empty;
        }

        if (_showNewLifeButton)
        {
            _newLifeButton = ContentRect(buttonX, y, buttonW, buttonH);
            y += buttonH + buttonGap;
        }
        else
        {
            _newLifeButton = Rectangle.Empty;
        }

        _logoutButton = ContentRect(buttonX, y, buttonW, buttonH);
        y += buttonH + sectionGap;
        _infoSectionY = y;
        if (_infoLines is { Count: > 0 })
            y += font.LineSpacing + 4 + _infoLines.Count * font.LineSpacing + sectionGap;
        _contentHeight = y;
    }

    private Rectangle ContentRect(int x, int localY, int w, int h) =>
        new(x, _contentArea.Y + localY - _scrollY, w, h);

    private void DrawContentString(SpriteBatch sb, SpriteFont font, string text, int x, int localY, Color color)
    {
        var y = _contentArea.Y + localY - _scrollY;
        if (y < _contentArea.Y || y + font.LineSpacing > _contentArea.Bottom) return;
        sb.DrawString(font, text, new Vector2(x, y), color);
    }

    private void DrawMenuButtonIfVisible(
        SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover, bool disabled = false)
    {
        if (!Visible(rect)) return;
        DrawMenuButton(sb, font, rect, label, hover, disabled);
    }

    private bool Visible(Rectangle rect) =>
        rect.Y >= _contentArea.Y && rect.Bottom <= _contentArea.Bottom;

    private bool HitVisible(Rectangle rect) =>
        Visible(rect) && rect.Contains(Mouse.GetState().Position);

    private void JumpScrollbarTo(int pointerY)
    {
        if (_maxScroll <= 0) return;
        var thumbH = _scrollbarThumb.Height;
        var travel = Math.Max(1, _scrollbarTrack.Height - thumbH);
        var t = Math.Clamp((pointerY - _scrollbarTrack.Y - thumbH / 2f) / travel, 0f, 1f);
        _scrollY = (int)MathF.Round(t * _maxScroll);
    }

    private void SetVolumeFromMouse(int mouseX)
    {
        var t = (mouseX - _sliderTrack.X) / (float)_sliderTrack.Width;
        MusicPlayer.SetVolume(Math.Clamp(t, 0f, 1f));
    }

    private static List<string> WrapText(SpriteFont font, string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return lines;

        var line = words[0];
        for (var i = 1; i < words.Length; i++)
        {
            var next = line + " " + words[i];
            if (font.MeasureString(next).X > maxWidth)
            {
                lines.Add(line);
                line = words[i];
            }
            else
            {
                line = next;
            }
        }

        lines.Add(line);
        return lines;
    }

    private static void DrawMenuButton(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover, bool disabled = false)
    {
        if (FarmRpgUi.IsLoaded)
        {
            FarmRpgUi.DrawButton(sb, rect, pressed: hover, disabled: disabled, danger: label == "Log out");
            var size = font.MeasureString(label);
            sb.DrawString(font, label,
                new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f),
                disabled ? FarmRpgUi.InkMuted * 0.65f : FarmRpgUi.Ink);
            return;
        }

        DrawPrimitives.FillRect(sb, rect, disabled ? new Color(32, 28, 24) : hover ? new Color(68, 56, 38) : new Color(48, 40, 30));
        var sz = font.MeasureString(label);
        sb.DrawString(font, label, new Vector2(rect.X + (rect.Width - sz.X) / 2f, rect.Y + (rect.Height - sz.Y) / 2f), Color.White);
    }

    private static void DrawVolumeSlider(SpriteBatch sb, Rectangle rect, float value)
    {
        FarmRpgUi.DrawBar(sb, rect, value, new Color(201, 100, 53));
    }

    private static void DrawMuteCheckbox(SpriteBatch sb, SpriteFont font, Rectangle rect, bool hover)
    {
        FarmRpgUi.DrawToggle(sb, rect, MusicPlayer.IsMuted, hover);
        sb.DrawString(font, "Mute music", new Vector2(rect.Right + 8, rect.Y + 1), FarmRpgUi.Ink);
    }
}
