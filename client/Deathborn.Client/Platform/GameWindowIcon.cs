using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;

namespace Deathborn.Client.Platform;

/// <summary>
/// Fallback when the game is hosted by <c>dotnet exec</c> — set a DPI-sized ICO on the HWND.
/// Native <c>Deathborn.Client.exe</c> dev runs use the PE icon instead and skip this.
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

    public static void Apply(Game game)
    {
        if (!OperatingSystem.IsWindows())
            return;

        // Hosted by dotnet.exe — the apphost PE icon is not used; patch the window icon.
        if (string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, "Deathborn.Client", StringComparison.Ordinal))
            return;

        var handle = game.Window.Handle;
        if (handle == IntPtr.Zero)
            return;

        var icoBytes = LoadEmbeddedIco();
        if (icoBytes is null)
            return;

        var taskbarSize = PickIcoSize(TaskbarPixels(handle));
        TrySetIcon(handle, icoBytes, IconSmall, taskbarSize, 48, 32, 64, 24, 16);
        TrySetIcon(handle, icoBytes, IconBig, 256, 128, 64, 48, 32);
    }

    private static int TaskbarPixels(IntPtr hwnd)
    {
        var dpi = GetDpiForWindow(hwnd);
        if (dpi == 0)
            dpi = 96;

        // Win11 taskbar uses a 24×24 logical icon; scale to physical pixels.
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
    private static void TrySetIcon(IntPtr hwnd, byte[] icoBytes, int slot, params int[] sizes)
    {
        foreach (var size in sizes)
        {
            if (!TryCreateIcon(icoBytes, size, out var handle))
                continue;

            SendMessage(hwnd, WmSetIcon, (IntPtr)slot, handle);
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
            handle = icon.Handle;
            handle = CopyIcon(handle);
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
