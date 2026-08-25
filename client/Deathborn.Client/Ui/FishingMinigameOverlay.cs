using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Stardew Valley-style fishing: keep the fish inside your bar until the catch meter fills.</summary>
public sealed class FishingMinigameOverlay
{
    private enum Phase { Idle, Waiting, Bite, Playing, Success, Failed }

    private const float TrackHeight = 360f;
    private const float TrackWidth = 56f;
    private const float BarHeight = 96f;
    private const float FishSize = 22f;

    private Texture2D? _fishIcon;
    private Phase _phase = Phase.Idle;
    private string _spotId = "";
    private string _spotName = "";
    private float _waitTimer;
    private float _fishY;
    private float _fishVel;
    private float _fishTarget;
    private float _barY;
    private float _barVel;
    private float _progress;
    private float _resultTimer;
    private float _fishTargetTimer;
    private int _fishingLevel = 1;

    public bool IsActive => _phase != Phase.Idle;
    public bool IsReeling => _phase == Phase.Playing;
    public event Action<string>? Caught;
    public event Action? Cancelled;

    public void Start(string spotId, string spotName, int fishingLevel, Texture2D? fishIcon = null)
    {
        _spotId = spotId;
        _spotName = spotName;
        _fishIcon = fishIcon;
        _fishingLevel = Math.Max(1, fishingLevel);
        _phase = Phase.Waiting;
        _waitTimer = 1.2f + Random.Shared.NextSingle() * 2.2f;
        _fishY = TrackHeight * 0.5f;
        _fishTarget = _fishY;
        _barY = TrackHeight * 0.55f;
        _barVel = 0;
        _fishVel = 0;
        _progress = 0.25f;
        _resultTimer = 0;
        _fishTargetTimer = 0;
    }

    public void Cancel()
    {
        if (_phase == Phase.Idle) return;
        _phase = Phase.Idle;
        Cancelled?.Invoke();
    }

    public void Dismiss()
    {
        _phase = Phase.Idle;
        _fishIcon = null;
    }

    public bool Update(float dt, KeyboardState kb, KeyboardState prevKb, MouseState mouse, MouseState prevMouse)
    {
        if (_phase == Phase.Idle) return false;

        if (InputKeys.EscapePressed(kb, prevKb))
        {
            Cancel();
            return true;
        }

        switch (_phase)
        {
            case Phase.Waiting:
                _waitTimer -= dt;
                if (_waitTimer <= 0)
                    _phase = Phase.Bite;
                break;

            case Phase.Bite:
                if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
                    || kb.IsKeyDown(Keys.Space) && !prevKb.IsKeyDown(Keys.Space))
                {
                    BeginPlaying();
                }
                break;

            case Phase.Playing:
                UpdatePlaying(dt, kb, mouse);
                break;

            case Phase.Success:
            case Phase.Failed:
                _resultTimer -= dt;
                if (_resultTimer <= 0)
                    _phase = Phase.Idle;
                break;
        }

