using System;
using System.Runtime.InteropServices;

namespace TlsClient.Native.NativeMethods
{
    public static class NativeAndroidMethods
    {
        /// <summary>
        /// Loads the native library using the specified path. 
        /// NativeLibrary.Load abstracts the dlopen (Linux/Android) or LoadLibrary (Windows) call. 
        /// </summary>
        /// <returns>Library handle (in case of IntPtr.Zero failure)</returns>
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

        /// <summary>
        /// Releases the loaded library. 
        /// </summary>
        /// <returns>Returns 0 if successful, -1 if handle is invalid.</returns>
        public static int FreeLibrary(IntPtr hLibrary)
        {
            if (hLibrary == IntPtr.Zero) return -1;

            try
            {
                NativeLibrary.Free(hLibrary);
                return 0; // `dlclose()` returns 0 on success.
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Finds the address (symbol) of the function in the library. 
        /// </summary>
        /// <returns>Memory address of the function</returns>
        public static IntPtr GetProcAddress(IntPtr handle, string symbol)
        {
            if (handle == IntPtr.Zero || string.IsNullOrEmpty(symbol))
                return IntPtr.Zero;

            try
            {
                if (NativeLibrary.TryGetExport(handle, symbol, out IntPtr address))
                {
                    return address;
                }
            }
            catch
            {
                
            }

            return IntPtr.Zero;
        }

        // Note: The NativeLibrary class does not directly accept flags (Lazy, Global, etc.) as parameters.
        // The .NET Runtime uses the most secure and performant defaults (usually RTLD_NOW | RTLD_GLOBAL) for each platform.
    }
}