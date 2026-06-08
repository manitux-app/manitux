using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using AngleSharp.Media;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.Core.Application;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;
using Manitux.Pages;
using Manitux.Player;
using Manitux.Services.Favorites;
using Manitux.Services.Localizations;
using Manitux.Services.Plugins;
using Manitux.Services.WatchedEpisodes;
using Ursa.Controls;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Manitux.ViewModels;

public partial class MediaInfoViewModel : ViewModelBase, IDialogContext
{
    private readonly IPluginService _pluginService;
    private readonly ILocalizationService _localizationService;
    private readonly IFavoritesService _favoritesService;
    private readonly IWatchedEpisodesService _watchedEpisodesService;
    private readonly PageItemModel? _sourcePageItem;
    private readonly Action<PlayerViewModel>? _showPlayer;
    private readonly Action? _goBack;

    [ObservableProperty] private MediaInfoModel? _mediaInfo;

    [ObservableProperty] private AppStrings? _localize;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _favoriteButtonText;

    [ObservableProperty] private bool _isFavoriteButtonVisible;

    [ObservableProperty] private bool _isFavorite;

    public AppStrings L { get; }

    public WindowNotificationManager? NotificationManager { get; set; }
    public WindowToastManager? ToastManager { get; set; }

    [ObservableProperty] private List<SeasonModel>? _seasons;

    public event Action? OnDataRefreshed;
    public event Action? OnRequestClose;
    public event EventHandler<object?>? RequestClose;

    //public ICommand ActivateCommand { get; set; }
    // <!--Content="{Binding EpisodeNumber, StringFormat='{}{0}. B�l�m'}"-->
    public MediaInfoViewModel(
        IPluginService pluginService,
        ILocalizationService localizationService,
        IFavoritesService favoritesService,
        IWatchedEpisodesService watchedEpisodesService,
        PageItemModel? pageItem,
        Action<PlayerViewModel>? showPlayer = null,
        Action? goBack = null)
    {
        //ActivateCommand = new RelayCommand(OnActivate);

        _pluginService = pluginService;
        _localizationService = localizationService;
        _favoritesService = favoritesService;
        _watchedEpisodesService = watchedEpisodesService;
        _sourcePageItem = pageItem;
        _showPlayer = showPlayer;
        _goBack = goBack;
        L = _localizationService.Strings;
        Localize = L;
        FavoriteButtonText = L.AddToFavorites;

        if (pageItem is null) return;

        MediaInfo = CreatePlaceholderMediaInfo(pageItem);
        _ = LoadMediaInfo(pageItem);
    }

    [RelayCommand]
    private void GoBack()
    {
        _goBack?.Invoke();
    }

    private static MediaInfoModel CreatePlaceholderMediaInfo(PageItemModel pageItem)
    {
        return new MediaInfoModel
        {
            Title = pageItem.Title,
            Url = pageItem.Url,
            Poster = pageItem.Poster,
            Rating = pageItem.Rating,
            Year = pageItem.Year,
        };
    }