        return true;
    }

    private void BeginPlaying()
    {
        _phase = Phase.Playing;
        _progress = 0.2f;
        _barY = TrackHeight * 0.55f;
        _fishY = TrackHeight * 0.35f;
        PickFishTarget();
    }

    private void UpdatePlaying(float dt, KeyboardState kb, MouseState mouse)
    {
        var reeling = kb.IsKeyDown(Keys.Space) || mouse.LeftButton == ButtonState.Pressed;
        const float gravity = 520f;
        const float lift = 680f;
        _barVel += (reeling ? -lift : gravity) * dt;
        _barVel *= 0.86f;
        _barY += _barVel * dt;
        _barY = Math.Clamp(_barY, BarHeight * 0.5f, TrackHeight - BarHeight * 0.5f);

        _fishTargetTimer -= dt;
        if (_fishTargetTimer <= 0)
            PickFishTarget();

        var fishAccel = (_fishTarget - _fishY) * (2.4f + _fishingLevel * 0.02f);
        _fishVel += fishAccel * dt;
        _fishVel += MathF.Sin(_fishY * 0.04f) * 18f * dt;
        _fishVel *= 0.92f;
        _fishY += _fishVel * dt;
        _fishY = Math.Clamp(_fishY, FishSize, TrackHeight - FishSize);

        var barTop = _barY - BarHeight * 0.5f;
        var barBottom = _barY + BarHeight * 0.5f;
        var inBar = _fishY >= barTop && _fishY <= barBottom;
        var catchRate = inBar ? 0.38f + _fishingLevel * 0.004f : -0.22f;
        _progress += catchRate * dt;
        _progress = Math.Clamp(_progress, 0f, 1f);

        if (_progress >= 1f)
        {
            _phase = Phase.Success;
            _resultTimer = 1.4f;
            Caught?.Invoke(_spotId);
        }
        else if (_progress <= 0f)
        {
            _phase = Phase.Failed;
            _resultTimer = 1.2f;
        }
    }

    private void PickFishTarget()
    {
        var difficulty = Math.Clamp(_fishingLevel * 0.015f, 0.05f, 0.45f);
        _fishTarget = Random.Shared.NextSingle() * TrackHeight;
        if (Random.Shared.NextDouble() < difficulty)
            _fishTarget = _fishY > TrackHeight * 0.5f ? TrackHeight * 0.12f : TrackHeight * 0.88f;
        _fishTargetTimer = 0.35f + Random.Shared.NextSingle() * 0.9f;
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (_phase == Phase.Idle) return;

        var cx = GameViewport.Width / 2;
        var cy = GameViewport.Height / 2;
        var panelW = Math.Min(360, Math.Max(240, GameViewport.Width - 32));
        var panelH = Math.Min((int)TrackHeight + 112, Math.Max(260, GameViewport.Height - 32));
        var panel = new Rectangle(cx - panelW / 2, cy - panelH / 2, panelW, panelH);
        FarmRpgUi.DrawWindowPanel(sb, panel, 0.98f);

        var title = _phase switch
        {
            Phase.Waiting => $"Fishing at {_spotName}...",
            Phase.Bite => "Bite! Click or Space to hook!",
            Phase.Playing => "Keep the fish in the green bar!",
            Phase.Success => "Caught!",
            Phase.Failed => "The fish got away...",
            _ => "Fishing",
        };
        var titlePanel = new Rectangle(panel.X + 12, panel.Y + 10, panel.Width - 24, 36);
        FarmRpgUi.DrawTitle(sb, titlePanel);
        DrawCenteredText(sb, font, title, titlePanel, FarmRpgUi.Ink, 1f);

        if (_phase is Phase.Waiting or Phase.Bite)
        {
            var hint = _phase == Phase.Waiting ? "Waiting for a bite..." : "Hook it now!";
            var hintPanel = new Rectangle(panel.X + 30, cy - 24, panel.Width - 60, 48);
            FarmRpgUi.DrawInsetPanel(sb, hintPanel);
            DrawCenteredText(sb, font, hint, hintPanel, FarmRpgUi.InkMuted, 0.95f);
            if (_phase == Phase.Bite)
            {
                var pulse = 0.6f + MathF.Sin((float)Environment.TickCount64 * 0.01f) * 0.4f;
                var pulseSize = (int)(28 + pulse * 12);
                var pulseRect = new Rectangle(cx - pulseSize / 2, hintPanel.Bottom + 12, pulseSize, pulseSize);
                FarmRpgUi.DrawButton(sb, pulseRect, pressed: pulse > 0.8f);
            }
            return;
        }

        if (_phase is Phase.Success or Phase.Failed)
        {
            var resultPanel = new Rectangle(panel.X + 42, cy - 24, panel.Width - 84, 48);
            FarmRpgUi.DrawInsetPanel(sb, resultPanel);
            DrawCenteredText(sb, font,
                _phase == Phase.Success ? "Nice catch!" : "Try again at the fishing spot.",
                resultPanel, _phase == Phase.Success ? new Color(52, 108, 66) : FarmRpgUi.Rust, 0.9f);
            return;
        }

        var displayTrackH = Math.Max(140, panel.Height - 116);
        var trackX = cx - TrackWidth / 2;
        var trackY = panel.Y + 50;
        var track = new Rectangle((int)trackX, trackY, (int)TrackWidth, displayTrackH);
        FarmRpgUi.DrawInsetPanel(sb, track);

        var displayScale = displayTrackH / TrackHeight;
        var displayBarH = Math.Max(24, (int)(BarHeight * displayScale));
        var barRect = new Rectangle(
            (int)trackX + 4,
            (int)(trackY + _barY * displayScale - displayBarH * 0.5f),
            (int)TrackWidth - 8,
            displayBarH);
        FarmRpgUi.DrawBar(sb, barRect, 1f, new Color(58, 145, 72));

        var fishSize = Math.Max(16, (int)(FishSize * displayScale));
        var fishRect = new Rectangle(
            cx - fishSize / 2,
            (int)(trackY + _fishY * displayScale - fishSize * 0.5f),
            fishSize,
            fishSize);
        if (_fishIcon != null)
            sb.Draw(_fishIcon, fishRect, Color.White);
        else
            FarmRpgUi.DrawButton(sb, fishRect);

        var meterW = Math.Min(200, panel.Width - 48);
        var meter = new Rectangle(cx - meterW / 2, panel.Bottom - 28, meterW, 18);
        FarmRpgUi.DrawBar(sb, meter, _progress, new Color(55, 120, 175));

        var ctrl = "Hold Space / Click to raise bar  |  Esc to cancel";
        var ctrlArea = new Rectangle(panel.X + 16, panel.Bottom - 52, panel.Width - 32, 22);
        DrawCenteredText(sb, font, ctrl, ctrlArea, FarmRpgUi.InkMuted, 0.78f);
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
