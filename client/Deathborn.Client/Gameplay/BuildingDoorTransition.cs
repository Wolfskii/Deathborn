using Microsoft.Xna.Framework;
using Deathborn.Client.Audio;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Door open → wait for SFX → swap building view → door close.
/// Works for player houses now; reusable for future building interiors.
/// </summary>
public sealed class BuildingDoorTransition
{
    private enum Phase
    {
        Idle,
        OpeningEnter,
        AwaitEnter,
        OpeningExit,
        AwaitExit,
    }

    /// <summary>
    /// How long to wait for the server to confirm an enter or exit. If it never does (the
    /// server rejected the request), the transition is cancelled instead of locking the player.
    /// </summary>
    private const float AwaitTimeoutSeconds = 2f;

    /// <summary>
    /// After a transition ends, walking cannot start another one for this long (matches the
    /// server's house transition cooldown). Without it, holding a direction at the door
    /// re-triggered a failed transition every frame and froze the player in place.
    /// </summary>
    private const float AutoRetriggerCooldownSeconds = 1.1f;

    private Phase _phase;
    private float _openRemaining;
    private float _awaitRemaining;
    private long _pendingBuildingId;
    private Vector2? _frozenFeet;
    private bool _timedOut;
    private float _autoCooldown;

    public bool IsBusy => _phase != Phase.Idle;

    /// <summary>True when walking into a door may start a transition.</summary>
    public bool CanAutoStart => !IsBusy && _autoCooldown <= 0f;

    public void TickCooldown(float dt)
    {
        if (_autoCooldown > 0f)
            _autoCooldown = MathF.Max(0f, _autoCooldown - dt);
    }

    /// <summary>
    /// When set, local camera / interior rendering uses this house id instead of the server value
    /// (0 = force exterior during enter open, >0 = force interior during exit open).
    /// </summary>
    public long? VisualInsideOverride { get; private set; }

    public Vector2? FrozenFeet => _frozenFeet;

    public long PendingBuildingId => _pendingBuildingId;

    public bool TryBeginEnter(long buildingId, Vector2 exteriorFeet)
    {
        if (IsBusy || buildingId <= 0) return false;

        _phase = Phase.OpeningEnter;
        _pendingBuildingId = buildingId;
        _frozenFeet = exteriorFeet;
        VisualInsideOverride = 0;
        _openRemaining = MathF.Max(0.15f, SfxPlayer.PlayDoorOpen());
        return true;
    }

    public bool TryBeginExit(long currentBuildingId, Vector2 interiorFeet)
    {
        if (IsBusy || currentBuildingId <= 0) return false;

        _phase = Phase.OpeningExit;
        _pendingBuildingId = currentBuildingId;
        _frozenFeet = interiorFeet;
        VisualInsideOverride = currentBuildingId;
        _openRemaining = MathF.Max(0.15f, SfxPlayer.PlayDoorOpen());
        return true;
    }

    /// <summary>
    /// Server already moved us in/out (auto door) before the client started a transition.
    /// Still play open + close so the cue is never silent.
    /// </summary>
    public void PlayCatchUpSfx(bool entered)
    {
        if (IsBusy) return;
        _ = entered;
        SfxPlayer.PlayDoorOpen();
        SfxPlayer.PlayDoorClose();
    }

    /// <summary>
    /// Advance timers. Returns true when the network enter/exit should be sent.
    /// </summary>
    public bool Update(float dt, long serverInsideHouseId, out bool shouldSendEnter, out bool shouldSendExit)
    {
        shouldSendEnter = false;
        shouldSendExit = false;

        switch (_phase)
        {
            case Phase.OpeningEnter:
                _openRemaining -= dt;
                if (_openRemaining > 0f) return false;
                shouldSendEnter = true;
                _phase = Phase.AwaitEnter;
                _awaitRemaining = AwaitTimeoutSeconds;
                return true;

            case Phase.AwaitEnter:
                // Server may have auto-entered during the open SFX — treat as confirmed.
                if (serverInsideHouseId == _pendingBuildingId || serverInsideHouseId > 0)
                {
                    CompleteViewSwap();
                    return false;
                }
                return TickAwait(dt);

            case Phase.OpeningExit:
                _openRemaining -= dt;
                if (_openRemaining > 0f) return false;
                shouldSendExit = true;
                _phase = Phase.AwaitExit;
                _awaitRemaining = AwaitTimeoutSeconds;
                return true;

            case Phase.AwaitExit:
                if (serverInsideHouseId <= 0)
                {
                    CompleteViewSwap();
                    return false;
                }
                return TickAwait(dt);

            default:
                return false;
        }
    }

    /// <summary>True while we should hold the local feet at <see cref="FrozenFeet"/>.</summary>
    public bool ShouldFreezeFeet(long serverInsideHouseId) =>
        _phase is Phase.OpeningEnter or Phase.OpeningExit
        || (_phase == Phase.AwaitEnter && serverInsideHouseId <= 0)
        || (_phase == Phase.AwaitExit && serverInsideHouseId > 0);

    /// <summary>Returns true once after a transition was cancelled because the server never confirmed it.</summary>
    public bool ConsumeTimeout()
    {
        var timedOut = _timedOut;
        _timedOut = false;
        return timedOut;
    }

    public void Cancel()
    {
        _phase = Phase.Idle;
        _openRemaining = 0f;
        _awaitRemaining = 0f;
        _pendingBuildingId = 0;
        VisualInsideOverride = null;
        _frozenFeet = null;
    }

    /// <summary>Counts down the wait for the server. Returns false (and cancels) when it runs out.</summary>
    private bool TickAwait(float dt)
    {
        _awaitRemaining -= dt;
        if (_awaitRemaining > 0f) return false;
        _timedOut = true;
        Cancel();
        _autoCooldown = AutoRetriggerCooldownSeconds;
        return false;
    }

    private void CompleteViewSwap()
    {
        VisualInsideOverride = null;
        _frozenFeet = null;
        _pendingBuildingId = 0;
        _phase = Phase.Idle;
        _autoCooldown = AutoRetriggerCooldownSeconds;
        SfxPlayer.PlayDoorClose();
    }
}
