using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Fishing. Waiting for a bite and the bite itself happen in the world (WorldScreen draws the
/// bobber, the "!" and the hop); clicking during the bite hooks the fish and opens this window.
///
/// Pendulum fishing. The line hangs from the rod tip and the bobber swings along a half circle
/// below it. Gravity pulls the bobber back to the bottom of the swing; A/D (or holding the mouse
/// on either side) pushes it left or right. Keep the fish inside the bobber's green catch zone
/// until the catch meter fills. Fish like to run for the ends of the arc, where you have to
/// fight the swing to stay with them.
/// </summary>
public sealed class FishingMinigameOverlay
{
    private enum Phase { Idle, Waiting, Bite, Playing, Success, Failed }

    /// <summary>How long the bite lasts before the fish swims off.</summary>
    private const float BiteWindowSeconds = 1.6f;
    /// <summary>No drain right after hooking, so the player can settle on the fish.</summary>
    private const float GraceSeconds = 1.2f;
    private const float StartProgress = 0.3f;
    private const float DrainPerSecond = 0.14f;

    // Swing physics in track units (0 = left end, 1 = right end, 0.5 = bottom of the swing).
    private const float Gravity = 2.6f;
    private const float Push = 4.2f;
    private const float Damping = 2.4f;

    private Texture2D? _fishIcon;
    private Phase _phase = Phase.Idle;
    private string _spotId = "";
    private string _spotName = "";
    private int _fishingLevel = 1;

    private float _waitTimer;
    private float _biteTimer;
    private float _resultTimer;
    private float _grace;
    private float _time;
    private float _phaseTime;
    private bool _mouseArmed;
    private bool _castSplashPlayed;

    private float _bobber = 0.5f;
    private float _bobberVel;
    private float _fish = 0.5f;
    private float _fishVel;
    private float _fishTarget = 0.5f;
    private float _fishTargetTimer;
    private float _fishLivelyness;
    private float _progress;
    private bool _fishInZone;
    private bool _wasInZone;
    private float _zoneFlash;

    public bool IsActive => _phase != Phase.Idle;
    public bool IsReeling => _phase == Phase.Playing;
    /// <summary>Line is in the water, no fish yet (shown in the world, no window).</summary>
    public bool IsWaiting => _phase == Phase.Waiting;
    /// <summary>A fish is nibbling: click to hook (shown in the world, no window).</summary>
    public bool IsBiting => _phase == Phase.Bite;
    /// <summary>The minigame window is showing (fight or result).</summary>
    public bool ShowsWindow => _phase is Phase.Playing or Phase.Success or Phase.Failed;
    /// <summary>Seconds since the line went in or the bite started (for world animations).</summary>
    public float PhaseTime => _phaseTime;
    /// <summary>1 when the bite starts, 0 when the fish swims off.</summary>
    public float BiteTimeLeft01 => _phase == Phase.Bite ? Math.Clamp(_biteTimer / BiteWindowSeconds, 0f, 1f) : 0f;
    /// <summary>A fish took the bait (play the "!" and hop).</summary>
    public event Action? BiteStarted;
    /// <summary>Clicked while waiting: the line comes back empty.</summary>
    public event Action? ReeledInEarly;
    public event Action<string>? Caught;
    /// <summary>The player gave up (Esc).</summary>
    public event Action? Cancelled;
    /// <summary>The fish got away (missed bite or catch meter emptied).</summary>
    public event Action? Escaped;

    /// <summary>Half the width of the green catch zone, in track units. Grows a little with level.</summary>
    private float ZoneHalf => Math.Clamp(0.11f + _fishingLevel * 0.0025f, 0.11f, 0.17f);

    public void Start(string spotId, string spotName, int fishingLevel, Texture2D? fishIcon = null)
    {
        _spotId = spotId;
        _spotName = spotName;
        _fishIcon = fishIcon;
        _fishingLevel = Math.Max(1, fishingLevel);
        _phase = Phase.Waiting;
        _waitTimer = 1.4f + Random.Shared.NextSingle() * 2.4f;
        _bobber = 0.5f;
        _bobberVel = 0;
        _progress = StartProgress;
        _resultTimer = 0;
        _time = 0;
        _phaseTime = 0;
        _castSplashPlayed = false;
    }

    /// <summary>Player gave up (Esc). Escape is routed here by WorldScreen.HandleEscape.</summary>
    public void Cancel()
    {
        if (_phase == Phase.Idle) return;
        _phase = Phase.Idle;
        SfxPlayer.StopLoop();
        Cancelled?.Invoke();
    }

    public void Dismiss()
    {
        _phase = Phase.Idle;
        _fishIcon = null;
        SfxPlayer.StopLoop();
    }

    private void SetPhase(Phase phase)
    {
        _phase = phase;
        _phaseTime = 0;
    }

    public bool Update(float dt, KeyboardState kb, KeyboardState prevKb, MouseState mouse, MouseState prevMouse)
    {
        if (_phase == Phase.Idle) return false;
        _time += dt;
        _phaseTime += dt;

        var clicked = (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
            || (mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released)
            || (kb.IsKeyDown(Keys.Space) && !prevKb.IsKeyDown(Keys.Space));

        switch (_phase)
        {
            case Phase.Waiting:
                // The bobber lands a moment after the cast.
                if (!_castSplashPlayed && _phaseTime >= 0.3f)
                {
                    _castSplashPlayed = true;
                    SfxPlayer.PlayFishingCast();
                }
                if (clicked && _phaseTime > 0.35f)
                {
                    // Too early: reel the empty line back in.
                    _phase = Phase.Idle;
                    ReeledInEarly?.Invoke();
                    break;
                }
                _waitTimer -= dt;
                if (_waitTimer <= 0)
                {
                    SetPhase(Phase.Bite);
                    _biteTimer = BiteWindowSeconds;
                    SfxPlayer.PlayFishingBite();
                    BiteStarted?.Invoke();
                }
                break;

            case Phase.Bite:
                if (clicked)
                {
                    BeginPlaying();
                    break;
                }
                _biteTimer -= dt;
                if (_biteTimer <= 0)
                    Fail();
                break;

            case Phase.Playing:
                UpdatePlaying(dt, kb, mouse);
                break;

            case Phase.Success:
            case Phase.Failed:
                _resultTimer -= dt;
                if (_resultTimer <= 0)
                    SetPhase(Phase.Idle);
                break;
        }

        return true;
    }

    private void BeginPlaying()
    {
        SetPhase(Phase.Playing);
        _mouseArmed = false;
        SfxPlayer.PlayFishingHook();
        SfxPlayer.PlayLoop(GameSfx.FishingReelLoop, 0.4f);
        _progress = StartProgress;
        _grace = GraceSeconds;
        // Start on the fish so the first second is about learning the swing, not chasing.
        _fish = 0.5f;
        _fishVel = 0;
        _bobber = 0.5f;
        _bobberVel = 0;
        _fishLivelyness = 0.7f + Random.Shared.NextSingle() * 0.6f + Math.Min(0.6f, _fishingLevel * 0.02f);
        _fishTargetTimer = 0.8f;
        _fishTarget = 0.5f;
        _wasInZone = true;
    }

    private void UpdatePlaying(float dt, KeyboardState kb, MouseState mouse)
    {
        // Input: A/D or arrows, or hold the left mouse button to swing left and the right one to
        // swing right. Mouse buttons count only after the hooking click has been released.
        var leftDown = mouse.LeftButton == ButtonState.Pressed;
        var rightDown = mouse.RightButton == ButtonState.Pressed;
        if (!leftDown && !rightDown)
            _mouseArmed = true;
        var push = 0f;
        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left) || (_mouseArmed && leftDown)) push -= 1f;
        if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right) || (_mouseArmed && rightDown)) push += 1f;

        // Pendulum: gravity toward the bottom of the swing, damped.
        var fromBottom = (_bobber - 0.5f) * MathF.PI;
        _bobberVel += (-Gravity * MathF.Sin(fromBottom) + push * Push) * dt;
        _bobberVel -= _bobberVel * Damping * dt;
        _bobber += _bobberVel * dt;
        var half = ZoneHalf;
        if (_bobber < half)
        {
            _bobber = half;
            _bobberVel = MathF.Max(0f, _bobberVel) * 0.3f;
        }
        else if (_bobber > 1f - half)
        {
            _bobber = 1f - half;
            _bobberVel = MathF.Min(0f, _bobberVel) * 0.3f;
        }

        UpdateFish(dt);

        _fishInZone = MathF.Abs(_fish - _bobber) <= half;
        if (_fishInZone != _wasInZone)
            _zoneFlash = 0.25f;
        _wasInZone = _fishInZone;
        _zoneFlash = MathF.Max(0f, _zoneFlash - dt);

        // Reel sound: louder and higher while the fish is on the hook, slacker when it slips.
        SfxPlayer.SetLoopVolume(_fishInZone ? 0.5f : 0.28f);
        SfxPlayer.SetLoopPitch(_fishInZone ? 0.12f : -0.12f);

        _grace = MathF.Max(0f, _grace - dt);
        if (_fishInZone)
            _progress += (0.26f + _fishingLevel * 0.004f) * dt;
        else if (_grace <= 0f)
            _progress -= DrainPerSecond * dt;
        _progress = Math.Clamp(_progress, 0f, 1f);

        if (_progress >= 1f)
        {
            // The window closes at once; WorldScreen flies the fish out of the water to the player.
            SetPhase(Phase.Idle);
            SfxPlayer.StopLoop();
            SfxPlayer.PlayFishingCast();
            Caught?.Invoke(_spotId);
        }
        else if (_progress <= 0f)
        {
            Fail();
        }
    }

    private void UpdateFish(float dt)
    {
        _fishTargetTimer -= dt;
        if (_fishTargetTimer <= 0)
            PickFishTarget();

        var accel = (_fishTarget - _fish) * 9f * _fishLivelyness;
        _fishVel += accel * dt;
        // A little wiggle so the fish never sits perfectly still.
        _fishVel += MathF.Sin(_time * 7.3f + _fish * 11f) * 0.25f * dt;
        _fishVel -= _fishVel * 3.2f * dt;
        _fish = Math.Clamp(_fish + _fishVel * dt, 0.05f, 0.95f);
    }

    private void PickFishTarget()
    {
        // Higher level fish run for the ends of the arc more often (where gravity fights you).
        var runChance = Math.Clamp(0.18f + _fishingLevel * 0.012f, 0.18f, 0.5f);
        if (Random.Shared.NextSingle() < runChance)
            _fishTarget = _fish < 0.5f ? 0.82f + Random.Shared.NextSingle() * 0.12f : 0.06f + Random.Shared.NextSingle() * 0.12f;
        else
            _fishTarget = 0.15f + Random.Shared.NextSingle() * 0.7f;
        _fishTargetTimer = (0.7f + Random.Shared.NextSingle() * 1.2f) / _fishLivelyness;
    }

    private void Fail()
    {
        // A missed bite needs no window: the world shows the fish swimming off.
        var missedBite = _phase == Phase.Bite;
        SetPhase(missedBite ? Phase.Idle : Phase.Failed);
        _resultTimer = 1.4f;
        SfxPlayer.StopLoop();
        SfxPlayer.PlayFishingEscape();
        Escaped?.Invoke();
    }

    // ---------------------------------------------------------------- drawing

    private readonly record struct ArcLayout(Rectangle Panel, Rectangle Pond, Vector2 Pivot, float Radius);

    private static ArcLayout Layout()
    {
        var w = Math.Min(460, Math.Max(300, GameViewport.Width - 32));
        var h = Math.Min(360, Math.Max(280, GameViewport.Height - 32));
        var panel = new Rectangle(GameViewport.Width / 2 - w / 2, GameViewport.Height / 2 - h / 2 - 20, w, h);
        var pond = new Rectangle(panel.X + 20, panel.Y + 56, panel.Width - 40, panel.Height - 120);
        var radius = MathF.Min(pond.Width * 0.5f - 30f, pond.Height - 50f);
        var pivot = new Vector2(pond.Center.X, pond.Y + 18);
        return new ArcLayout(panel, pond, pivot, radius);
    }

    /// <summary>Point on the swing arc: 0 = left end, 0.5 = bottom, 1 = right end.</summary>
    private static Vector2 ArcPoint(ArcLayout l, float t, float radius)
    {
        var angle = MathF.PI * (1f - t);
        return l.Pivot + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }

    private static readonly Color PondDeep = new(38, 92, 140);
    private static readonly Color PondShallow = new(70, 140, 186);
    private static readonly Color TrackDark = new(32, 58, 86);
    private static readonly Color TrackLight = new(120, 182, 214);
    private static readonly Color ZoneFill = new(92, 188, 92);
    private static readonly Color ZoneEdge = new(198, 246, 150);
    private static readonly Color ZoneMiss = new(214, 120, 74);
    private static readonly Color LineColor = new(240, 236, 220);
    private static readonly Color MeterFill = new(244, 196, 72);
    private static readonly Color MeterTrack = new(54, 40, 34);

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!ShowsWindow) return;

        var l = Layout();
        FarmRpgUi.DrawWindowPanel(sb, l.Panel, 0.98f);

        var title = _phase switch
        {
            Phase.Playing => _fishInZone ? "Reel it in!" : "Swing back to the fish!",
            Phase.Success => "Caught!",
            Phase.Failed => "The fish got away...",
            _ => "Fishing",
        };
        var titlePanel = new Rectangle(l.Panel.X + 14, l.Panel.Y + 10, l.Panel.Width - 28, 36);
        FarmRpgUi.DrawTitle(sb, titlePanel);
        DrawCenteredText(sb, font, SpriteFontSafe.Filter(title), titlePanel, FarmRpgUi.Ink, 1f);

        DrawPond(sb, l);
        DrawTrack(sb, l);

        switch (_phase)
        {
            case Phase.Playing:
                DrawPlaying(sb, l);
                break;
            case Phase.Success:
                DrawResult(sb, font, l, success: true);
                break;
            case Phase.Failed:
                DrawResult(sb, font, l, success: false);
                break;
        }

        var hint = _phase switch
        {
            Phase.Playing => "Hold A / D or left / right mouse button to swing  |  Esc to give up",
            _ => "",
        };
        if (hint.Length > 0)
        {
            var hintArea = new Rectangle(l.Panel.X + 18, l.Panel.Bottom - 34, l.Panel.Width - 36, 22);
            DrawCenteredText(sb, font, hint, hintArea, FarmRpgUi.InkMuted, 0.78f);
        }
    }

    private void DrawPond(SpriteBatch sb, ArcLayout l)
    {
        FarmRpgUi.DrawInsetPanel(sb, l.Pond);
        var water = new Rectangle(l.Pond.X + 6, l.Pond.Y + 6, l.Pond.Width - 12, l.Pond.Height - 12);
        DrawPrimitives.FillRoundedRect(sb, water, PondDeep, 6);
        // Light bands that drift slowly, like a sunlit surface.
        for (var i = 0; i < 4; i++)
        {
            var y = water.Y + 10 + (int)((i * 37 + _time * 8f) % Math.Max(1, water.Height - 20));
            var band = new Rectangle(water.X + 10 + i * 23 % 40, y, water.Width - 20 - i * 31 % 50, 2);
            DrawPrimitives.FillRect(sb, band, PondShallow * 0.55f);
        }

        // Rod tip at the pivot.
        var tip = l.Pivot;
        DrawPrimitives.DrawLine(sb, tip + new Vector2(-34, -8), tip, new Color(122, 74, 40), 4f);
        DrawPrimitives.FillCircle(sb, tip, 3.5f, new Color(90, 52, 30), 12);
    }

    private static void DrawTrack(SpriteBatch sb, ArcLayout l)
    {
        DrawArc(sb, l, 0f, 1f, l.Radius, 20f, TrackDark);
        DrawArc(sb, l, 0f, 1f, l.Radius, 12f, TrackLight * 0.55f);
        // Tick marks every eighth of the swing.
        for (var i = 0; i <= 8; i++)
        {
            var t = i / 8f;
            var a = ArcPoint(l, t, l.Radius + 13f);
            var b = ArcPoint(l, t, l.Radius + 19f);
            DrawPrimitives.DrawLine(sb, a, b, TrackLight * 0.8f, 2f);
        }
    }

    private void DrawPlaying(SpriteBatch sb, ArcLayout l)
    {
        var half = ZoneHalf;
        var zoneColor = _fishInZone ? ZoneFill : ZoneMiss;
        var flash = _zoneFlash > 0f ? 0.25f : 0f;
        DrawArc(sb, l, _bobber - half, _bobber + half, l.Radius, 22f, zoneColor * (0.9f + flash));
        DrawArc(sb, l, _bobber - half, _bobber + half, l.Radius + 9f, 3f, ZoneEdge);
        DrawArc(sb, l, _bobber - half, _bobber + half, l.Radius - 9f, 3f, ZoneEdge);

        // Fishing line from the rod tip to the bobber in the middle of the zone.
        var bobberPos = ArcPoint(l, _bobber, l.Radius);
        DrawPrimitives.DrawLine(sb, l.Pivot, bobberPos, LineColor * 0.85f, 1.5f);

        // Fish riding the arc, tilted along it.
        var fishPos = ArcPoint(l, _fish, l.Radius) + new Vector2(0, MathF.Sin(_time * 9f) * 1.5f);
        DrawFish(sb, fishPos, _fishInZone);
        DrawBobber(sb, bobberPos);

        // Catch meter: an inner arc that fills from the left.
        var meterR = l.Radius - 34f;
        DrawArc(sb, l, 0f, 1f, meterR, 7f, MeterTrack * 0.85f);
        if (_progress > 0.001f)
            DrawArc(sb, l, 0f, _progress, meterR, 4f, _progress < 0.25f ? FarmRpgUi.Danger : MeterFill);
        if (_grace > 0f)
            DrawArc(sb, l, 0f, 1f, meterR + 7f, 2f, LineColor * (_grace / GraceSeconds));
    }

    private void DrawResult(SpriteBatch sb, SpriteFont font, ArcLayout l, bool success)
    {
        var center = new Vector2(l.Pond.Center.X, l.Pivot.Y + l.Radius * 0.55f);
        if (success)
        {
            // Little splash of sparkles around the catch.
            for (var i = 0; i < 8; i++)
            {
                var a = i / 8f * MathF.Tau + _time * 2f;
                var r = 34f + MathF.Sin(_time * 6f + i) * 4f;
                DrawPrimitives.FillCircle(sb, center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r, 2.5f, MeterFill, 8);
            }
            DrawFish(sb, center, true, 2.2f);
        }

        var text = success ? "Nice catch!" : "It slipped the hook. Try again!";
        var box = new Rectangle(l.Pond.X + 30, (int)l.Pond.Bottom - 50, l.Pond.Width - 60, 36);
        FarmRpgUi.DrawCompactPanel(sb, box);
        DrawCenteredText(sb, font, text, box, success ? FarmRpgUi.Positive : FarmRpgUi.Rust, 0.9f);
    }

    private void DrawFish(SpriteBatch sb, Vector2 pos, bool happy, float scale = 1f)
    {
        var size = (int)(26 * scale);
        var rect = new Rectangle((int)(pos.X - size / 2f), (int)(pos.Y - size / 2f), size, size);
        if (happy)
            DrawPrimitives.FillCircle(sb, pos, size * 0.62f, ZoneEdge * 0.35f, 20);
        if (_fishIcon != null)
        {
            sb.Draw(_fishIcon, rect, Color.White);
            return;
        }
        // Fallback fish: body, tail and eye.
        DrawPrimitives.FillCircle(sb, pos, size * 0.32f, new Color(236, 150, 70), 16);
        DrawPrimitives.FillTriangle(sb,
            pos + new Vector2(size * 0.25f, 0),
            pos + new Vector2(size * 0.5f, -size * 0.22f),
            pos + new Vector2(size * 0.5f, size * 0.22f),
            new Color(214, 112, 52));
        DrawPrimitives.FillCircle(sb, pos + new Vector2(-size * 0.14f, -size * 0.05f), 2f * scale, Color.Black, 8);
    }

    private static void DrawBobber(SpriteBatch sb, Vector2 pos) => DrawBobber(sb, pos, 1f, 1f);

    /// <summary>Red and white float; <paramref name="scale"/> 1 is 7 px radius.</summary>
    public static void DrawBobber(SpriteBatch sb, Vector2 pos, float scale, float alpha)
    {
        DrawPrimitives.FillCircle(sb, pos, 7f * scale, new Color(250, 246, 236) * alpha, 16);
        DrawPrimitives.FillCircle(sb, pos + new Vector2(0, -2f * scale), 5f * scale, new Color(214, 52, 46) * alpha, 16);
        DrawPrimitives.DrawCircleOutline(sb, pos, 7f * scale, new Color(60, 30, 26) * alpha, 16, MathF.Max(1f, 1.5f * scale));
    }

    /// <summary>Thick arc between track positions <paramref name="from"/> and <paramref name="to"/>.</summary>
    private static void DrawArc(SpriteBatch sb, ArcLayout l, float from, float to, float radius, float thickness, Color color)
    {
        from = Math.Clamp(from, 0f, 1f);
        to = Math.Clamp(to, 0f, 1f);
        if (to <= from) return;
        var segments = Math.Max(2, (int)((to - from) * 64));
        var prev = ArcPoint(l, from, radius);
        for (var i = 1; i <= segments; i++)
        {
            var t = from + (to - from) * i / segments;
            var next = ArcPoint(l, t, radius);
            // Extend each segment a little so neighbours overlap and the arc has no cracks.
            var dir = next - prev;
            if (dir.LengthSquared() > 0.0001f) dir.Normalize();
            DrawPrimitives.DrawLine(sb, prev - dir * 0.8f, next + dir * 0.8f, color, thickness);
            prev = next;
        }
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

    /// <summary>Dev preview: put the minigame mid-fight so the UI can be rendered without input.</summary>
    internal void PreviewPlaying(float bobber, float fish, float progress)
    {
        _phase = Phase.Playing;
        _bobber = bobber;
        _fish = fish;
        _progress = progress;
        _grace = 0f;
        _fishInZone = MathF.Abs(fish - bobber) <= ZoneHalf;
    }
}
