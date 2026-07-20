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

    private Phase _phase;
    private float _openRemaining;
    private long _pendingBuildingId;
    private Vector2? _frozenFeet;

    public bool IsBusy => _phase != Phase.Idle;

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
        _openRemaining = MathF.Max(0.05f, SfxPlayer.PlayDoorOpen());
        return true;
    }

    public bool TryBeginExit(long currentBuildingId, Vector2 interiorFeet)
    {
        if (IsBusy || currentBuildingId <= 0) return false;

        _phase = Phase.OpeningExit;
        _pendingBuildingId = currentBuildingId;
        _frozenFeet = interiorFeet;
        VisualInsideOverride = currentBuildingId;
        _openRemaining = MathF.Max(0.05f, SfxPlayer.PlayDoorOpen());
        return true;
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
                // Keep exterior until server confirms inside.
                return true;

            case Phase.AwaitEnter:
                if (serverInsideHouseId == _pendingBuildingId)
                    CompleteViewSwap();
                return false;

            case Phase.OpeningExit:
                _openRemaining -= dt;
                if (_openRemaining > 0f) return false;
                shouldSendExit = true;
                _phase = Phase.AwaitExit;
                return true;

            case Phase.AwaitExit:
                if (serverInsideHouseId <= 0)
                    CompleteViewSwap();
                return false;

            default:
                return false;
        }
    }

    /// <summary>True while we should hold the local feet at <see cref="FrozenFeet"/>.</summary>
    public bool ShouldFreezeFeet(long serverInsideHouseId) =>
        _phase is Phase.OpeningEnter or Phase.OpeningExit
        || (_phase == Phase.AwaitEnter && serverInsideHouseId != _pendingBuildingId)
        || (_phase == Phase.AwaitExit && serverInsideHouseId > 0);

    public void Cancel()
    {
        _phase = Phase.Idle;
        _openRemaining = 0f;
        _pendingBuildingId = 0;
        VisualInsideOverride = null;
        _frozenFeet = null;
    }

    private void CompleteViewSwap()
    {
        VisualInsideOverride = null;
        _frozenFeet = null;
        _pendingBuildingId = 0;
        _phase = Phase.Idle;
        SfxPlayer.PlayDoorClose();
    }
}
