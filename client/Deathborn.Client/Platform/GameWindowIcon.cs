using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Platform;

/// <summary>
/// MonoGame/SDL sets a single <c>Icon.bmp</c> on the HWND; override it with the ICO frames that
/// match the taskbar and title bar sizes at the window's DPI, so Windows never rescales them.
/// </summary>
internal static class GameWindowIcon
{
    private const int WmSetIcon = 0x0080;
    private const int IconSmall = 0;
    private const int IconBig = 1;

    private static readonly int[] IcoSizes = [16, 24, 32, 48, 64, 128, 256];
    private static IntPtr _smallIcon;
    private static IntPtr _largeIcon;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lIcon);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    public static void Apply(Game game)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var hwnd = game.Window.Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var icoBytes = LoadEmbeddedIco();
            if (icoBytes is null)
                return;

            var dpi = WindowDpi(hwnd);
            var taskbarSize = PickIcoSize((int)Math.Round(TaskbarIconPixels * dpi / 96.0));
            var titleBarSize = PickIcoSize((int)Math.Round(TitleBarIconPixels * dpi / 96.0));

            // The taskbar draws the big icon. Hand it the frame that already matches its
            // pixel size; a 256px frame gets shrunk by Windows and comes out blurry.
            TrySetFromIco(hwnd, icoBytes, IconBig, taskbarSize, 32, 24, 48, 16);
            TrySetFromIco(hwnd, icoBytes, IconSmall, titleBarSize, 16, 24, 32);
        }
        catch
        {
            // Icon overrides are best-effort — never take down the game.
        }
    }

    /// <summary>Taskbar button icon size at 96 DPI (Windows 10/11).</summary>
    private const double TaskbarIconPixels = 24.0;
    /// <summary>Title bar icon size at 96 DPI.</summary>
    private const double TitleBarIconPixels = 16.0;

    private static uint WindowDpi(IntPtr hwnd)
    {
        var dpi = GetDpiForWindow(hwnd);
        return dpi == 0 ? 96u : dpi;
    }

    private static int PickIcoSize(int targetPixels)
    {
        var best = IcoSizes[0];
        var bestDelta = Math.Abs(best - targetPixels);
        foreach (var size in IcoSizes)
        {
            var delta = Math.Abs(size - targetPixels);
            if (delta < bestDelta)
            {
                best = size;
                bestDelta = delta;
            }
        }

        return best;
    }

    private static byte[]? LoadEmbeddedIco()
    {
        var assembly = typeof(DeathbornGame).Assembly;
        using var stream =
            assembly.GetManifestResourceStream("Icon.ico") ??
            assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Icon.ico");
        if (stream is null)
            return null;

        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        return bytes;
    }

    [SupportedOSPlatform("windows")]
    private static void TrySetFromIco(IntPtr hwnd, byte[] icoBytes, int slot, params int[] sizes)
    {
        foreach (var size in sizes)
        {
            if (!TryCreateIcon(icoBytes, size, out var handle))
                continue;

            SendMessage(hwnd, WmSetIcon, (IntPtr)slot, handle);
            if (slot == IconSmall)
                _smallIcon = handle;
            else
                _largeIcon = handle;
            return;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryCreateIcon(byte[] icoBytes, int size, out IntPtr handle)
    {
        handle = IntPtr.Zero;
        try
        {
            using var stream = new MemoryStream(icoBytes, writable: false);
            using var icon = new System.Drawing.Icon(stream, size, size);
            handle = CopyIcon(icon.Handle);
            return handle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }
}
