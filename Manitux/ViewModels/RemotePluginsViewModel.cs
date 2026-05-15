using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manitux.Core.Services.Plugins;

namespace Manitux.ViewModels;

public partial class RemotePluginsViewModel : ViewModelBase
{
    private readonly IRemotePluginService _remotePluginService;

    [ObservableProperty] private string? _repositoryInput;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Information;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<ManagedRemoteRepository> _repositories = [];
    [ObservableProperty] private ObservableCollection<RemotePluginManifest> _availablePlugins = [];
    [ObservableProperty] private ObservableCollection<ManagedRemotePlugin> _installedPlugins = [];

    public RemotePluginsViewModel(IRemotePluginService remotePluginService)
    {
        _remotePluginService = remotePluginService;
        _ = Refresh();
    }

    [RelayCommand]
    private async Task AddRepository()
    {
        if (string.IsNullOrWhiteSpace(RepositoryInput))
        {
            SetStatus("Repository URL or short code is required.", NotificationType.Warning);
            return;
        }

        await RunBusy(async () =>
        {
            var repository = await _remotePluginService.AddRepositoryAsync(RepositoryInput);
            await LoadRepositoryPlugins(repository.Url);
            await RefreshSettings();
            SetStatus($"Repository added: {repository.Name}", NotificationType.Success);
        });
    }

    [RelayCommand]
    private async Task LoadRepository(ManagedRemoteRepository? repository)
    {
        if (repository is null) return;

        RepositoryInput = repository.Url;
        await RunBusy(async () =>
        {
            await LoadRepositoryPlugins(repository.Url);
            SetStatus($"Repository loaded: {repository.Name}", NotificationType.Success);
        });
    }

    [RelayCommand]
    private async Task Install(RemotePluginManifest? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        var source = RepositoryInput;
        if (string.IsNullOrWhiteSpace(source))
        {
            source = plugin.RepositoryUrl ?? plugin.Url;
        }

        await RunBusy(async () =>
        {
            var result = await _remotePluginService.InstallAsync(source, plugin.InternalName);
            await RefreshSettings();
            SetStatus(result.Message, result.Success ? NotificationType.Success : NotificationType.Error);
        });
    }

    [RelayCommand]
    private async Task Update(ManagedRemotePlugin? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        await RunBusy(async () =>
        {
            var result = await _remotePluginService.UpdateAsync(plugin.InternalName);
            await RefreshSettings();
            SetStatus(result.Message, result.Success ? NotificationType.Success : NotificationType.Error);
        });
    }

    [RelayCommand]
    private async Task Remove(ManagedRemotePlugin? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        await RunBusy(async () =>
        {
            var removed = await _remotePluginService.RemoveAsync(plugin.InternalName);
            await RefreshSettings();
            SetStatus(
                removed ? $"Plugin removed: {plugin.Name}" : "Plugin was not found.",
                removed ? NotificationType.Success : NotificationType.Warning);
        });
    }

    [RelayCommand]
    private async Task UpdateAll()
    {
        await RunBusy(async () =>
        {
            var results = await _remotePluginService.UpdateAllAsync();
            await RefreshSettings();
            var updated = results.Count(x => x.Success);
            SetStatus($"Update check completed. {updated}/{results.Count} plugins processed.", NotificationType.Success);
        });
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await RunBusy(RefreshSettings);
    }

    private async Task LoadRepositoryPlugins(string repositoryUrl)
    {
        var plugins = await _remotePluginService.GetRepositoryPluginsAsync(repositoryUrl);
        AvailablePlugins = new ObservableCollection<RemotePluginManifest>(plugins);
    }

    private async Task RefreshSettings()
    {
        var settings = await _remotePluginService.GetSettingsAsync();
        Repositories = new ObservableCollection<ManagedRemoteRepository>(
            settings.Repositories.OrderBy(x => x.Name));
        InstalledPlugins = new ObservableCollection<ManagedRemotePlugin>(
            settings.InstalledPlugins.OrderBy(x => x.Name));
    }

    private async Task RunBusy(Func<Task> action)
    {
        IsLoading = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, NotificationType.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetStatus(string message, NotificationType type)
    {
        StatusMessage = message;
        StatusType = type;
    }
}