    private async Task LoadMediaInfo(PageItemModel pageItem)
    {
        if (_pluginService.CurrentPlugin is null)
        {
            ShowError(Localize?.PageNotFound);
            return;
        }

        IsLoading = true;

        try
        {
            var mediaInfo = await _pluginService.CurrentPlugin.GetMediaInfo(pageItem);
            if (mediaInfo is null)
            {
                ShowError(Localize?.PageNotFound);
                return;
            }

            SetMediaInfo(mediaInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MediaInfo load failed: {ex}");
            ShowError(Localize?.PageNotFound);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetMediaInfo(MediaInfoModel mediaInfo)
    {
        IsFavoriteButtonVisible = false;
        MediaInfo = mediaInfo;
        Seasons = mediaInfo.Episodes is null ? null : CreateSeasonGroup(mediaInfo.Episodes);
        _ = ApplyWatchedEpisodeStates(mediaInfo);
        OnDataRefreshed?.Invoke();
        _ = UpdateFavoriteButtonVisibility(mediaInfo);
    }

    [RelayCommand]
    private async Task AddToFavorites()
    {
        if (MediaInfo is null)
        {
            ShowError(Localize?.PageNotFound);
            return;
        }

        var favoriteItem = CreateFavoritePageItem(MediaInfo);
        if (IsFavorite)
        {
            await _favoritesService.RemoveAsync(favoriteItem);
            SetFavoriteState(false);
            ShowSuccess(L.RemovedFromFavorites);
            return;
        }

        await _favoritesService.AddOrUpdateAsync(favoriteItem);
        SetFavoriteState(true);
        ShowSuccess(L.AddedToFavorites);
    }

    private async Task UpdateFavoriteButtonVisibility(MediaInfoModel mediaInfo)
    {
        try
        {
            var favoriteItem = CreateFavoritePageItem(mediaInfo);
            var exists = await _favoritesService.ExistsAsync(favoriteItem);
            SetFavoriteState(exists);
            IsFavoriteButtonVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Favorite status check failed: {ex}");
            IsFavoriteButtonVisible = true;
            SetFavoriteState(false);
        }
    }

    private void SetFavoriteState(bool isFavorite)
    {
        IsFavorite = isFavorite;
        FavoriteButtonText = isFavorite ? L.RemoveFromFavorites : L.AddToFavorites;
    }

    private void ShowSuccess(string message)
    {
        ToastManager?.Show(
            new Toast(message),
            type: NotificationType.Success,
            showIcon: true,
            classes: ["Light"]);
    }

    private PageItemModel CreateFavoritePageItem(MediaInfoModel mediaInfo)
    {
        var currentPlugin = _pluginService.CurrentPlugin;

        return new PageItemModel
        {
            Title = mediaInfo.Title,
            Url = mediaInfo.Url,
            CategoryName = _sourcePageItem?.CategoryName,
            Poster = mediaInfo.Poster ?? _sourcePageItem?.Poster,
            Rating = mediaInfo.Rating ?? _sourcePageItem?.Rating,
            Year = mediaInfo.Year ?? _sourcePageItem?.Year,
            PluginId = _sourcePageItem?.PluginId ?? currentPlugin?.Manifest.Id,
            PluginName = _sourcePageItem?.PluginName ?? currentPlugin?.Manifest.Name,
            PluginFavicon = _sourcePageItem?.PluginFavicon ?? currentPlugin?.Config.Favicon
        };
    }

    public async void Play(VideoSourceModel videoSource)
    {
        if(videoSource is not null)
        {
           Debug.WriteLine($"VideoSource: {JsonSerializer.Serialize(videoSource)}" + Environment.NewLine);
           ShowPlayer(videoSource, MediaInfo?.VideoSources);
        }
        else
        {
           ShowError(Localize?.VideoNotInitialized);
        }
    }

    public async void VlcPlay(VideoSourceModel videoSource)
    {
        var source = await GetVideoSources(videoSource);
        if(source is null)
        {
            ShowError(Localize?.VideoNotInitialized); 
            return;
        } 
        await PlayWithExternalPlayer(source, isVlc: true);
        //Debug.WriteLine(source.Url);
    }

    public async void MpvPlay(VideoSourceModel videoSource)
    {
        var source = await GetVideoSources(videoSource);
        if(source is null)
        {
            ShowError(Localize?.VideoNotInitialized); 
            return;
        } 
        await PlayWithExternalPlayer(source, isVlc: false);
        //Debug.WriteLine(source.Url);
    }

    [RelayCommand]
    private async Task DownloadVideo(VideoSourceModel? videoSource)
    {
        if (videoSource is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        var source = await GetVideoSources(videoSource);
        if (source is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        await DownloadSource(source);
    }

    public async void PlayEpisode(EpisodeModel episode)
    {
        var sources = await GetEpisodeVideoSources(episode);
        var source = sources?.FirstOrDefault(x => !x.IsTrailer)
                     ?? sources?.FirstOrDefault();
        if (source is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        Debug.WriteLine($"Episode VideoSource: {JsonSerializer.Serialize(source)}" + Environment.NewLine);
        ShowPlayer(source, sources);
    }

    [RelayCommand]
    private async Task ToggleWatchedEpisode(EpisodeModel? episode)
    {
        if (episode is null || MediaInfo is null)
        {
            return;
        }

        var pluginId = GetCurrentPluginId();
        if (episode.IsWatched)
        {
            await _watchedEpisodesService.UnmarkAsWatchedAsync(pluginId, MediaInfo.Url, episode.Url);
            episode.IsWatched = false;
            return;
        }

        await _watchedEpisodesService.MarkAsWatchedAsync(pluginId, MediaInfo.Url, episode.Url);
        episode.IsWatched = true;
    }

    public async void VlcPlayEpisode(EpisodeModel episode)
    {
        var source = await GetEpisodeVideoSource(episode);
        if (source is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        var resolvedSource = await GetVideoSources(source);
        if (resolvedSource is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        await PlayWithExternalPlayer(resolvedSource, isVlc: true);
    }

    public async void MpvPlayEpisode(EpisodeModel episode)
    {
        var source = await GetEpisodeVideoSource(episode);
        if (source is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        var resolvedSource = await GetVideoSources(source);
        if (resolvedSource is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        await PlayWithExternalPlayer(resolvedSource, isVlc: false);
    }

    [RelayCommand]
    private async Task DownloadEpisode(EpisodeModel? episode)
    {
        if (episode is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        var source = await GetEpisodeVideoSource(episode);
        if (source is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        var resolvedSource = await GetVideoSources(source);
        if (resolvedSource is null)
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        await DownloadSource(resolvedSource);
    }

    public async void GetMediaInfo(RelatedVideoModel relatedVideo)
    {
        if (relatedVideo is null)
        {
            ShowError(Localize?.PageNotFound);
            return;
        }

        var pageItem = new PageItemModel()
        {
            Title = relatedVideo.Title,
            Url = relatedVideo.Url,
            Poster = relatedVideo.Poster
        };

        await LoadMediaInfo(pageItem);
    }

    private async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        if (_pluginService.CurrentPlugin is not null)
        {
            var source = await _pluginService.CurrentPlugin.GetVideoSources(videoSource);
            
            if (source is not null)
            {
                return source;
            }
            else
            {
                ShowError(Localize?.PageNotFound);
            }
        }
        else
        {
            ShowError(Localize?.PageNotFound);
        }

        return null;
    }

    private async Task<VideoSourceModel?> GetEpisodeVideoSource(EpisodeModel episode)
    {
        var sources = await GetEpisodeVideoSources(episode);
        return sources?.FirstOrDefault(x => !x.IsTrailer)
               ?? sources?.FirstOrDefault();
    }

    private async Task<List<VideoSourceModel>?> GetEpisodeVideoSources(EpisodeModel episode)
    {
        if (_pluginService.CurrentPlugin is null)
        {
            ShowError(Localize?.PageNotFound);
            return null;
        }

        if (episode is null || string.IsNullOrWhiteSpace(episode.Url))
        {
            ShowError(Localize?.PageNotFound);
            return null;
        }

        IsLoading = true;

        try
        {
            var pageItem = new PageItemModel
            {
                Title = episode.Title ?? $"{L.Episode} {episode.EpisodeNumber}",
                Url = episode.Url
            };

            var episodeInfo = await _pluginService.CurrentPlugin.GetMediaInfo(pageItem);
            var sources = episodeInfo?.VideoSources?
                .Where(source => !string.IsNullOrWhiteSpace(source.Url))
                .ToList();

            if (sources is null || sources.Count == 0)
            {
                ShowError(Localize?.VideoNotInitialized);
                return null;
            }

            return sources;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Episode source load failed: {ex}");
            ShowError(Localize?.VideoNotInitialized);
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DownloadSource(VideoSourceModel source)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider is null || string.IsNullOrWhiteSpace(source.Url))
        {
            ShowError(Localize?.VideoNotInitialized);
            return;
        }

        var extension = GetFileExtension(source.Url);
        var downloadsFolder = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = L.Download,
            SuggestedFileName = $"{SanitizeFileName(MediaInfo?.Title ?? source.Name)} - {SanitizeFileName(source.Name)}{extension}",
            SuggestedStartLocation = downloadsFolder,
            DefaultExtension = extension.TrimStart('.'),
            FileTypeChoices =
            [
                new FilePickerFileType("Video")
                {
                    Patterns = ["*.mp4", "*.mkv", "*.webm", "*.m3u8"]
                }
            ]
        });

        if (file is null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, source.Url);

            if (!string.IsNullOrWhiteSpace(source.Referer))
            {
                request.Headers.Referrer = new Uri(source.Referer);
            }

            if (source.Headers is not null)
            {
                foreach (var header in source.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = await file.OpenWriteAsync();
            await input.CopyToAsync(output);

            ShowSuccess(L.DownloadCompleted);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Download failed: {ex}");
            ShowError($"{L.DownloadFailed}: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PlayWithExternalPlayer(VideoSourceModel source, bool isVlc)
    {
        var playerManager = new ExternalPlayerManager();
        var started = isVlc
            ? playerManager.VlcPlay(source)
            : playerManager.MpvPlay(source);

        if (started)
        {
            return;
        }

        ShowError(isVlc ? L.VlcPlayerNotFound : L.MpvPlayerNotFound);

        var executablePath = await PickExternalPlayerPath(isVlc);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        started = isVlc
            ? playerManager.VlcPlay(source, executablePath)
            : playerManager.MpvPlay(source, executablePath);

        if (!started)
        {
            ShowError(isVlc ? L.VlcPlayerNotFound : L.MpvPlayerNotFound);
        }
    }

    private async Task<string?> PickExternalPlayerPath(bool isVlc)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider is null)
        {
            return null;
        }

         var defaultFolder = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = isVlc ? L.SelectVlcPlayerPath : L.SelectMpvPlayerPath,
            AllowMultiple = false,
            SuggestedStartLocation = defaultFolder,
            FileTypeFilter =
            [
                new FilePickerFileType(isVlc ? L.VlcPlayer : L.MpvPlayer)
                {
                    Patterns = OperatingSystem.IsWindows() ? ["*.exe"] : ["*"]
                }
            ]
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private static IStorageProvider? GetStorageProvider()
    {
        return Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow?.StorageProvider,
            ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView)?.StorageProvider,
            _ => null
        };
    }

    private static string GetFileExtension(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension;
            }
        }

        return ".mp4";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "video" : cleaned;
    }

    public List<SeasonModel> CreateSeasonGroup(List<EpisodeModel> episodes)
    {
        return episodes
            .GroupBy(e => e.SeasonNumber)
            .OrderBy(g => g.Key)
            .Select(g => new SeasonModel
            {
                Title = $"{Localize?.Season} {g.Key}",
                Episodes = g.OrderBy(e => e.EpisodeNumber).ToList()
            })
            .ToList();
    }

    private async Task ApplyWatchedEpisodeStates(MediaInfoModel mediaInfo)
    {
        if (mediaInfo.Episodes is null || mediaInfo.Episodes.Count == 0)
        {
            return;
        }

        try
        {
            var watchedEpisodeUrls = await _watchedEpisodesService.GetWatchedEpisodeUrlsAsync(GetCurrentPluginId(), mediaInfo.Url);
            foreach (var episode in mediaInfo.Episodes)
            {
                episode.IsWatched = watchedEpisodeUrls.Contains(episode.Url);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Watched episode status load failed: {ex}");
        }
    }

    private string? GetCurrentPluginId()
    {
        return _sourcePageItem?.PluginId ?? _pluginService.CurrentPlugin?.Manifest.Id;
    }

    private void ShowPlayer(VideoSourceModel? videoSource, IEnumerable<VideoSourceModel>? availableSources = null)
    {
        _showPlayer?.Invoke(new PlayerViewModel(_pluginService, _localizationService, videoSource, availableSources));

        //  ShowPlayer(new VideoSourceModel() { Name = "Test", Url = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json", Subtitles = new() { new() { Name = "Test", Url = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt" } } });
    }

    private void ShowTestPlayer()
    {
        ShowPlayer(new VideoSourceModel() { Name = "Test", Url = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json", Subtitles = new() { new() { Id="1", Name = "Test", Url = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt" } } });
    }

    private bool IsValidUrlFormat(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public void ShowToast()
    {
        ToastManager?.Show(
                new Toast(Localize?.Welcome),
                type: NotificationType.Success,
                showIcon: true,
                classes: ["Light"]);

    }

    private void ShowError(string? message)
    {
        ToastManager?.Show(
                new Toast(message ?? L.Error),
                type: NotificationType.Error,
                showIcon: true,
                classes: ["Light"]);

        //RequestClose?.Invoke(this, false);
        //OnRequestClose?.Invoke();
    }

   public void Close()
    {
        RequestClose?.Invoke(this, null);
    }
}
