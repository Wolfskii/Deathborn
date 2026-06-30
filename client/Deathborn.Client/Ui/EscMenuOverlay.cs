using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Esc menu with music volume, mute, and game info.</summary>
public sealed class EscMenuOverlay
{
    private const int PanelW = 380;
    private const int BasePanelH = 210;

    private static readonly Color PanelFill = new(28, 24, 18);
    private static readonly Color PanelBorder = new(210, 170, 80);
    private static readonly Color GoldDim = new(130, 105, 55);

    private Rectangle _panel;
    private Rectangle _sliderTrack;
    private Rectangle _muteBox;
    private bool _draggingVolume;
    private MouseState _prevMouse;
    private IReadOnlyList<string>? _infoLines;

    public bool IsOpen { get; private set; }

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

        DrawPanel(sb, _panel);

        var cx = _panel.X + _panel.Width / 2;
        var title = "Menu";
        sb.DrawString(font, title, new Vector2(cx - font.MeasureString(title).X / 2f, _panel.Y + 14), PanelBorder);

        var musicLabel = "Music volume";
        sb.DrawString(font, musicLabel, new Vector2(_panel.X + 24, _panel.Y + 52), Color.White);

        var track = _sliderTrack;
        DrawPrimitives.FillRect(sb, track, new Color(35, 32, 28));
        DrawBorder(sb, track, GoldDim, 1);

        var fillW = (int)(track.Width * MusicPlayer.Volume);
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(track.X, track.Y, fillW, track.Height), new Color(90, 72, 38));

        var knobX = track.X + fillW;
        DrawPrimitives.FillRect(sb, new Rectangle(knobX - 2, track.Y - 2, 4, track.Height + 4), PanelBorder);

        var pct = $"{(int)(MusicPlayer.Volume * 100)}%";
        sb.DrawString(font, pct, new Vector2(track.Right + 10, track.Y + 2), new Color(200, 200, 210));

        var mute = new Checkbox
        {
            Label = "Mute music",
            Checked = MusicPlayer.IsMuted,
            BoxBounds = _muteBox,
        };
        mute.Draw(sb, font, _muteBox.Contains(Mouse.GetState().Position));

        if (_infoLines is { Count: > 0 })
        {
            var infoY = _muteBox.Bottom + 18;
            sb.DrawString(font, "Info", new Vector2(_panel.X + 24, infoY), PanelBorder);
            infoY += font.LineSpacing + 2;

            foreach (var line in _infoLines)
            {
                sb.DrawString(font, line, new Vector2(_panel.X + 24, infoY), new Color(200, 200, 210));
                infoY += font.LineSpacing;
            }
        }

        var hint = "Esc to close  |  F12 toggles HUD";
        sb.DrawString(font, hint,
            new Vector2(cx - font.MeasureString(hint).X / 2f, _panel.Bottom - 28),
            new Color(180, 180, 190));
    }

    private void Layout(SpriteFont font)
    {
        var cx = GameViewport.Width / 2;
        var cy = GameViewport.Height / 2;
        var infoCount = _infoLines?.Count ?? 0;
        var panelH = BasePanelH + (infoCount > 0 ? 36 + infoCount * (int)font.LineSpacing : 0);

        _panel = new Rectangle(cx - PanelW / 2, cy - panelH / 2, PanelW, panelH);
        _sliderTrack = new Rectangle(_panel.X + 24, _panel.Y + 78, PanelW - 110, 18);
        _muteBox = new Rectangle(_panel.X + 24, _panel.Y + 118, 20, 20);
    }

    private void SetVolumeFromMouse(int mouseX)
    {
        var t = (mouseX - _sliderTrack.X) / (float)_sliderTrack.Width;
        MusicPlayer.SetVolume(Math.Clamp(t, 0f, 1f));
    }

    private static void DrawPanel(SpriteBatch sb, Rectangle panel)
    {
        DrawPrimitives.FillRect(sb, panel, PanelFill);
        DrawBorder(sb, panel, PanelBorder, 2);
        DrawBorder(sb, new Rectangle(panel.X + 6, panel.Y + 6, panel.Width - 12, panel.Height - 12), GoldDim, 1);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
