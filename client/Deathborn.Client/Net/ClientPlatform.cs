namespace Deathborn.Client.Net;

public static class ClientPlatform
{
    public static string RuntimeId
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return "win-x64";
            if (OperatingSystem.IsLinux())
                return "linux-x64";
            if (OperatingSystem.IsMacOS())
                return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture
                       == System.Runtime.InteropServices.Architecture.Arm64
                    ? "osx-arm64"
                    : "osx-x64";
            return "win-x64";
        }
    }
}
