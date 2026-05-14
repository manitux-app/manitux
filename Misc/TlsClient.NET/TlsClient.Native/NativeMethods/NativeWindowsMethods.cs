using System;
using System.Runtime.InteropServices;

namespace TlsClient.Native.NativeMethods
{
    public static class NativeWindowsMethods
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

        public static IntPtr GetProcAddress(IntPtr hModule, string procName)
        {
            if (hModule == IntPtr.Zero || string.IsNullOrEmpty(procName))
                return IntPtr.Zero;

            try
            {
                return NativeLibrary.TryGetExport(hModule, procName, out var address)
                    ? address
                    : IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public static bool FreeLibrary(IntPtr hLibrary)
        {
            if (hLibrary == IntPtr.Zero) return false;

            try
            {
                NativeLibrary.Free(hLibrary);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
