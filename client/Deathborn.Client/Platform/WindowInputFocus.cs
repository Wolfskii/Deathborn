using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Platform;

/// <summary>
/// Reliable focus / cursor-over checks. MonoGame's <see cref="Game.IsActive"/> and
/// <c>Mouse.GetState()</c> can stay "inside" the client after focus moves to another window,
/// which would otherwise fire world click actions (e.g. sword swing).
/// </summary>
internal static class WindowInputFocus
{
    private const uint GaRoot = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct PointApi
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out PointApi lpPoint);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref PointApi lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(PointApi point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    /// <summary>True when this game window (or an owned child) is the Win32 foreground window.</summary>
    public static bool IsForeground(GameWindow window)
    {
        if (!OperatingSystem.IsWindows())
            return true;

        var hwnd = window.Handle;
        if (hwnd == IntPtr.Zero)
            return false;

        var fg = GetForegroundWindow();
        if (fg == IntPtr.Zero)
            return false;
        if (fg == hwnd)
            return true;

        // SDL may use a parent/child HWND tree for the same window.
        return SameRoot(fg, hwnd);
    }

    /// <summary>
    /// True when the OS cursor is over our client area and that pixel belongs to our window
    /// (not another overlapping window), and we hold foreground focus.
    /// </summary>
    public static bool IsPointerOverFocusedClient(GameWindow window, int clientWidth, int clientHeight)
    {
        if (!OperatingSystem.IsWindows())
            return true;

        var hwnd = window.Handle;
        if (hwnd == IntPtr.Zero || clientWidth <= 0 || clientHeight <= 0)
            return false;

        if (!IsForeground(window))
            return false;

        if (!GetCursorPos(out var screen))
            return false;

        var under = WindowFromPoint(screen);
        if (under == IntPtr.Zero || !SameRoot(under, hwnd))
            return false;

        var client = screen;
        if (!ScreenToClient(hwnd, ref client))
            return false;

        return client.X >= 0 && client.Y >= 0
            && client.X < clientWidth && client.Y < clientHeight;
    }

    private static bool SameRoot(IntPtr a, IntPtr b)
    {
        if (a == b) return true;
        var rootA = GetAncestor(a, GaRoot);
        var rootB = GetAncestor(b, GaRoot);
        if (rootA == IntPtr.Zero) rootA = a;
        if (rootB == IntPtr.Zero) rootB = b;
        return rootA == rootB || rootA == b || rootB == a;
    }
}
