using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Manitux.Services.Localizations;
using Updatum;

namespace Manitux.Services.Updates;

public sealed class ApplicationUpdateService : IApplicationUpdateService
{
    private const string RepositoryOwner = "manitux-app";
    private const string RepositoryName = "manitux";
    private const string ApplicationName = "Manitux";

    private readonly ILocalizationService _localizationService;
    private readonly UpdatumManager _updater;
    private bool _updateAvailableRaised;
    private string _statusMessage = string.Empty;

    public ApplicationUpdateService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _updater = new UpdatumManager(RepositoryOwner, RepositoryName)
        {
            AssetRegexPattern = $"^{ApplicationName}_{GetRuntimeIdentifier()}_v",
            AssetExtensionFilter = ".zip",
            InstallUpdateSingleFileExecutableNameStrategy = UpdatumSingleFileExecutableNameStrategy.EntryApplicationName,
            InstallUpdateSingleFileExecutableName = "Manitux.Desktop",
            InstallUpdateWindowsExeType = UpdatumWindowsExeType.SingleFileApp
        };

        _updater.PropertyChanged += UpdaterOnPropertyChanged;
        StatusMessage = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? UpdateAvailable;

    public string CurrentVersion => GetEntryVersion();
    public string? LatestVersion => _updater.LatestRelease?.TagName;
    public string? LatestReleaseName => _updater.LatestRelease?.Name ?? _updater.LatestRelease?.TagName;
    public string? AssetName => _updater.LatestRelease is null
        ? null
        : _updater.GetCompatibleReleaseAsset(_updater.LatestRelease)?.Name;
    public string? Changelog => _updater.GetChangelog(maxReleases: 5);
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsUpdateAvailable => _updater.IsUpdateAvailable;
    public bool IsBusy => _updater.IsBusy;
    public bool IsChecking => _updater.State == UpdatumState.CheckingForUpdate;
    public bool IsDownloading => _updater.State == UpdatumState.DownloadingUpdate;
    public bool IsInstalling => _updater.State == UpdatumState.InstallingUpdate;
    public double DownloadedPercentage => _updater.DownloadedPercentage;
    public string DownloadProgressText => IsDownloading
        ? $"{_updater.DownloadedMegabytes:0.##} MB / {_updater.DownloadSizeMegabytes:0.##} MB ({_updater.DownloadedPercentage:0.#}%)"
        : string.Empty;

    public async Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            StatusMessage = _localizationService.Strings.CheckForUpdates;
            var hasUpdate = await _updater.CheckForUpdatesAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            StatusMessage = hasUpdate
                ? string.Format(_localizationService.Strings.ApplicationUpdateFoundFormat, LatestVersion ?? LatestReleaseName)
                : _localizationService.Strings.NoApplicationUpdateFound;

            RaiseAllProperties();

            if (hasUpdate && !_updateAvailableRaised)
            {
                _updateAvailableRaised = true;
                await Dispatcher.UIThread.InvokeAsync(() => UpdateAvailable?.Invoke(this, EventArgs.Empty));
            }

            return hasUpdate;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            StatusMessage = string.Format(_localizationService.Strings.ApplicationUpdateFailedFormat, ex.Message);
            RaiseAllProperties();
            return false;
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsUpdateAvailable)
            {
                var hasUpdate = await CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);
                if (!hasUpdate)
                {
                    return false;
                }
            }

            StatusMessage = _localizationService.Strings.ApplicationUpdateStarting;
            var downloaded = await _updater.DownloadUpdateAsync(cancellationToken).ConfigureAwait(false);
            if (downloaded is null)
            {
                StatusMessage = _localizationService.Strings.DownloadFailed;
                return false;
            }

            return await _updater.InstallUpdateAsync(downloaded).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            StatusMessage = string.Format(_localizationService.Strings.ApplicationUpdateFailedFormat, ex.Message);
            RaiseAllProperties();
            return false;
        }
    }

    public void Dispose()
    {
        _updater.PropertyChanged -= UpdaterOnPropertyChanged;
        _updater.Dispose();
    }

    private void UpdaterOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UpdatumManager.State) or nameof(UpdatumManager.DownloadedPercentage) or nameof(UpdatumManager.DownloadedBytes))
        {
            RaiseAllProperties();
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }

    private void RaiseAllProperties()
    {
        OnPropertyChanged(nameof(CurrentVersion));
        OnPropertyChanged(nameof(LatestVersion));
        OnPropertyChanged(nameof(LatestReleaseName));
        OnPropertyChanged(nameof(AssetName));
        OnPropertyChanged(nameof(Changelog));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsChecking));
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsInstalling));
        OnPropertyChanged(nameof(DownloadedPercentage));
        OnPropertyChanged(nameof(DownloadProgressText));
    }

    private static string GetRuntimeIdentifier()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

        if (OperatingSystem.IsWindows()) return $"win-{arch}";
        if (OperatingSystem.IsMacOS()) return $"osx-{arch}";
        return $"linux-{arch}";
    }

    private static string GetEntryVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString();

        return string.IsNullOrWhiteSpace(version) ? "0.0.0" : version;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
