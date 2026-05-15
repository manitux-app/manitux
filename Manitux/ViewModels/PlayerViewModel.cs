using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom.Events;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using LibMPVSharp;
using LibMPVSharp.Avalonia;
using LibMPVSharp.Extensions;
using Manitux.Core.Application;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using Manitux.Services.Localizations;
using Manitux.Services.Plugins;

namespace Manitux.ViewModels
{
    public partial class PlayerViewModel : ViewModelBase, IDialogContext, IDisposable
    {
        private readonly IPluginService _pluginService;
        private readonly ILocalizationService _localizationService;

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
        public event Action<string>? OnErrorClose;
        public event Action<List<SubtitleModel>>? OnAddSubtitleRequested;
        public event EventHandler<object?>? RequestClose;

        public void Close()
        {
            RequestClose?.Invoke(this, null);
            OnRequestClose?.Invoke();
        }

        public PlayerViewModel(IPluginService pluginService, ILocalizationService localizationService, VideoSourceModel? videoSource)
        {
            _pluginService = pluginService;
            _localizationService = localizationService;
            _localize = _localizationService.Strings;

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
                            Dispatcher.UIThread.Post(async () =>
                            {
                                //IsReady = false;
                                //OnPropertyChanged(nameof(IsReady));
                                if (endFile.reason == MpvEndFileReason.MPV_END_FILE_REASON_ERROR)
                                {
                                    ErrorString = $"Player Error: {endFile.error}";
                                    HasError = true;
                                    OnPropertyChanged(nameof(ErrorString));
                                    OnPropertyChanged(nameof(HasError));
                                    await Task.Delay(500);
                                    OnErrorClose?.Invoke(ErrorString);
                                    //return;
                                }

                                if (!_fileLoaded)
                                {
                                    ErrorString = $"Player Error: reason={endFile.reason} error={endFile.error}";
                                    HasError = true;
                                    OnPropertyChanged(nameof(ErrorString));
                                    OnPropertyChanged(nameof(HasError));
                                    await Task.Delay(500);
                                    OnErrorClose?.Invoke(ErrorString);
                                    //return;
                                }

                                Close();
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

            if (videoSource is null)
            {
                SetError(_localize?.VideoNotInitialized);
                return;
            }

            Dispatcher.UIThread.Post(async () =>
            {
                await LoadAndPlay(videoSource);
            });
               

            //Task.Run(() => Play(videoSource));
            //Task.Run(async () => await Play(videoSource));

            //ErrorString = "An error occurred!";
            //HasError = true;
        }

        private async Task LoadAndPlay(VideoSourceModel videoSource)
        {
            var source = await GetVideoSources(videoSource);
            if (source is null || string.IsNullOrEmpty(source.Url) || !IsValidUrlFormat(source.Url))
            {
                SetError(_localize?.VideoNotInitialized);
                return;
            }

            Play(source);
        }

        private async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
        {
            try
            {
                if (_pluginService.CurrentPlugin is not null)
                {
                    var source = await _pluginService.CurrentPlugin.GetVideoSources(videoSource);
                    if (source is not null)
                    {
                        return source;
                    }

                    SetError(_localize?.PageNotFound);
                    return null;
                }

                return IsValidUrlFormat(videoSource.Url) ? videoSource : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PlayerViewModel] Video source resolve failed. Url: {videoSource.Url} Error: {ex}");
                SetError(_localize?.VideoNotInitialized);
                return null;
            }
        }

        private async void Play(VideoSourceModel source)
        {
            if (MediaPlayer is null)
            {
                SetError(_localize?.PlayerNotInitialized);
                return;
            }

            await Task.Delay(1000);

            try
            {
                MediaPlayer.SetProperty("terminal", "no");
                //MediaPlayer.SetProperty("msg-level", "all=v,sub=debug,lavf=debug");

                MediaPlayer.SetProperty("idle", "yes");
                MediaPlayer.SetProperty("vo", "libmpv");
                MediaPlayer.SetProperty("hwdec", "auto-safe");
                MediaPlayer.SetProperty("osd-level", "0");
                MediaPlayer.SetProperty("keep-open", "no");
                MediaPlayer.SetProperty("sub-auto", "all");
                MediaPlayer.SetProperty("sub-file-paths", ".");

                MediaPlayer.SetProperty("volume", 50.0);

                // string ytdlPath = Path.Combine(AppContext.BaseDirectory, "yt-dlp");
                // Debug.WriteLine($"yt-dlp path:{ytdlPath}");
                // MediaPlayer.SetProperty("script-opts", $"ytdl_hook-ytdl_path={ytdlPath}");
                //MediaPlayer.SetProperty("script-opts-append", "ytdl_hook-ytdl_path=C:\\yt-dlp\\yt-dlp.exe");

                // if (source.Url.Contains("youtube"))
                // {
                //     MediaPlayer.SetProperty("ytdl", "yes");
                //     MediaPlayer.SetProperty("try_ytdl_first", "yes");
                // }

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

                var httpHeaderFields = BuildHttpHeaderFields(source.Headers, source.Referer);
                if (!string.IsNullOrWhiteSpace(httpHeaderFields))
                {
                    MediaPlayer.SetProperty("http-header-fields", httpHeaderFields);
                }

                if (source.Headers != null && source.Headers.Any())
                {
                    var ua = source.Headers.FirstOrDefault(h => h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
                    if (ua != null)
                    {
                        MediaPlayer.SetProperty("user-agent", ua.Value);
                    }
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
                SetError(ex.Message);
            }
            finally
            {
                //IsReady = true;
                //OnPropertyChanged(nameof(IsReady));
            }

            //await Task.Delay(10000);
            //Dispose();
        }

        private void SetError(string? message)
        {
            ErrorString = message ?? "Error";
            HasError = true;
            IsReady = true;
            OnPropertyChanged(nameof(ErrorString));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsReady));
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

        private static string? BuildHttpHeaderFields(List<HeaderModel>? headers, string? referer)
        {
            var headerList = headers?
                .Where(h => !string.IsNullOrWhiteSpace(h.Name)
                            && !string.IsNullOrWhiteSpace(h.Value)
                            && !h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                .Select(h => $"{h.Name}: {h.Value}")
                .ToList()
                ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(referer)
                && !headerList.Any(h => h.StartsWith("Referer:", StringComparison.OrdinalIgnoreCase)))
            {
                headerList.Add($"Referer: {referer}");
            }

            return headerList.Count == 0 ? null : string.Join(",", headerList);
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
