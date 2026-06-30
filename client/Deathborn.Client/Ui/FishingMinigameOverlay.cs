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
    public event Action<string>? Caught;
    public event Action? Cancelled;

    public void Start(string spotId, string spotName, int fishingLevel)
    {
        _spotId = spotId;
        _spotName = spotName;
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

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height),
            new Color(0, 0, 0, 0.45f));

        var cx = GameViewport.Width / 2;
        var cy = GameViewport.Height / 2;
        var panel = new Rectangle((int)(cx - 180), (int)(cy - TrackHeight / 2 - 40), 360, (int)(TrackHeight + 80));
        DrawPrimitives.FillRect(sb, panel, new Color(22, 28, 38, 240));
        DrawBorder(sb, panel, new Color(120, 170, 210), 2);

        var title = _phase switch
        {
            Phase.Waiting => $"Fishing at {_spotName}...",
            Phase.Bite => "Bite! Click or Space to hook!",
            Phase.Playing => "Keep the fish in the green bar!",
            Phase.Success => "Caught!",
            Phase.Failed => "The fish got away...",
            _ => "Fishing",
        };
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title, new Vector2(cx - titleSize.X / 2f, panel.Y + 12), new Color(220, 230, 245));

        if (_phase is Phase.Waiting or Phase.Bite)
        {
            var hint = _phase == Phase.Waiting ? "Waiting for a bite..." : "Hook it now!";
            var hintSize = font.MeasureString(hint);
            sb.DrawString(font, hint, new Vector2(cx - hintSize.X / 2f, cy), new Color(180, 200, 220));
            if (_phase == Phase.Bite)
            {
                var pulse = 0.6f + MathF.Sin((float)Environment.TickCount64 * 0.01f) * 0.4f;
                DrawPrimitives.FillCircle(sb, new Vector2(cx, cy + 40), 14 + pulse * 6, new Color(0.95f, 0.55f, 0.2f, 0.85f));
            }
            return;
        }

        if (_phase is Phase.Success or Phase.Failed) return;

        var trackX = cx - TrackWidth / 2;
        var trackY = cy - TrackHeight / 2;
        var track = new Rectangle((int)trackX, (int)trackY, (int)TrackWidth, (int)TrackHeight);
        DrawPrimitives.FillRect(sb, track, new Color(18, 24, 34));
        DrawBorder(sb, track, new Color(70, 90, 110), 1);

        var barRect = new Rectangle((int)trackX, (int)(trackY + _barY - BarHeight * 0.5f), (int)TrackWidth, (int)BarHeight);
        DrawPrimitives.FillRect(sb, barRect, new Color(0.25f, 0.72f, 0.38f, 0.75f));

        var fishPos = new Vector2(trackX + TrackWidth / 2f, trackY + _fishY);
        DrawPrimitives.FillCircle(sb, fishPos, FishSize * 0.5f, new Color(0.45f, 0.72f, 0.95f));
        DrawPrimitives.DrawCircleOutline(sb, fishPos, FishSize * 0.5f, new Color(0.15f, 0.35f, 0.55f), 16, 2f);

        var meterW = 200f;
        var meter = new Rectangle((int)(cx - meterW / 2), panel.Bottom - 36, (int)meterW, 14);
        DrawPrimitives.FillRect(sb, meter, new Color(30, 34, 42));
        DrawPrimitives.FillRect(sb, new Rectangle(meter.X, meter.Y, (int)(meterW * _progress), meter.Height),
            new Color(0.35f, 0.75f, 0.95f));

        var ctrl = "Hold Space / Click to raise bar  |  Esc to cancel";
        var ctrlSize = font.MeasureString(ctrl);
        sb.DrawString(font, ctrl, new Vector2(cx - ctrlSize.X / 2f, panel.Bottom - 58), new Color(160, 170, 185));
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
