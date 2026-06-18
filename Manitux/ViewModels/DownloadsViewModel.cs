using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Manitux.Core.Application;
using Manitux.Models;
using Manitux.Services.Downloads;
using Manitux.Services.Localizations;

namespace Manitux.ViewModels;

public partial class DownloadsViewModel : ViewModelBase
{
    private readonly IDownloadService _downloadService;

    public DownloadsViewModel(IDownloadService downloadService, ILocalizationService localizationService)
    {
        _downloadService = downloadService;
        L = localizationService.Strings;
        _downloadService.DownloadsChanged += DownloadServiceOnDownloadsChanged;
        _ = InitializeAsync();
    }

    public AppStrings L { get; }
    public ObservableCollection<DownloadItemModel> Downloads { get; } = [];

    public bool HasDownloads => Downloads.Count > 0;

    [RelayCommand]
    private void Start(DownloadItemModel? download)
    {
        if (download is null)
        {
            return;
        }

        RunCommandAsync(_downloadService.StartAsync(download.Id));
    }

    [RelayCommand]
    private void Pause(DownloadItemModel? download)
    {
        if (download is null)
        {
            return;
        }

        RunCommandAsync(_downloadService.PauseAsync(download.Id));
    }

    [RelayCommand]
    private void Remove(DownloadItemModel? download)
    {
        if (download is null)
        {
            return;
        }

        RunCommandAsync(_downloadService.RemoveAsync(download.Id));
    }

    private async Task InitializeAsync()
    {
        await _downloadService.InitializeAsync();
        RefreshDownloads();
    }

    private void DownloadServiceOnDownloadsChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RefreshDownloads);
            return;
        }

        RefreshDownloads();
    }

    private void RefreshDownloads()
    {
        Downloads.Clear();
        foreach (var download in _downloadService.Downloads.OrderByDescending(x => x.CreatedAt))
        {
            Downloads.Add(download);
        }

        OnPropertyChanged(nameof(HasDownloads));
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
    }

    private static void RunCommandAsync(Task task)
    {
        _ = task.ContinueWith(
            completedTask => Debug.WriteLine($"Download command failed: {completedTask.Exception}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }
}
