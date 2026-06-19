using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TlsClient.Native.NativeMethods;

namespace TlsClient.Native
{
    public class NativeLoader
    {
        private static readonly string Platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                             RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                             RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" :
                             OperatingSystem.IsAndroid() ? "android" :
                             throw new PlatformNotSupportedException("Unsupported OS platform");

        private static readonly string Extension = Platform switch
        {
            "win" => ".dll",
            "linux" => ".so",
            "darwin" => ".dylib",
            "android" => ".so",
            _ => throw new PlatformNotSupportedException("Unsupported OS platform")
        };

        public static string GetLibraryPath()
        {
            if (OperatingSystem.IsAndroid())
            {
                return "tlsclient";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "Frameworks",
                    GetLibraryFileName()));
            }
            else
            {
                return Path.Combine(AppContext.BaseDirectory, "libs", GetLibraryFileName());
            }
        }

        private static string GetLibraryFileName()
        {
            return Platform switch
            {
                "win" => "tlsclient.dll",
                "linux" => "libtlsclient.so",
                "darwin" => "libtlsclient.dylib",
                _ => $"tlsclient{Extension}"
            };
        }

        public static IntPtr LoadNativeAssembly(string libraryPath)
        {
           
            if (!OperatingSystem.IsAndroid())
            {
                if (!File.Exists(libraryPath))
                {
                    throw new DllNotFoundException($"The native library '{libraryPath}' was not found.");
                }
            }

            if (Platform == "win")
            {
                return NativeWindowsMethods.LoadLibrary(libraryPath);
            }
            else if (Platform == "linux" || Platform == "linux-ubuntu" || Platform == "linux-alpine")
            {
                return NativeLinuxMethods.LoadLibrary(libraryPath);
            }
            else if (Platform == "darwin")
            {
                return NativeDarwinMethods.LoadLibrary(libraryPath);
            }
            else if (Platform == "android")
            {
                return NativeAndroidMethods.LoadLibrary(libraryPath);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS platform");
            }

        }


        public static bool FreeNativeAssembly(IntPtr libraryHandle)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? NativeWindowsMethods.FreeLibrary(libraryHandle) :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? NativeLinuxMethods.FreeLibrary(libraryHandle) == 0 :
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? NativeDarwinMethods.FreeLibrary(libraryHandle) == 0 :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")) ? NativeAndroidMethods.FreeLibrary(libraryHandle) == 0:
                   throw new PlatformNotSupportedException("Unsupported OS platform");
        }


        public static IntPtr GetProcAddress(IntPtr handle, string name)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? NativeWindowsMethods.GetProcAddress(handle, name) :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? NativeLinuxMethods.GetProcAddress(handle, name) :
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? NativeDarwinMethods.GetProcAddress(handle, name) :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")) ? NativeAndroidMethods.GetProcAddress(handle, name) :
                   throw new PlatformNotSupportedException("Unsupported OS platform");
        }
    }
}
