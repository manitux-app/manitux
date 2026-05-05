using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibMPVSharp;
using LibMPVSharp.Avalonia;
using LibMPVSharp.Extensions;
using Manitux.Core.Models;

namespace Manitux.ViewModels
{
    public partial class PlayerViewModel : ViewModelBase, IDisposable
    {
        [ObservableProperty]
        private MPVMediaPlayer? _mediaPlayer;

        //[ObservableProperty]
        //private VideoView _videoView;

        public bool IsReady { get; set; } = false;
        public bool HasError { get; set; }
        public string? ErrorString { get; set; }

        public event Action? OnRequestClose;

        public PlayerViewModel(VideoSourceModel? videoSource)
        {
            if (videoSource is null || string.IsNullOrEmpty(videoSource?.Url) || !IsValidUrlFormat(videoSource?.Url ?? ""))
            {
                ErrorString = "VideoSource is not initialized!";
                HasError = true;
                return;
            }

            //var opt = new MPVMediaPlayerOptions();
            MediaPlayer = new MPVMediaPlayer();

            MediaPlayer.MpvEvent += (sender, e) =>
                {
                    switch (e.event_id)
                    {
                        case MpvEventId.MPV_EVENT_FILE_LOADED:
                            Debug.WriteLine("MPV_EVENT_FILE_LOADED");
                            Dispatcher.UIThread.Post(() =>
                            {
                                IsReady = true;
                                OnPropertyChanged(nameof(IsReady));
                            });
                            break;

                        case MpvEventId.MPV_EVENT_END_FILE:
                            Debug.WriteLine("MPV_EVENT_END_FILE");
                            Dispatcher.UIThread.Post(() =>
                            {
                                //IsReady = false;
                                //OnPropertyChanged(nameof(IsReady));
                                OnRequestClose?.Invoke();
                            });
                            break;

                        //case MpvEventId.MPV_EVENT_PROPERTY_CHANGE:
                        //    var data = e.ReadData<MpvEventProperty>();
                        //    if (data.ReadStringValue() == "eof-reached" && MediaPlayer.GetPropertyString("eof-reached") == "yes")
                        //    {
                        //        IsReady = false;
                        //        OnRequestClose?.Invoke();
                        //    }
                        //    break;
                    }
                };

            if (videoSource is not null) Play(videoSource);

            //Task.Run(() => Play(videoSource));
            //Task.Run(async () => await Play(videoSource));

            //ErrorString = "An error occurred!";
            //HasError = true;
        }

        private async void Play(VideoSourceModel source)
        {
            if (MediaPlayer is null)
            {
                ErrorString = "MediaPlayer is not initialized!";
                HasError = true;
                return;
            }

            await Task.Delay(10000);

            try
            {
                MediaPlayer.SetProperty("terminal", "no");
                MediaPlayer.SetProperty("idle", "yes");
                MediaPlayer.SetProperty("vo", "libmpv");
                MediaPlayer.SetProperty("hwdec", "auto-safe");
                MediaPlayer.SetProperty("osd-level", "0");
                MediaPlayer.SetProperty("keep-open", "no");

                MediaPlayer.SetProperty("volume", 50.0);

                //MediaPlayer.EnsureRenderContextCreated();

                if (source.Referer != null)
                {
                    MediaPlayer.SetProperty("referrer", source.Referer);
                }

                if (source.Headers != null && source.Headers.Any())
                {
                    var headerList = source.Headers.Select(h => $"{h.Name}: {h.Value}").ToList();
                    string allHeaders = string.Join(",", headerList);
                    MediaPlayer.SetProperty("http-header-fields", allHeaders);

                    var ua = source.Headers.FirstOrDefault(h => h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
                    if (ua != null) MediaPlayer.SetProperty("user-agent", ua.Value);
                }

                await MediaPlayer.ExecuteCommandAsync([MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, source.Url]);

                if (source.Subtitles != null && source.Subtitles.Any())
                {
                    foreach (var track in source.Subtitles)
                    {
                        MediaPlayer.ExecuteCommand(new[] { "sub-add", track.Url });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorString = ex.Message;
                HasError = true;
            }
            finally
            {
                //IsReady = true;
                //OnPropertyChanged(nameof(IsReady));
            }
        }

        private bool IsValidUrlFormat(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            // Geçerli bir URI formatı mı ve HTTP/HTTPS ile mi başlıyor?
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public void Dispose()
        {
            if (MediaPlayer is not null)
            {
                MediaPlayer.ExecuteCommand(new[] { "stop" });
                MediaPlayer.Dispose();
                MediaPlayer = null;
                Debug.WriteLine("PlayerViewModel Dispose");
            }
        }
    }
}
