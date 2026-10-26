using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Esc menu: sound settings, shortcuts to windows, homestead actions, account
/// actions and session info. Layout is rebuilt every draw; clicks use the last layout.
/// </summary>
public sealed class EscMenuOverlay
{
    private const int PanelWidth = 460;
    private const int ViewportMargin = 16;
    private const int SidePad = 28;
    private const int HeaderHeight = 56;
    private const int BottomPad = 22;
    private const int RowHeight = 32;
    private const int LabelWidth = 76;
    private const int PercentWidth = 48;
    private const int ScrollStep = 32;
    private const string FooterHint = "Esc to close   -   F12 hides the HUD";

    private enum SliderKind { Music, Sfx }

    private sealed record ButtonItem(Rectangle Rect, string Label, Action Click, bool Enabled, bool Danger);
    private sealed record SliderItem(Rectangle Track, SliderKind Kind);
    private sealed record ToggleItem(Rectangle Box, SliderKind Kind);

    private readonly List<ButtonItem> _buttons = [];
    private readonly List<SliderItem> _sliders = [];
    private readonly List<ToggleItem> _toggles = [];

    private Rectangle _panel;
    private Rectangle _viewport;
    private int _scrollY;
    private int _maxScroll;
    private SliderKind? _dragging;
    private MouseState _prevMouse;
    private bool _buildHouseEnabled = true;
    private bool _destroyHouseEnabled;
    private bool _deathMenuMode;
    private bool _showNewLifeButton;

    public bool IsOpen { get; private set; }
    public Action? OnOpenCharacter;
    public Action? OnOpenSpellBook;
    public Action? OnOpenInventory;
    public Action? OnOpenSkills;
    public Action? OnBuildHouse;
    public Action? OnDestroyHouse;
    public Action? OnLogout;
    public Action? OnNewLife;

    public void SetBuildHouseEnabled(bool enabled) => _buildHouseEnabled = enabled;

    public void SetDestroyHouseEnabled(bool enabled) => _destroyHouseEnabled = enabled;

    /// <summary>Death / spirit mode: hide live-world actions and offer a new character.</summary>
    public void SetDeathMenuMode(bool enabled, bool showNewLife = true)
    {
        _deathMenuMode = enabled;
        _showNewLifeButton = enabled && showNewLife;
    }

    public void Open()
    {
        IsOpen = true;
        _dragging = null;
        _scrollY = 0;
        _prevMouse = Mouse.GetState();
    }

    public void Close()
    {
        IsOpen = false;
        StopDragging();
    }

