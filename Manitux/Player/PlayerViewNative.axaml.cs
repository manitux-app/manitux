using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LibMPVSharp;
using Manitux.Core.Models;

namespace Manitux.Player;

public partial class PlayerViewNative : UserControl //, IDisposable
{
    public MPVMediaPlayer? Player { get; set; } = new();
    
    public PlayerViewNative()
    {
        //InitializeComponent();

        //VideoView.MediaPlayer = Player;

        //this.Loaded += (_, _) =>
        //{
        //    TestPlay();
        //    //Play(videoSource);
        //};
    }

    //private async void TestPlay()
    //{
    //    if (VideoView.MediaPlayer is null) return;

    //    VideoView.MediaPlayer.EnsureRenderContextCreated();

    //    //VideoView.MediaPlayer.ExecuteCommand(new[] { "set", "mute", "yes" }); // sessiz

    //    string uriString = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json"; //"https://test-videos.co.uk/vids/bigbuckbunny/mp4/av1/360/Big_Buck_Bunny_360_10s_1MB.mp4";
    //    string track = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt";

    //    // http headers önce set edilecek, sonra video url sonra altyazýlar eklenecek

    //    // User-Agent (Kullanýcý Kimliði) Tanýmlama
    //    //VideoView.MediaPlayer.SetProperty("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

    //    // Referer (Yönlendiren Sayfa) Tanýmlama
    //    //VideoView.MediaPlayer.SetProperty("referrer", "https://ornek-site.com");

    //    //VideoView.MediaPlayer.SetProperty("http-header-fields", "X-Custom-Header: Deðer, Authorization: Bearer Token");

    //    await VideoView.MediaPlayer.ExecuteCommandAsync([MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, uriString]);

    //    // 1. Altyazý dosyasýný video oynatýcýya ekler ve hemen seçer.
    //    VideoView.MediaPlayer.ExecuteCommand(new[] { "sub-add", track });

    //    // 2. Altyazýyý ekler ancak varsayýlan olarak hemen etkinleþtirmek istemiyorsanýz auto/select/cached bayraklarý kullanýlabilir:
    //    //VideoView.MediaPlayer.ExecuteCommand(new[] { "sub-add", "altyazi_dosyasi_yolu.srt", "select" });

    //    // Altyazýyý tamamen kapatmak için:
    //    //VideoView.MediaPlayer.SetProperty("sid", "no");

    //    // Ýlk altyazý kanalýna (ID: 1) geçiþ yapmak için:
    //    //VideoView.MediaPlayer.SetProperty("sid", "1");

    //    VideoView.MediaPlayer.MpvEvent += (sender, e) =>
    //    {
    //        switch (e.event_id)
    //        {
    //            case MpvEventId.MPV_EVENT_START_FILE:
    //                break;

    //            case MpvEventId.MPV_EVENT_FILE_LOADED:
    //                break;

    //            case MpvEventId.MPV_EVENT_END_FILE:
    //                break;

    //            case MpvEventId.MPV_EVENT_PROPERTY_CHANGE:
    //                break;

    //            case MpvEventId.MPV_EVENT_SEEK:
    //                break;
    //        }
            
            
    //    };
    //}

    //private void Play(VideoSourceModel videoSource)
    //{
    //    if (VideoView.MediaPlayer is null) return;

    //    VideoView.MediaPlayer.EnsureRenderContextCreated();

    //    string args = GenerateArgs(videoSource);

    //    VideoView.MediaPlayer.ExecuteCommandString(args);

    //    //VideoView.MediaPlayer.ExecuteCommand(new[] { "set", "mute", "yes" }); // sessiz
    //}

    //private string GenerateArgs(VideoSourceModel source)
    //{
    //    string mpvCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mpv.exe" : "mpv";

    //    var args = new List<string>();

    //    args.Add($"\"{source.Url}\"");

    //    if (source.Headers != null && source.Headers.Any())
    //    {
    //        var headerList = source.Headers.Select(h => $"{h.Name}: {h.Value}").ToList();

    //        string allHeaders = string.Join(",", headerList);
    //        args.Add($"--http-header-fields=\"{allHeaders}\"");

    //        var ua = source.Headers.FirstOrDefault(h => h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
    //        if (ua != null) args.Add($"--user-agent=\"{ua.Value}\"");
    //    }

    //    if (!string.IsNullOrWhiteSpace(source.Referer))
    //    {
    //        args.Add($"--referrer=\"{source.Referer}\"");
    //    }

    //    if (source.Subtitles != null && source.Subtitles.Any())
    //    {
    //        foreach (var sub in source.Subtitles)
    //        {
    //            if (!string.IsNullOrWhiteSpace(sub.Url))
    //            {
    //                args.Add($"--sub-file=\"{sub.Url}\"");
    //            }
    //        }

    //        args.Add("--demuxer-max-bytes=100M");
    //        args.Add("--sub-auto=all");          
    //    }

    //    //args.Add("--extractor-args \"generic:impersonate\"");

    //    string commands = string.Join(" ", args.Select(s => s));
    //    return commands;
    //}

    //public void TogglePause()
    //{
    //    // Videoyu duraklatýr (Pause)
    //    VideoView.MediaPlayer.SetProperty("pause", "yes");

    //    // Videoyu devam ettirir (Resume)
    //    VideoView.MediaPlayer.SetProperty("pause", "no");

    //    // Durumu tersine çevirir (Oynatýlýyorsa duraklatýr, duraklatýldýysa devam ettirir)
    //    VideoView.MediaPlayer.ExecuteCommand(new[] { "cycle", "pause" });
    //}

    //public void Stop()
    //{
    //    // Videoyu tamamen durdurur
    //    VideoView.MediaPlayer.ExecuteCommand(new[] { "stop" });

    //}

    //private void SubtitleSize()
    //{
    //    // Altyazý boyutunu doðrudan belirli bir orana eþitlemek için kullanýlýr. Varsayýlan deðer 1.0 dýr
    //    // Boyutu %20 büyütür (Varsayýlanýn 1.2 katý)
    //    VideoView.MediaPlayer.SetProperty("sub-scale", "1.2");

    //    // Boyutu %20 küçültür (Varsayýlanýn 0.8 katý)
    //    VideoView.MediaPlayer.SetProperty("sub-scale", "0.8");

    //    // (dinamik) Uygulamadaki "+" ve "-" butonlarýna basýldýðýnda altyazý boyutunu mevcut deðer üzerinden artýrmak veya azaltmak için add komutu çalýþtýrýlýr
    //    // Altyazý boyutunu mevcut boyuta göre %10 artýrýr
    //    VideoView.MediaPlayer.ExecuteCommand(new[] { "add", "sub-scale", "0.1" });

    //    // Altyazý boyutunu mevcut boyuta göre %10 azaltýr
    //    VideoView.MediaPlayer.ExecuteCommand(new[] { "add", "sub-scale", "-0.1" });

    //    // Altyazý yazý tipi boyutunu 45 piksel yapar (sadece .srt için)
    //    VideoView.MediaPlayer.SetProperty("sub-font-size", "45");

    //}

    //protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    //{
    //    base.OnDetachedFromVisualTree(e);
    //    Dispose();
    //}

    //protected bool disposed = false;
    //protected virtual void Dispose(bool disposing)
    //{
    //    if (!this.disposed && disposing)
    //    {
    //        //Player?.ExecuteCommand(new[] {"stop"});
    //        //Player?.Dispose();
    //        //Player = null;
    //        Debug.WriteLine("VideoPlayer Dispose");
    //    }

    //    this.disposed = true;
    //}

    //public void Dispose()
    //{
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //    GC.Collect();
    //}
}