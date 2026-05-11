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
using Manitux.Core.Helpers;
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
        private readonly SubtitleManager _subtitleManager = new();

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
                MediaPlayer.SetProperty("terminal", "yes");
                MediaPlayer.SetProperty("msg-level", "all=v,sub=debug,lavf=debug");

                MediaPlayer.SetProperty("idle", "yes");
                MediaPlayer.SetProperty("vo", "libmpv");
                MediaPlayer.SetProperty("hwdec", "auto-safe");
                MediaPlayer.SetProperty("osd-level", "0");
                MediaPlayer.SetProperty("keep-open", "no");
                MediaPlayer.SetProperty("sub-auto", "all");
                MediaPlayer.SetProperty("sub-file-paths", ".");

                MediaPlayer.SetProperty("volume", 50.0);

                //MediaPlayer.EnsureRenderContextCreated();

                if (IsHlsUrl(source.Url))
                {
                    MediaPlayer.SetProperty("ytdl", "no");
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
                        MediaPlayer.SetProperty("http-header-fields", $"{ua.Name}: {ua.Value},referer: {source.Referer}");
                        MediaPlayer.SetProperty("user-agent", ua.Value);
                    }

                    // if (!string.IsNullOrWhiteSpace(source.Referer)
                    //     && !source.Headers.Any(h => h.Name.Equals("Referer", StringComparison.OrdinalIgnoreCase)))
                    // {
                    //     MediaPlayer.SetProperty("referrer", source.Referer);
                    // }

                    // var headerList = source.Headers
                    //     //.Where(h => !h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                    //     .Select(h => $"{h.Name}: {h.Value}")
                    //     .ToList();

                    // if (!string.IsNullOrWhiteSpace(source.Referer)
                    //     && !source.Headers.Any(h => h.Name.Equals("Referer", StringComparison.OrdinalIgnoreCase)))
                    // {
                    //     headerList.Add($"Referer: {source.Referer}");

                    //     //var uri = new Uri(source.Referer);
                    //     //Debug.WriteLine(uri.Host);
                    //     //headerList.Add($"Host: {uri.Host.Replace("www.", "")}");
                    // }

                    // if (headerList.Count > 0)
                    // {
                    //     string allHeaders = string.Join(",", headerList);
                    //     MediaPlayer.SetProperty("http-header-fields", allHeaders);
                    // }
                }

                _fileLoaded = false;
                var resolvedSubtitles = await ResolveSubtitlesAsync(source.Subtitles);
                var loadFileCommand = CreateLoadFileCommand(source.Url, resolvedSubtitles);
                Debug.WriteLine($"[PlayerViewModel] loadfile args: {string.Join(" | ", loadFileCommand)}");
                try
                {
                    await Task.Delay(1000);
                    await MediaPlayer.ExecuteCommandAsync(loadFileCommand);
                }
                catch (Exception ex) when (resolvedSubtitles.Any())
                {
                    Debug.WriteLine($"[PlayerViewModel] loadfile with subtitles failed, retrying without subtitles. Args: {string.Join(" | ", loadFileCommand)} Error: {ex}");
                    await MediaPlayer.ExecuteCommandAsync([MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, source.Url]);
                }

                if (resolvedSubtitles.Any())
                {
                    await WaitForFileLoadedAsync();

                    MediaPlayer.SetProperty( "sid", "no" );

                    var subtitles = resolvedSubtitles
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

        private static string NormalizeSubtitlePathForMpv(string subtitleUrl)
        {
            if (string.IsNullOrWhiteSpace(subtitleUrl)
                || Uri.TryCreate(subtitleUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return subtitleUrl;
            }

            var path = Uri.TryCreate(subtitleUrl, UriKind.Absolute, out uri) && uri.IsFile
                ? uri.LocalPath
                : subtitleUrl;

            try
            {
                path = System.IO.Path.GetFullPath(path);
            }
            catch
            {
            }

            return path.Replace('\\', '/');

            //return OperatingSystem.IsWindows()
                //? "file:///" + path.Replace('/', '\\') // C:\\Users\\metek\\AppData\\Local\\Temp\\Manitux\\Subtitles\\252bac07b2978e98db6942f7.vtt
                //: path.Replace('\\', '/'); // /home/metek/temp/Manitux/Subtitles/252bac07b2978e98db6942f7.vtt
        }

        private async Task<List<SubtitleModel>> ResolveSubtitlesAsync(List<SubtitleModel>? subtitles)
        {
            var resolvedSubtitles = new List<SubtitleModel>();

            if (subtitles is null || subtitles.Count == 0)
            {
                return resolvedSubtitles;
            }

            foreach (var track in subtitles)
            {
                try
                {
                    var subtitleUrl = await _subtitleManager.ResolveAsync(track.Url);
                    var subtitlePath = NormalizeSubtitlePathForMpv(subtitleUrl);

                    resolvedSubtitles.Add(new SubtitleModel
                    {
                        Id = track.Id,
                        Name = track.Name,
                        Url = subtitlePath
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PlayerViewModel] Subtitle resolve failed. Url: {track.Url} Error: {ex}");
                }
            }

            return resolvedSubtitles;
        }

        private static string[] CreateLoadFileCommand(string url, List<SubtitleModel> subtitles)
        {
            if (subtitles.Count == 0)
            {
                return [MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, url];
            }

            var separator = OperatingSystem.IsWindows() ? ";" : ":";
            var subtitleFiles = string.Join(separator, subtitles
                .Where(subtitle => !string.IsNullOrWhiteSpace(subtitle.Url))
                .Select(subtitle => NormalizeSubtitlePathForMpv(subtitle.Url)));
                //.Select(subtitle => $"'{NormalizeSubtitlePathForMpv(subtitle.Url)}'"));

            var options = string.IsNullOrWhiteSpace(subtitleFiles)
                ? string.Empty
                : $"sub-files={subtitleFiles}";

            return string.IsNullOrWhiteSpace(options)
                ? [MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, url]
                : [MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, url, "replace", "-1", options];
        }

        private static string EscapeLoadFileOptionValue(string value)
        {
            //string url = $"\"file:///{subtitle?.Url.Replace('\\', '/')}\"";
            return OperatingSystem.IsWindows()
                ? value.Replace('\\', '/') //.Replace('/', '\\') // C:\\Users\\metek\\AppData\\Local\\Temp\\Manitux\\Subtitles\\252bac07b2978e98db6942f7.vtt
                : value.Replace('\\', '/'); // /home/metek/temp/Manitux/Subtitles/252bac07b2978e98db6942f7.vtt
        }

        private async Task WaitForFileLoadedAsync()
        {
            const int maxAttempts = 100;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (_fileLoaded || HasError)
                {
                    return;
                }

                await Task.Delay(100);
            }

            Debug.WriteLine("[PlayerViewModel] Timed out waiting for MPV file-loaded before adding subtitles.");
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