    public void Update(GameTime gameTime)
    {
        if (!IsOpen) return;
        var mouse = Mouse.GetState();
        var pos = mouse.Position;
        var clicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

        if (_maxScroll > 0 && _panel.Contains(pos))
        {
            var wheel = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (wheel != 0)
                _scrollY = Math.Clamp(_scrollY - Math.Sign(wheel) * ScrollStep, 0, _maxScroll);
        }

        if (clicked)
        {
            foreach (var slider in _sliders)
            {
                if (Hit(slider.Track, pos, grow: 10))
                {
                    _dragging = slider.Kind;
                    break;
                }
            }
            foreach (var toggle in _toggles)
            {
                if (Hit(toggle.Box, pos))
                    ToggleMute(toggle.Kind);
            }
            foreach (var button in _buttons)
            {
                if (button.Enabled && Hit(button.Rect, pos))
                {
                    button.Click();
                    break;
                }
            }
        }

        if (_dragging is { } kind)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var track = _sliders.Find(s => s.Kind == kind)?.Track ?? Rectangle.Empty;
                if (!track.IsEmpty)
                    SetVolume(kind, (pos.X - track.X) / (float)track.Width, save: false);
            }
            else
            {
                StopDragging();
            }
        }

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, IReadOnlyList<string>? infoLines = null)
    {
        if (!IsOpen) return;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), new Color(0, 0, 0, 0.5f));

        var contentHeight = LayoutContent(font, infoLines, measureOnly: true, 0);
        var maxPanelH = GameViewport.Height - ViewportMargin * 2;
        var panelH = Math.Min(maxPanelH, HeaderHeight + contentHeight + BottomPad);
        var panelW = Math.Min(PanelWidth, GameViewport.Width - ViewportMargin * 2);
        _panel = new Rectangle(GameViewport.Width / 2 - panelW / 2, GameViewport.Height / 2 - panelH / 2, panelW, panelH);
        _viewport = new Rectangle(_panel.X + SidePad, _panel.Y + HeaderHeight, _panel.Width - SidePad * 2, panelH - HeaderHeight - BottomPad);
        _maxScroll = Math.Max(0, contentHeight - _viewport.Height);
        _scrollY = Math.Clamp(_scrollY, 0, _maxScroll);

        FarmRpgUi.DrawWindowPanel(sb, _panel);
        FarmRpgUi.DrawTitlePlate(sb, font, _deathMenuMode ? "You have fallen" : "Menu", _panel.Center.X, _panel.Y + 12);

        _buttons.Clear();
        _sliders.Clear();
        _toggles.Clear();
        LayoutContent(font, infoLines, measureOnly: false, _viewport.Y - _scrollY, sb);

        if (_maxScroll > 0)
        {
            var track = new Rectangle(_panel.Right - 18, _viewport.Y, 8, _viewport.Height);
            var thumbH = Math.Max(30, _viewport.Height * _viewport.Height / contentHeight);
            var thumbY = track.Y + (int)((track.Height - thumbH) * (_scrollY / (float)_maxScroll));
            FarmRpgUi.DrawScrollbar(sb, track, new Rectangle(track.X, thumbY, track.Width, thumbH));
        }
    }

    /// <summary>
    /// Walks the menu top to bottom. When measuring it only returns the height; otherwise
    /// it draws each visible item and records its hit area.
    /// </summary>
    private int LayoutContent(SpriteFont font, IReadOnlyList<string>? infoLines, bool measureOnly, int top, SpriteBatch? sb = null)
    {
        var x = _viewport.X;
        var width = measureOnly ? PanelWidth - SidePad * 2 : _viewport.Width;
        var y = top;
        var mouse = Mouse.GetState().Position;

        void Section(string title)
        {
            if (!measureOnly && Visible(y, font.LineSpacing))
                FarmRpgUi.DrawSectionHeader(sb!, font, title, x, y, width);
            y += font.LineSpacing + 10;
        }

        void AudioRow(string label, SliderKind kind)
        {
            if (!measureOnly && Visible(y, RowHeight))
            {
                var textY = y + (RowHeight - font.LineSpacing) / 2;
                UiText.Draw(sb!, font, label, new Vector2(x, textY), FarmRpgUi.Ink);

                var box = new Rectangle(x + width - RowHeight, y, RowHeight, RowHeight);
                var muted = IsMuted(kind);
                FarmRpgUi.DrawToggle(sb!, box, on: !muted, Hit(box, mouse));
                _toggles.Add(new ToggleItem(box, kind));

                var track = new Rectangle(x + LabelWidth + 8, y, width - LabelWidth - 8 - PercentWidth - RowHeight - 12, RowHeight);
                var value = Volume(kind);
                FarmRpgUi.DrawSlider(sb!, track, muted ? 0f : value, muted ? FarmRpgUi.InkFaint : FarmRpgUi.Rust);
                _sliders.Add(new SliderItem(track, kind));

                var pct = muted ? "Off" : $"{(int)MathF.Round(value * 100)}%";
                UiText.DrawRight(sb!, font, pct, track.Right + 8 + PercentWidth, textY, FarmRpgUi.InkMuted);
            }
            y += RowHeight + FarmRpgUi.Gap;
        }

        void ButtonRow(params (string Label, Action? Click, bool Enabled, bool Danger)[] row)
        {
            if (!measureOnly && Visible(y, FarmRpgUi.ButtonHeight))
            {
                const int gap = 12;
                var buttonW = (width - gap * (row.Length - 1)) / row.Length;
                for (var i = 0; i < row.Length; i++)
                {
                    var (label, click, enabled, danger) = row[i];
                    var rect = new Rectangle(x + i * (buttonW + gap), y, buttonW, FarmRpgUi.ButtonHeight);
                    var usable = enabled && click != null;
                    var pressed = usable && Hit(rect, mouse);
                    FarmRpgUi.DrawButton(sb!, rect, pressed: pressed, disabled: !usable, danger: danger);
                    var textArea = pressed ? new Rectangle(rect.X, rect.Y + 2, rect.Width, rect.Height - 2) : rect;
                    UiText.DrawCentered(sb!, font, label, textArea, usable ? FarmRpgUi.Ink : FarmRpgUi.InkFaint);
                    if (usable)
                        _buttons.Add(new ButtonItem(rect, label, () => { click!(); Close(); }, true, danger));
                }
            }
            y += FarmRpgUi.ButtonHeight + FarmRpgUi.Gap;
        }

        Section("Sound");
        AudioRow("Music", SliderKind.Music);
        AudioRow("Effects", SliderKind.Sfx);
        y += FarmRpgUi.SectionGap - FarmRpgUi.Gap;

        if (!_deathMenuMode)
        {
            Section("Windows");
            ButtonRow(("Character  (C)", OnOpenCharacter, true, false), ("Spell Book  (K)", OnOpenSpellBook, true, false));
            ButtonRow(("Inventory  (I)", OnOpenInventory, true, false), ("Skills  (L)", OnOpenSkills, true, false));
            y += FarmRpgUi.SectionGap - FarmRpgUi.Gap;

            Section("Homestead");
            if (_destroyHouseEnabled)
                ButtonRow(("Destroy house", OnDestroyHouse, true, true));
            else
                ButtonRow((_buildHouseEnabled ? "Build house" : "House already built", OnBuildHouse, _buildHouseEnabled, false));
            y += FarmRpgUi.SectionGap - FarmRpgUi.Gap;
        }

        Section(_deathMenuMode ? "Your journey" : "Account");
        if (_showNewLifeButton)
            ButtonRow(("Begin anew", OnNewLife, true, false), ("Log out", OnLogout, true, true));
        else
            ButtonRow(("Log out", OnLogout, true, true));

        if (infoLines is { Count: > 0 })
        {
            y += FarmRpgUi.SectionGap - FarmRpgUi.Gap;
            Section("Info");
            foreach (var line in infoLines)
            {
                if (!measureOnly && Visible(y, font.LineSpacing))
                    UiText.Draw(sb!, font, UiText.Truncate(font, line, width), new Vector2(x, y), FarmRpgUi.InkMuted);
                y += font.LineSpacing + 2;
            }
        }

        y += FarmRpgUi.SectionGap;
        if (!measureOnly && Visible(y, font.LineSpacing))
            UiText.DrawCentered(sb!, font, FooterHint, new Rectangle(x, y, width, font.LineSpacing), FarmRpgUi.InkFaint);
        y += font.LineSpacing;

        return y - top;
    }

    private bool Visible(int y, int height) => y >= _viewport.Y && y + height <= _viewport.Bottom;

    private static bool Hit(Rectangle rect, Point p, int grow = 0)
    {
        if (grow > 0) rect.Inflate(0, grow / 2);
        return rect.Contains(p);
    }

    private static bool IsMuted(SliderKind kind) => kind == SliderKind.Music ? MusicPlayer.IsMuted : SfxPlayer.IsMuted;

    private static float Volume(SliderKind kind) => kind == SliderKind.Music ? MusicPlayer.Volume : SfxPlayer.Volume;

    private static void SetVolume(SliderKind kind, float value, bool save)
    {
        value = Math.Clamp(value, 0f, 1f);
        if (kind == SliderKind.Music)
        {
            MusicPlayer.SetVolume(value, save);
            if (MusicPlayer.IsMuted && value > 0f) MusicPlayer.SetMuted(false, save);
        }
        else
        {
            AudioSettings.SetSfx(SfxPlayer.IsMuted && value <= 0f, value, save);
        }
    }

    private static void ToggleMute(SliderKind kind)
    {
        if (kind == SliderKind.Music)
            MusicPlayer.SetMuted(!MusicPlayer.IsMuted);
        else
            AudioSettings.SetSfx(!SfxPlayer.IsMuted, SfxPlayer.Volume);
    }

    /// <summary>Ends a slider drag and saves the final value once.</summary>
    private void StopDragging()
    {
        if (_dragging is { } kind)
            SetVolume(kind, Volume(kind), save: true);
        _dragging = null;
    }
}
