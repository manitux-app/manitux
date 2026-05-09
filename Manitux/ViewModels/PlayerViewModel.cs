using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibMPVSharp;
using LibMPVSharp.Avalonia;
using LibMPVSharp.Extensions;
using Manitux.Core.Application;
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
        private bool _fileLoaded;

        private AppStrings? _localize;

        public event Action? OnRequestClose;
        public event Action<List<SubtitleModel>>? OnAddSubtitleRequested;

        public void RequestClose()
        {
            OnRequestClose?.Invoke();
        }

        public PlayerViewModel(VideoSourceModel? videoSource, AppStrings? localize)
        {
            if (videoSource is null || string.IsNullOrEmpty(videoSource?.Url) || !IsValidUrlFormat(videoSource?.Url ?? ""))
            {
                _localize = localize;
                ErrorString = _localize?.VideoNotInitialized;
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
                            Dispatcher.UIThread.Post(() =>
                            {
                                _fileLoaded = true;
                                IsReady = true;
                                OnPropertyChanged(nameof(IsReady));
                            });
                            break;

                        case MpvEventId.MPV_EVENT_END_FILE:
                            var endFile = e.ReadData<MpvEventEndFile>();
                            Dispatcher.UIThread.Post(() =>
                            {
                                //IsReady = false;
                                //OnPropertyChanged(nameof(IsReady));
                                if (endFile.reason == MpvEndFileReason.MPV_END_FILE_REASON_ERROR)
                                {
                                    ErrorString = $"MPV playback failed. error={endFile.error}";
                                    HasError = true;
                                    OnPropertyChanged(nameof(ErrorString));
                                    OnPropertyChanged(nameof(HasError));
                                    return;
                                }

                                if (!_fileLoaded)
                                {
                                    ErrorString = $"MPV ended before file loaded. reason={endFile.reason} error={endFile.error}";
                                    HasError = true;
                                    OnPropertyChanged(nameof(ErrorString));
                                    OnPropertyChanged(nameof(HasError));
                                    return;
                                }

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

            if (videoSource is not null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Play(videoSource);
                });
            }
               

            //Task.Run(() => Play(videoSource));
            //Task.Run(async () => await Play(videoSource));

            //ErrorString = "An error occurred!";
            //HasError = true;
        }

        private async void Play(VideoSourceModel source)
        {
            if (MediaPlayer is null)
            {
                ErrorString = _localize?.PlayerNotInitialized;
                HasError = true;
                return;
            }

            await Task.Delay(1000);

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

                if (IsHlsUrl(source.Url))
                {
                    MediaPlayer.SetProperty("ytdl", false);
                    MediaPlayer.SetProperty("demuxer-lavf-format", "hls");
                    MediaPlayer.SetProperty("vid", "auto");
                    MediaPlayer.SetProperty("aid", "auto");
                }

                if (source.Referer != null)
                {
                    MediaPlayer.SetProperty("referrer", source.Referer);
                }

                if (source.Headers != null && source.Headers.Any())
                {
                    var ua = source.Headers.FirstOrDefault(h => h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
                    if (ua != null)
                    {
                        MediaPlayer.SetProperty("user-agent", ua.Value);
                    }

                    var headerList = source.Headers
                        .Where(h => !h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                        .Select(h => $"{h.Name}: {h.Value}")
                        .ToList();

                    if (!string.IsNullOrWhiteSpace(source.Referer)
                        && !source.Headers.Any(h => h.Name.Equals("Referer", StringComparison.OrdinalIgnoreCase)))
                    {
                        headerList.Add($"Referer: {source.Referer}");

                        var uri = new Uri(source.Referer);
                        Debug.WriteLine(uri.Host);
                        headerList.Add($"Host: {uri.Host.Replace("www.", "")}");
                    }

                    if (headerList.Count > 0)
                    {
                        string allHeaders = string.Join(",", headerList);
                        MediaPlayer.SetProperty("http-header-fields", allHeaders);
                    }
                }

                _fileLoaded = false;
                await MediaPlayer.ExecuteCommandAsync([MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, source.Url]);

                if (source.Subtitles != null && source.Subtitles.Any())
                {
                    foreach (var track in source.Subtitles)
                    {
                        try
                        {
                            await MediaPlayer.ExecuteCommandAsync([
                                MPVMediaPlayer.TrackManipulationCommands.SubAdd,
                                track.Url,
                                "auto",
                                track.Name
                            ]);
                        }
                        catch
                        {
                        }
                    }

                    MediaPlayer.SetProperty( "sid", "no" );

                    var subtitles = source.Subtitles
                        .Select((track, index) => new SubtitleModel
                        {
                            Id = (index + 1).ToString(),
                            Name = track.Name,
                            Url = track.Url
                        })
                        .Prepend(new SubtitleModel
                        {
                            Id = "no",
                            Name = _localize?.Closed ?? "Closed",
                            Url = string.Empty
                        })
                        .ToList();

                    OnAddSubtitleRequested?.Invoke(subtitles);
                }
            }
            catch (Exception ex)
            {
                ErrorString = ex.Message;
                HasError = true;
                OnPropertyChanged(nameof(ErrorString));
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                //IsReady = true;
                //OnPropertyChanged(nameof(IsReady));
            }

            //await Task.Delay(10000);
            //Dispose();
        }

        private bool IsValidUrlFormat(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static bool IsHlsUrl(string url)
        {
            return url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (MediaPlayer is not null)
            {
                MediaPlayer.ExecuteCommand(new[] { "stop" });
                MediaPlayer.Dispose();
                MediaPlayer = null;
            }
        }
    }
}
