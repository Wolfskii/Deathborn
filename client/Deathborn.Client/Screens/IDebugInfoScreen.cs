namespace Deathborn.Client.Screens;

/// <summary>Screens that expose live debug/status lines for the HUD and Esc menu.</summary>
public interface IDebugInfoScreen
{
    IReadOnlyList<string> DebugInfoLines { get; }
}
