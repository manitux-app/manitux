using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using AngleSharp.Media;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Application;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;
using Manitux.Pages;
using Manitux.Player;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Manitux.ViewModels;

public partial class MediaInfoViewModel : ViewModelBase
{
    [ObservableProperty] private MediaInfoModel? _mediaInfo;

    [ObservableProperty] private AppStrings? _localize;

    public WindowNotificationManager? NotificationManager { get; set; }
    public WindowToastManager? ToastManager { get; set; }

    private PluginBase? _plugin;
    public List<SeasonModel>? Seasons { get; set; }

    public event Action? OnDataRefreshed;
    public event Action? OnRequestClose;

    //public ICommand ActivateCommand { get; set; }
    // <!--Content="{Binding EpisodeNumber, StringFormat='{}{0}. Bölüm'}"-->
    public MediaInfoViewModel(PluginBase plugin, MediaInfoModel? mediaInfo, AppStrings localize)
    {
        //ActivateCommand = new RelayCommand(OnActivate);

        if (mediaInfo is null) return;
        _plugin = plugin;
        Localize = localize;
        MediaInfo = mediaInfo;

        if(mediaInfo.Episodes is not null)
        {
            CreateSeasonGroup(mediaInfo.Episodes);
        }
        
        OnPropertyChanged(nameof(MediaInfo));
    }

    public async void Play(VideoSourceModel videoSource)
    {
        Debug.WriteLine(videoSource.Url);
        var source = await GetVideoSources(videoSource);

        Debug.WriteLine($"VideoSource: {JsonSerializer.Serialize(source)}" + Environment.NewLine);
        ShowPlayer(source);

        //if(source is not null & IsValidUrlFormat(source?.Url ?? ""))
        //{
        //    Debug.WriteLine($"VideoSource: {JsonSerializer.Serialize(source)}" + Environment.NewLine);
        //    ShowPlayer(source);
        //}
        //else
        //{
        //    ShowError(Localize?.PageNotFound ?? "Page not found");
        //}
    }

    public void VlcPlay(VideoSourceModel videoSource)
    {
        ShowTestPlayer();
        Debug.WriteLine(videoSource.Url);
    }

    public void MpvPlay(VideoSourceModel videoSource)
    {
        Debug.WriteLine(videoSource.Url);
    }

    public async void GetMediaInfo(RelatedVideoModel relatedVideo)
    {
        if (_plugin is not null)
        {
            var pageItem = new PageItemModel() { Title = relatedVideo.Title, Url = relatedVideo.Url };
            var mediaInfo = await _plugin.GetMediaInfo(pageItem);
            if(mediaInfo is not null)
            {
                MediaInfo = mediaInfo;
                OnPropertyChanged(nameof(MediaInfo));
                OnDataRefreshed?.Invoke();
            }
            else
            {
                ShowError(Localize?.PageNotFound ?? "Page not found");
            }
        }
        else
        {
            ShowError(Localize?.PageNotFound ?? "Page not found");
        }
    }

    private async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        if (_plugin is not null)
        {
            var source = await _plugin.GetVideoSources(videoSource);
            
            if (source is not null)
            {
                return source;
            }
            else
            {
                ShowError(Localize?.PageNotFound ?? "Page not found");
            }
        }
        else
        {
            ShowError(Localize?.PageNotFound ?? "Page not found");
        }

        return null;
    }

    public void CreateSeasonGroup(List<EpisodeModel> episodes)
    {
        Seasons = episodes
            .GroupBy(e => e.SeasonNumber)
            .OrderBy(g => g.Key)
            .Select(g => new SeasonModel
            {
                Title = $"{Localize?.Season} {g.Key}",
                Episodes = g.OrderBy(e => e.EpisodeNumber).ToList()
            })
            .ToList();
    }

    private async void ShowPlayer(VideoSourceModel? videoSource)
    {
        var options = new OverlayDialogOptions()
        {
            FullScreen = true,
            Buttons = DialogButton.None,
            Mode = DialogMode.None,
            CanDragMove = false,
            CanResize = false,
        };

        await OverlayDialog.ShowCustomModal<PlayerView, PlayerViewModel, object>(new PlayerViewModel(videoSource, Localize), null, options: options);

        //  ShowPlayer(new VideoSourceModel() { Name = "Test", Url = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json", Subtitles = new() { new() { Name = "Test", Url = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt" } } });
    }

    private void ShowTestPlayer()
    {
        ShowPlayer(new VideoSourceModel() { Name = "Test", Url = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json", Subtitles = new() { new() { Id="1", Name = "Test", Url = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt" } } });
    }

    private bool IsValidUrlFormat(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // Geçerli bir URI formatý mý ve HTTP/HTTPS ile mi baþlýyor?
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private void ShowToast()
    {
        ToastManager?.Show(
                new Toast(Localize?.Welcome),
                type: NotificationType.Success,
                showIcon: true,
                classes: ["Light"]);

    }

    private void ShowError(string message)
    {
        ToastManager?.Show(
                new Toast(message),
                type: NotificationType.Error,
                showIcon: true,
                classes: ["Light"]);

        OnRequestClose?.Invoke();
    }
}