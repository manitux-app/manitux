using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TlsClient.Native.NativeMethods
{
    public static class NativeLinuxMethods
    {
        public static IntPtr LoadLibrary(string path)
        {
            if (string.IsNullOrEmpty(path)) return IntPtr.Zero;

            try
            {
                return NativeLibrary.Load(path);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public static int FreeLibrary(IntPtr hLibrary)
        {
            if (hLibrary == IntPtr.Zero) return -1;

            try
            {
                NativeLibrary.Free(hLibrary);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public static IntPtr GetProcAddress(IntPtr handle, string symbol)
        {
            if (handle == IntPtr.Zero || string.IsNullOrEmpty(symbol))
                return IntPtr.Zero;

            try
            {
                return NativeLibrary.TryGetExport(handle, symbol, out var address)
                    ? address
                    : IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
    
        public static string GetLinuxDistro()
        {
            const string osReleasePath = "/etc/os-release";

            if (File.Exists(osReleasePath))
            {
                var lines = File.ReadAllLines(osReleasePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ID="))
                    {
                        return line[3..].Trim('"').ToLower();
                    }
                }
            }

            return "UNKNOWN";
        }
    }
}
