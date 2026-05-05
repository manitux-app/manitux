using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Manitux.Core.Models;

namespace Manitux.Player;

public class ExternalPlayerManager
{
    public void Play(int playerNumber, VideoSourceModel source)
    {
        switch (playerNumber)
        {
            case 1:
                MpvPlay(source);
                break;
            case 2:
                VlcPlay(source);
                break;
        }
    }

    public void MpvPlay(VideoSourceModel source)
    {
        // 1. Null ve Boş Değer Kontrolü
        if (source == null || string.IsNullOrWhiteSpace(source.Url))
        {
            throw new ArgumentException("Video kaynağı veya URL'si geçersiz.");
        }

        string mpvCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mpv.exe" : "mpv";

        // Argümanları güvenli bir listede toplayalım
        var args = new List<string>();

        // Temel Video URL (Boşluklara karşı tırnak içinde)
        args.Add($"\"{source.Url}\"");

        // --- HTTP Headers & Referer ---
        if (source.Headers != null && source.Headers.Any())
        {
            var headerList = source.Headers.Select(h => $"{h.Name}: {h.Value}").ToList();

            // mpv --http-header-fields için virgülle ayrılmış liste bekler
            string allHeaders = string.Join(",", headerList);
            args.Add($"--http-header-fields=\"{allHeaders}\"");

            // User-Agent'ı listeden bulup ayrıca belirtmek uyumluluğu artırır
            var ua = source.Headers.FirstOrDefault(h => h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
            if (ua != null) args.Add($"--user-agent=\"{ua.Value}\"");
        }

        if (!string.IsNullOrWhiteSpace(source.Referer))
        {
            args.Add($"--referrer=\"{source.Referer}\"");
        }

        // --- İnternet Altyazıları (Remote Subtitles) ---
        if (source.Subtitles != null && source.Subtitles.Any())
        {
            // İnternet URL'leri söz konusu olduğunda ayırıcı (separator) 
            // işletim sisteminden bağımsız olarak genellikle düzgün çalışır 
            // ancak yine de platform standartlarını korumak en iyisidir.
            //string sep = OperatingSystem.IsWindows()  ? ";" : ":";

            // Her bir altyazı URL'sini alıyoruz
            //string subUrls = string.Join(sep, source.Subtitles.Select(s => s.Url));

            foreach (var sub in source.Subtitles)
            {
                if (!string.IsNullOrWhiteSpace(sub.Url))
                {
                    // HATA ÇÖZÜMÜ: --sub-files yerine her altyazı için --sub-file (tekil) kullanıyoruz.
                    // Bu sayede işletim sistemine özgü ayırıcı (; veya :) karmaşasından kurtuluyoruz.
                    args.Add($"--sub-file=\"{sub.Url}\"");
                }
            }

            //args.Add($"--sub-files=\"{subUrls}\"");

            // ÖNEMLİ: İnternetten altyazı yüklerken mpv'nin bazen 
            // bunları otomatik seçmesi için şu ek argümanlar hayat kurtarır:
            args.Add("--demuxer-max-bytes=100M"); // Buffer artırma (opsiyonel)
            args.Add("--sub-auto=all");           // Tüm altyazıları algıla
        }

        //args.Add("--extractor-args \"generic:impersonate\"");

        string commands = string.Join(" ", args.Select(s => s));
        Debug.WriteLine("mpv " + commands);

        // --- Başlatma Ayarları ---
        var startInfo = new ProcessStartInfo
        {
            FileName = mpvCmd,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            CreateNoWindow = true,
            // Hata ayıklama gerekirse çıktıları yönlendirebilirsiniz
            RedirectStandardError = false,
            RedirectStandardOutput = false
        };

        try
        {
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            // Kullanıcıya mpv'nin yüklü olmadığını veya erişilemediğini bildirin
            Debug.WriteLine($"Mpv Hata: {ex.Message}");
        }
    }

    public void VlcPlay(VideoSourceModel source)
    {
        // 1. Null ve Boş Değer Kontrolü
        if (source == null || string.IsNullOrWhiteSpace(source.Url))
        {
            throw new ArgumentException("Video kaynağı veya URL'si geçersiz.");
        }

        // 2. Platforma göre VLC komutunu belirle
        // Not: Windows'ta VLC genelde PATH'te olmaz. Eğer çalışmazsa tam yol gerekebilir.
        string vlcCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "vlc.exe" : "vlc";

        var args = new List<string>();

        // 1. ANA VİDEO URL'Sİ
        // VLC'nin bunu bir dosya sanmaması için başına hiçbir ek koymadan, 
        // sadece tırnak içinde en başa ekliyoruz.
        args.Add($"\"{source.Url}\"");

        // 2. HTTP HEADERS & REFERER
        if (source.Headers != null && source.Headers.Any())
        {
            foreach (var header in source.Headers)
            {
                // VLC'de boşluk içeren headerlar için tırnak kullanımı çok kritiktir
                args.Add($":http-header-fields=\"{header.Name}: {header.Value}\"");
            }
        }

        if (!string.IsNullOrWhiteSpace(source.Referer))
        {
            args.Add($":http-referrer=\"{source.Referer}\"");
        }

        // 3. ALTYAZI
        if (source.Subtitles != null)
        {
            foreach (var sub in source.Subtitles)
            {
                if (!string.IsNullOrWhiteSpace(sub.Url))
                {
                    args.Add($":input-slave=\"{sub.Url}\"");
                    // Veya alternatif olarak:
                    // args.Add($":sub-file=\"{sub.Url}\"");
                }
            }
        }

        //args.Add("--no-video-title-show");
        args.Add("--fullscreen");
        args.Add("--play-and-exit");


        string commands = string.Join(" ", args.Select(s => s));
        Debug.WriteLine("vlc " + commands);

        // 4. SİSTEME GÖRE BAŞLATMA
        var startInfo = new ProcessStartInfo
        {
            FileName = vlcCmd,
            Arguments = string.Join(" ", args), // Argümanları birleştir
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\" : "/"
        };


        try
        {
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"VLC Hata: {ex.Message}");
        }
    }
}
