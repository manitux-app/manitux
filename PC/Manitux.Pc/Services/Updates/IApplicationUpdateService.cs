using System.ComponentModel;

namespace Manitux.Services.Updates;

public interface IApplicationUpdateService : INotifyPropertyChanged, IDisposable
{
    event EventHandler? UpdateAvailable;

    string CurrentVersion { get; }
    string? LatestVersion { get; }
    string? LatestReleaseName { get; }
    string? AssetName { get; }
    string? Changelog { get; }
    string StatusMessage { get; }
    bool IsUpdateAvailable { get; }
    bool IsBusy { get; }
    bool IsChecking { get; }
    bool IsDownloading { get; }
    bool IsInstalling { get; }
    double DownloadedPercentage { get; }
    string DownloadProgressText { get; }

    Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<bool> DownloadAndInstallUpdateAsync(CancellationToken cancellationToken = default);
}
