using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Platform;

/// <summary>
/// MonoGame/SDL sets a single low-res <c>Icon.bmp</c> on the HWND; override with crisp PE/ICO sizes for the taskbar.
/// </summary>
internal static class GameWindowIcon
{
    private const int WmSetIcon = 0x0080;
    private const int IconSmall = 0;
    private const int IconBig = 1;

    private static readonly int[] IcoSizes = [16, 24, 32, 48, 64, 128, 256];

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lIcon);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint PrivateExtractIcons(
        string szFileName,
        int nIconIndex,
        int cxIcon,
        int cyIcon,
        IntPtr[]? phicon,
        uint[]? piconid,
        uint nIcons,
        uint flags);

    public static void Apply(Game game)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var hwnd = game.Window.Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var taskbarPx = TaskbarPixels(hwnd);
        var smallSize = PickIcoSize(taskbarPx);

        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath) &&
            exePath.EndsWith("Deathborn.Client.exe", StringComparison.OrdinalIgnoreCase) &&
            TrySetFromExe(hwnd, exePath, IconSmall, smallSize) &&
            TrySetFromExe(hwnd, exePath, IconBig, 256))
        {
            return;
        }

        var icoBytes = LoadEmbeddedIco();
        if (icoBytes is null)
            return;

        TrySetFromIco(hwnd, icoBytes, IconSmall, smallSize, 48, 32, 64, 24, 16);
        TrySetFromIco(hwnd, icoBytes, IconBig, 256, 128, 64, 48, 32);
    }

    private static int TaskbarPixels(IntPtr hwnd)
    {
        var dpi = GetDpiForWindow(hwnd);
        if (dpi == 0)
            dpi = 96;

        return (int)Math.Round(24.0 * dpi / 96.0);
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

    [SupportedOSPlatform("windows")]
    private static bool TrySetFromExe(IntPtr hwnd, string exePath, int slot, int size)
    {
        var icons = new IntPtr[1];
        var got = PrivateExtractIcons(exePath, 0, size, size, icons, null, 1, 0);
        if (got == 0 || icons[0] == IntPtr.Zero)
            return false;

        SendMessage(hwnd, WmSetIcon, (IntPtr)slot, icons[0]);
        DestroyIcon(icons[0]);
        return true;
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
            DestroyIcon(handle);
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);
}
