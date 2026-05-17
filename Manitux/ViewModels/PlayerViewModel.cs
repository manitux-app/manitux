using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
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
        private bool _hasStartedPlayback;
        private bool _isChangingSource;
        private bool _suppressSourceSelectionChanged;
        private string? _currentSourceKey;

        public ObservableCollection<VideoSourceModel> VideoSources { get; } = new();

        [ObservableProperty]
        private VideoSourceModel? _selectedVideoSource;

        public bool HasSourceOptions => VideoSources.Count > 1;

        private AppStrings? _localize;
        private readonly SubtitleManager _subtitleManager = new();
        public AppStrings L { get; }

        public event Action? OnRequestClose;
        public event Action<string>? OnErrorClose;
        public event Action<List<SubtitleModel>>? OnAddSubtitleRequested;
        public event EventHandler<object?>? RequestClose;

        public void Close()
        {
            RequestClose?.Invoke(this, null);
            OnRequestClose?.Invoke();
        }

        public PlayerViewModel(
            IPluginService pluginService,
            ILocalizationService localizationService,
            VideoSourceModel? videoSource,
            IEnumerable<VideoSourceModel>? availableSources = null)
        {
            _pluginService = pluginService;
            _localizationService = localizationService;
            _localize = _localizationService.Strings;
            L = _localizationService.Strings;

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
                                _hasStartedPlayback = true;
                                _isChangingSource = false;
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
                                if (_isChangingSource && endFile.reason != MpvEndFileReason.MPV_END_FILE_REASON_ERROR)
                                {
                                    return;
                                }

                                if (endFile.reason == MpvEndFileReason.MPV_END_FILE_REASON_ERROR)
                                {
                                    _isChangingSource = false;
                                    ErrorString = $"Player Error: {endFile.error}";
                                    HasError = true;
                                    OnPropertyChanged(nameof(ErrorString));
                                    OnPropertyChanged(nameof(HasError));
                                    //await Task.Delay(500);
                                    //OnErrorClose?.Invoke(ErrorString);
                                    //return;
                                }

                                if (!_fileLoaded)
                                {
                                    ErrorString = $"Player Error: reason={endFile.reason} error={endFile.error}";
                                    HasError = true;
                                    OnPropertyChanged(nameof(ErrorString));
                                    OnPropertyChanged(nameof(HasError));
                                    //await Task.Delay(500);
                                    //OnErrorClose?.Invoke(ErrorString);
                                    //return;
                                }

                                //Close();
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

            foreach (var source in CreateSourceList(videoSource, availableSources))
            {
                VideoSources.Add(source);
            }

            OnPropertyChanged(nameof(HasSourceOptions));

            _suppressSourceSelectionChanged = true;
            SelectedVideoSource = VideoSources.FirstOrDefault(source => SameSource(source, videoSource)) ?? videoSource;
            _suppressSourceSelectionChanged = false;

            Dispatcher.UIThread.Post(async () =>
            {
                await LoadAndPlaySelectedSource(SelectedVideoSource ?? videoSource);
            });
               

            //Task.Run(() => Play(videoSource));
            //Task.Run(async () => await Play(videoSource));

            //ErrorString = "An error occurred!";
            //HasError = true;
        }

        partial void OnSelectedVideoSourceChanged(VideoSourceModel? value)
        {
            if (_suppressSourceSelectionChanged || value is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(async () => await LoadAndPlaySelectedSource(value));
        }

        private async Task LoadAndPlaySelectedSource(VideoSourceModel videoSource)
        {
            var sourceKey = GetSourceKey(videoSource);
            if (_currentSourceKey == sourceKey && (_fileLoaded || _isChangingSource))
            {
                return;
            }

            _currentSourceKey = sourceKey;
            HasError = false;
            ErrorString = null;
            IsReady = false;
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(ErrorString));
            OnPropertyChanged(nameof(IsReady));

            await LoadAndPlay(videoSource);
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

                _isChangingSource = _hasStartedPlayback;
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
            _isChangingSource = false;
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

        private static List<VideoSourceModel> CreateSourceList(VideoSourceModel selectedSource, IEnumerable<VideoSourceModel>? availableSources)
        {
            var sources = new List<VideoSourceModel>();
            AddDistinct(sources, selectedSource);

            if (availableSources is not null)
            {
                foreach (var source in availableSources)
                {
                    AddDistinct(sources, source);
                }
            }

            return sources;
        }

        private static void AddDistinct(List<VideoSourceModel> sources, VideoSourceModel source)
        {
            if (string.IsNullOrWhiteSpace(source.Url)
                || sources.Any(existing => SameSource(existing, source)))
            {
                return;
            }

            sources.Add(source);
        }

        private static bool SameSource(VideoSourceModel left, VideoSourceModel right)
        {
            return string.Equals(left.Url, right.Url, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSourceKey(VideoSourceModel source)
        {
            return $"{source.Name}|{source.Url}".ToLowerInvariant();
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
