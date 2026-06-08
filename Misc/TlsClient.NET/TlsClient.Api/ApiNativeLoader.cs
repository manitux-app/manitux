using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TlsClient.Api.Native
{
    public class ApiNativeLoader
    {
        private static readonly string Platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                             RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                             RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" :
                             OperatingSystem.IsAndroid() ? "android" :
                             throw new PlatformNotSupportedException("Unsupported OS platform");

        private static readonly string Extension = Platform switch
        {
            "win" => ".exe",
            "linux" => "",
            "darwin" => "",
            "android" => "",
            _ => throw new PlatformNotSupportedException("Unsupported OS platform")
        };

        public static string GetBinaryPath()
        {
            if (OperatingSystem.IsAndroid())
            {
                return "tlsclientapi";
            }
            else
            {
                return Path.Combine(AppContext.BaseDirectory, "libs", "helpers", $"tlsclientapi{Extension}");
            }
        }
    }
}
