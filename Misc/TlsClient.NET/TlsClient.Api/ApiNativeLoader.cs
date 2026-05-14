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

        private static readonly string BaseArch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException("Unsupported process architecture")
        };

        public static string GetBinaryPath()
        {
            string platform = Platform;
            string arch = BaseArch;

            if (OperatingSystem.IsAndroid())
            {
                return "tlsclientapi";
            }
            else
            {
                return Path.GetFullPath($"helpers/{platform}-{arch}/tlsclientapi{Extension}");
            }
        }
    }
}
