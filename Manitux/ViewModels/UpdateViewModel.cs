using System.ComponentModel;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manitux.Core.Application;
using Manitux.Services.Localizations;
using Manitux.Services.Notifications;
using Manitux.Services.Updates;

namespace Manitux.ViewModels;

public partial class UpdateViewModel : ViewModelBase
{
    private readonly IApplicationUpdateService _updateService;
    private readonly INotificationService _notificationService;

    public UpdateViewModel(
        IApplicationUpdateService updateService,
        ILocalizationService localizationService,
        INotificationService notificationService)
    {
        _updateService = updateService;
        _notificationService = notificationService;
        L = localizationService.Strings;
        _updateService.PropertyChanged += UpdateServiceOnPropertyChanged;
    }

    public AppStrings L { get; }
    public string CurrentVersion => _updateService.CurrentVersion;
    public string? LatestVersion => _updateService.LatestVersion;
    public string? LatestReleaseName => _updateService.LatestReleaseName;
    public string? AssetName => _updateService.AssetName;
    public string? Changelog => _updateService.Changelog;
    public string StatusMessage => _updateService.StatusMessage;
    public bool IsUpdateAvailable => _updateService.IsUpdateAvailable;
    public bool IsBusy => _updateService.IsBusy;
    public bool IsChecking => _updateService.IsChecking;
    public bool IsDownloading => _updateService.IsDownloading;
    public bool IsInstalling => _updateService.IsInstalling;
    public double DownloadedPercentage => _updateService.DownloadedPercentage;
    public string DownloadProgressText => _updateService.DownloadProgressText;
    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasChangelog => !string.IsNullOrWhiteSpace(Changelog);
    public NotificationType StatusType => IsUpdateAvailable ? NotificationType.Success : NotificationType.Information;

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        await _updateService.CheckForUpdatesAsync();
    }

    [RelayCommand]
    private async Task StartUpdate()
    {
        if (!IsUpdateAvailable)
        {
            await CheckForUpdates();
            if (!IsUpdateAvailable)
            {
                return;
            }
        }

        _notificationService.ShowInfo(L.ApplicationUpdateStarting, L.ApplicationUpdate, true, true);
        await _updateService.DownloadAndInstallUpdateAsync();
    }

    private void UpdateServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CurrentVersion));
        OnPropertyChanged(nameof(LatestVersion));
        OnPropertyChanged(nameof(LatestReleaseName));
        OnPropertyChanged(nameof(AssetName));
        OnPropertyChanged(nameof(Changelog));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsChecking));
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsInstalling));
        OnPropertyChanged(nameof(DownloadedPercentage));
        OnPropertyChanged(nameof(DownloadProgressText));
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(HasChangelog));
        OnPropertyChanged(nameof(StatusType));
    }
}
