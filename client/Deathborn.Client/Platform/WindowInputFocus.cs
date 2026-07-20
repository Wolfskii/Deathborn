using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Platform;

/// <summary>
/// Cursor-over-client checks. MonoGame's <c>Mouse.GetState()</c> can keep an in-window
/// position after the cursor leaves, so world clicks must also verify the OS hit-test
/// window. Do not require <c>GetForegroundWindow</c> — SDL's HWND often differs from the
/// foreground window even while the game is focused (that broke F12 / hotbar clicks).
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
    private static extern bool GetCursorPos(out PointApi lpPoint);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref PointApi lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(PointApi point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    /// <summary>
    /// True when the OS cursor is over our client area and that pixel belongs to our window
    /// (not another overlapping app).
    /// </summary>
    public static bool IsPointerOverClient(GameWindow window, int clientWidth, int clientHeight)
    {
        if (!OperatingSystem.IsWindows())
            return true;

        var hwnd = window.Handle;
        if (hwnd == IntPtr.Zero || clientWidth <= 0 || clientHeight <= 0)
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
