using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Platform;

/// <summary>
/// Cursor-over-client checks. MonoGame's <c>Mouse.GetState()</c> can keep an in-window
/// position after the cursor leaves, so world clicks must also verify the OS hit-test
/// belongs to this process. Do not require <c>GetForegroundWindow</c> or HWND equality —
/// SDL's window handle often differs from the DXGI child <c>WindowFromPoint</c> returns
/// (that broke in-game hotbar / attack clicks).
/// </summary>
internal static class WindowInputFocus
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PointApi
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out PointApi lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(PointApi point);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// True when the OS cursor is over a window owned by this process (our game, not
    /// another app). Uses process id — robust against SDL/DXGI HWND nesting.
    /// </summary>
    public static bool IsPointerOverClient(GameWindow window, int clientWidth, int clientHeight)
    {
        _ = window;
        _ = clientWidth;
        _ = clientHeight;

        if (!OperatingSystem.IsWindows())
            return true;

        if (!GetCursorPos(out var screen))
            return false;

        var under = WindowFromPoint(screen);
        if (under == IntPtr.Zero)
            return false;

        GetWindowThreadProcessId(under, out var pid);
        return pid == (uint)Environment.ProcessId;
    }
}
