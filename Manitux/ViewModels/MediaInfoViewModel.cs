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

    private void OnActivate()
    {
        //WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(this));
    }

    public void Play(VideoSourceModel videoSource)
    {
        Debug.WriteLine(videoSource.Url);
        ShowToast();
    }

    public void VlcPlay(VideoSourceModel videoSource)
    {
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
                ShowError();
            }
        }
        else
        {
            ShowError();
        }
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

    private void ShowToast()
    {
        ToastManager?.Show(
                new Toast(Localize?.Welcome),
                type: NotificationType.Success,
                showIcon: true,
                classes: ["Light"]);

    }

    private void ShowError()
    {
        ToastManager?.Show(
                new Toast(Localize?.PageNotFound),
                type: NotificationType.Error,
                showIcon: true,
                classes: ["Light"]);

        OnRequestClose?.Invoke();
    }
}