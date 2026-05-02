using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Models;
using Manitux.Models;
using Manitux.Pages;

namespace Manitux.ViewModels;

public partial class MediaInfoViewModel : ViewModelBase
{
    [ObservableProperty] private MediaInfoModel? _mediaInfo;

    //public ICommand ActivateCommand { get; set; }
    public MediaInfoViewModel(MediaInfoModel? mediaInfo)
    {
        //ActivateCommand = new RelayCommand(OnActivate);

        if (mediaInfo is null) return;
        MediaInfo = mediaInfo;
        OnPropertyChanged(nameof(MediaInfo));
    }

    private void OnActivate()
    {
        //WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(this));
    }

    public void Play(VideoSourceModel videoSource)
    {
        Debug.WriteLine(videoSource.Url);
    }

    public void VlcPlay(VideoSourceModel videoSource)
    {
        Debug.WriteLine(videoSource.Url);
    }

    public void MpvPlay(VideoSourceModel videoSource)
    {
        Debug.WriteLine(videoSource.Url);
    }
}