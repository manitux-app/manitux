using System;
using System.Runtime.InteropServices;

namespace TlsClient.Native.NativeMethods
{
    public static class NativeAndroidMethods
    {
        /// <summary>
        /// Belirtilen yolu kullanarak native kütüphaneyi yükler.
        /// NativeLibrary.Load, dlopen (Linux/Android) veya LoadLibrary (Windows) çağrısını soyutlar.
        /// </summary>
        /// <returns>Kütüphane handle'ı (IntPtr.Zero başarısızlık durumunda)</returns>
        public static IntPtr LoadLibrary(string path)
        {
            if (string.IsNullOrEmpty(path)) return IntPtr.Zero;

            try
            {
                // NativeLibrary.Load başarısız olursa exception fırlatır, 
                // bu yüzden orijinal koda sadık kalarak hata durumunda Zero dönüyoruz.
                return NativeLibrary.Load(path);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Yüklenen kütüphaneyi serbest bırakır.
        /// </summary>
        /// <returns>Başarı durumunda 0, handle geçersizse -1 döndürür.</returns>
        public static int FreeLibrary(IntPtr hLibrary)
        {
            if (hLibrary == IntPtr.Zero) return -1;

            try
            {
                NativeLibrary.Free(hLibrary);
                return 0; // dlclose() başarı durumunda 0 döner.
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Kütüphane içindeki fonksiyonun (sembol) adresini bulur.
        /// </summary>
        /// <returns>Fonksiyonun bellek adresi</returns>
        public static IntPtr GetProcAddress(IntPtr handle, string symbol)
        {
            if (handle == IntPtr.Zero || string.IsNullOrEmpty(symbol))
                return IntPtr.Zero;

            try
            {
                // TryGetExport kullanarak exception maliyetinden kaçınabiliriz.
                if (NativeLibrary.TryGetExport(handle, symbol, out IntPtr address))
                {
                    return address;
                }
            }
            catch
            {
                // Bazı platformlarda TryGetExport bile kritik hatalarda exception fırlatabilir.
            }

            return IntPtr.Zero;
        }

        // Not: NativeLibrary sınıfı flags (Lazy, Global vb.) parametrelerini doğrudan almaz. 
        // .NET Runtime, her platform için en güvenli ve performanslı varsayılanları (genellikle RTLD_NOW | RTLD_GLOBAL) kullanır.
    }
}