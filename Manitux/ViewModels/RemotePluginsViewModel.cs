using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Application;
using Manitux.Core.Services.Plugins;
using Manitux.Models;
using Manitux.Services.Localizations;
using Manitux.Services.Plugins;

namespace Manitux.ViewModels;

public partial class RemotePluginsViewModel : ViewModelBase
{
    private readonly IRemotePluginService _remotePluginService;
    private readonly IPluginService? _pluginService;
    public AppStrings L { get; }

    private PluginManager? _pluginManager;

    [ObservableProperty] private string? _repositoryInput;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Information;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<ManagedRemoteRepository> _repositories = [];
    [ObservableProperty] private ObservableCollection<RemotePluginRepositoryGroup> _repositoryPluginGroups = [];
    [ObservableProperty] private ObservableCollection<RemotePluginManifest> _availablePlugins = [];
    [ObservableProperty] private ObservableCollection<ManagedRemotePlugin> _installedPlugins = [];

    public RemotePluginsViewModel(
        IRemotePluginService remotePluginService,
        ILocalizationService localizationService,
        IPluginService? pluginService = null)
    {
        _remotePluginService = remotePluginService;
        _pluginService = pluginService;
        L = localizationService.Strings;
        _pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        _ = Refresh();
    }

    [RelayCommand]
    private async Task AddRepository()
    {
        if (string.IsNullOrWhiteSpace(RepositoryInput))
        {
            SetStatus(L.RepositoryRequired, NotificationType.Warning);
            return;
        }

        await RunBusy(async () =>
        {
            var repository = await _remotePluginService.AddRepositoryAsync(RepositoryInput);
            await RefreshSettings();
            SetStatus(string.Format(L.RepositoryAddedFormat, repository.Name), NotificationType.Success);
        });
    }

    [RelayCommand]
    private async Task LoadRepository(ManagedRemoteRepository? repository)
    {
        if (repository is null) return;

        RepositoryInput = repository.Url;
        await RunBusy(async () =>
        {
            await RefreshSettings();
            SetStatus(string.Format(L.RepositoryLoadedFormat, repository.Name), NotificationType.Success);
        });
    }

    [RelayCommand]
    private async Task RemoveRepository(ManagedRemoteRepository? repository)
    {
        if (repository is null)
        {
            return;
        }

        //Debug.WriteLine(repository.Url);

        await RunBusy(async () =>
        {
            await UnloadPlugins();

            var removed = await _remotePluginService.RemoveRepositoryAsync(repository.Url);
            await RefreshSettings();
            SetStatus(
                removed ? string.Format(L.RepositoryRemovedFormat, repository.Name) : L.PluginWasNotFound,
                removed ? NotificationType.Success : NotificationType.Warning);
        });
    }

    [RelayCommand]
    private async Task Install(RemotePluginManifest? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        var source = plugin.RepositoryUrl ?? plugin.Url;

        await RunBusy(async () =>
        {
            var result = await _remotePluginService.InstallAsync(source, plugin.InternalName);
            await RefreshSettings();
            SetStatus(result.Message, result.Success ? NotificationType.Success : NotificationType.Error);
        });
    }

    [RelayCommand(CanExecute = nameof(CanInstallGroup))]
    private async Task InstallGroup(RemotePluginPackageGroup? group)
    {
        if (group is null)
        {
            return;
        }

        await RunBusy(async () =>
        {
            var installed = 0;
            foreach (var plugin in group.Plugins)
            {
                var result = await _remotePluginService.InstallAsync(plugin);
                if (result.Success)
                {
                    installed++;
                }
            }

            await ReloadPlugins();
            await RefreshSettings();
            var statusType = installed == group.Plugins.Count
                ? NotificationType.Success
                : installed == 0 ? NotificationType.Error : NotificationType.Warning;
            SetStatus(string.Format(L.PluginsInstalledFormat, installed, group.Plugins.Count), statusType);
        });
    }

    private static bool CanInstallGroup(RemotePluginPackageGroup? group)
    {
        return group is not null && !group.IsInstalled;
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
            var check = await _remotePluginService.CheckUpdateAsync(plugin.InternalName, plugin.PackageInternalName);
            if (!check.HasUpdate)
            {
                await RefreshSettings();
                SetStatus(check.Message, NotificationType.Information);
                return;
            }

            await UnloadPlugins();

            try
            {
                var result = await _remotePluginService.UpdateAsync(plugin.InternalName, plugin.PackageInternalName);
                await RefreshSettings();
                SetStatus(result.Message, result.Success ? NotificationType.Success : NotificationType.Error);
            }
            finally
            {
                await LoadPlugins();
            }
        });
    }

    [RelayCommand]
    private async Task UpdateGroup(RemotePluginPackageGroup? group)
    {
        if (group is null)
        {
            return;
        }

        await RunBusy(async () =>
        {
            if (group.InstalledPlugins.Count == 0)
            {
                SetStatus(L.PluginWasNotFound, NotificationType.Warning);
                return;
            }

            var updates = group.GetPluginUpdates().ToList();
            if (updates.Count == 0)
            {
                await RefreshSettings();
                SetStatus(string.Format(L.UpdateCheckCompletedFormat, 0, group.InstalledPlugins.Count), NotificationType.Information);
                return;
            }

            await UnloadPlugins();

            try
            {
                var processed = 0;
                foreach (var update in updates)
                {
                    var result = await _remotePluginService.InstallAsync(update);
                    if (result.Success)
                    {
                        processed++;
                    }
                }

                await RefreshSettings();
                SetStatus(string.Format(L.UpdateCheckCompletedFormat, processed, group.InstalledPlugins.Count), NotificationType.Success);
            }
            finally
            {
                await LoadPlugins();
            }
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
                removed ? string.Format(L.PluginRemovedFormat, plugin.Name) : L.PluginWasNotFound,
                removed ? NotificationType.Success : NotificationType.Warning);
        });
    }

    [RelayCommand]
    private async Task RemoveGroup(RemotePluginPackageGroup? group)
    {
        if (group is null)
        {
            return;
        }

        await RunBusy(async () =>
        {
            await UnloadPlugins();

            try
            {
                var removed = 0;
                foreach (var plugin in group.InstalledPlugins.ToList())
                {
                    if (await _remotePluginService.RemoveAsync(plugin.InternalName, plugin.PackageInternalName))
                    {
                        removed++;
                    }
                }

                await RefreshSettings();
                SetStatus(
                    removed > 0 ? string.Format(L.PluginsRemovedFormat, removed) : L.PluginWasNotFound,
                    removed > 0 ? NotificationType.Success : NotificationType.Warning);
            }
            finally
            {
                await LoadPlugins();
            }
        });
    }

    [RelayCommand]
    private async Task SavePluginSelections(RemotePluginRepositoryGroup? repositoryGroup)
    {
        if (repositoryGroup is null)
        {
            return;
        }

        var states = repositoryGroup.PluginGroups
            .SelectMany(group => group.Plugins.Where(plugin => plugin.IsInstalled).Select(plugin => new RemotePluginEnabledState
            {
                InternalName = plugin.InternalName,
                PackageInternalName = GetPackageKey(plugin),
                IsEnabled = plugin.IsEnabled
            }))
            .ToList();

        await RunBusy(async () =>
        {
            var changed = await _remotePluginService.SetEnabledStatesAsync(states);

            if (changed > 0)
            {
                await ReloadPlugins();
            }

            await RefreshSettings();
            SetStatus(L.Success, NotificationType.Success);
        });
    }

    [RelayCommand]
    private async Task UpdateAll()
    {
        await RunBusy(async () =>
        {
            var checks = await _remotePluginService.CheckUpdatesAsync();
            var updates = checks.Where(x => x.HasUpdate).ToList();
            if (updates.Count == 0)
            {
                await RefreshSettings();
                SetStatus(string.Format(L.UpdateCheckCompletedFormat, 0, checks.Count), NotificationType.Information);
                return;
            }

            await UnloadPlugins();

            try
            {
                var results = new System.Collections.Generic.List<RemotePluginInstallResult>();
                foreach (var update in updates)
                {
                    results.Add(await _remotePluginService.UpdateAsync(
                        update.InstalledPlugin.InternalName,
                        update.InstalledPlugin.PackageInternalName));
                }

                await RefreshSettings();
                var updated = results.Count(x => x.Success);
                SetStatus(string.Format(L.UpdateCheckCompletedFormat, updated, checks.Count), NotificationType.Success);
            }
            finally
            {
                await LoadPlugins();
            }
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
        var repositories = settings.Repositories.OrderBy(x => x.Name).ToList();
        var groups = new ObservableCollection<RemotePluginRepositoryGroup>();

        foreach (var repository in repositories)
        {
            var plugins = await _remotePluginService.GetRepositoryPluginsAsync(repository.Url);
            var packageGroups = plugins
                .GroupBy(GetPackageKey)
                .Select(x =>
                {
                    var orderedPlugins = x.OrderBy(p => p.Name).ToList();
                    var installedPlugins = settings.InstalledPlugins
                        .Where(p => orderedPlugins.Any(available =>
                            string.Equals(available.InternalName, p.InternalName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(p.PackageInternalName, GetPackageKey(available), StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(p => p.Name)
                        .ToList();

                    foreach (var plugin in orderedPlugins)
                    {
                        var installed = installedPlugins.FirstOrDefault(installed =>
                            string.Equals(installed.InternalName, plugin.InternalName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(installed.PackageInternalName, GetPackageKey(plugin), StringComparison.OrdinalIgnoreCase));

                        plugin.Strings = L;
                        plugin.IsInstalled = installed is not null;
                        plugin.IsEnabled = installed?.IsEnabled ?? true;
                    }

                    return new RemotePluginPackageGroup(
                        x.Key,
                        orderedPlugins[0],
                        new ObservableCollection<RemotePluginManifest>(orderedPlugins),
                        new ObservableCollection<ManagedRemotePlugin>(installedPlugins),
                        L);
                })
                .OrderBy(x => x.Name)
                .ToList();

            groups.Add(new RemotePluginRepositoryGroup(
                repository,
                new ObservableCollection<RemotePluginPackageGroup>(packageGroups)));
        }

        Repositories = new ObservableCollection<ManagedRemoteRepository>(repositories);
        RepositoryPluginGroups = groups;
        AvailablePlugins = new ObservableCollection<RemotePluginManifest>(
            groups.SelectMany(x => x.PluginGroups).SelectMany(x => x.Plugins).OrderBy(x => x.Name));
        InstalledPlugins = new ObservableCollection<ManagedRemotePlugin>(
            settings.InstalledPlugins.OrderBy(x => x.Name));
        InstallGroupCommand.NotifyCanExecuteChanged();
    }

    private static string GetPackageKey(RemotePluginManifest plugin)
    {
        if (!string.IsNullOrWhiteSpace(plugin.PackageInternalName))
        {
            return plugin.PackageInternalName;
        }

        if (!string.IsNullOrWhiteSpace(plugin.PackageName))
        {
            return plugin.PackageName;
        }

        return string.IsNullOrWhiteSpace(plugin.Url) ? plugin.InternalName : plugin.Url;
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

    private async Task ReloadPlugins()
    {
        await UnloadPlugins();
        await LoadPlugins();
    }

    private async Task UnloadPlugins()
    {
        _pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        if (_pluginManager is null)
        {
            return;
        }

        WeakReferenceMessenger.Default.Send(new PluginCatalogReloadingMessage(true));

        if (_pluginService is not null)
        {
            _pluginService.CurrentPlugin = null;
        }

        await _pluginManager.UnloadAllAsync();
        await Task.Delay(250);
    }

    private async Task LoadPlugins()
    {
        _pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        if (_pluginManager is null)
        {
            return;
        }

        var settings = await _remotePluginService.GetSettingsAsync();
        var enabledPlugins = settings.InstalledPlugins
            .Where(x => x.IsEnabled)
            .Select(x => x.InternalName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasPluginSettings = settings.InstalledPlugins.Count > 0;

        _pluginManager.SetPluginEnabledFilter((manifest, _) =>
            !hasPluginSettings || enabledPlugins.Contains(manifest.Id));

        await _pluginManager.LoadAllAsync();
        WeakReferenceMessenger.Default.Send(new PluginCatalogChangedMessage(true));
    }
}

public sealed class RemotePluginRepositoryGroup
{
    public RemotePluginRepositoryGroup(
        ManagedRemoteRepository repository,
        ObservableCollection<RemotePluginPackageGroup> pluginGroups)
    {
        Repository = repository;
        PluginGroups = pluginGroups;
    }

    public ManagedRemoteRepository Repository { get; }
    public ObservableCollection<RemotePluginPackageGroup> PluginGroups { get; }
}

public sealed class RemotePluginPackageGroup
{
    public RemotePluginPackageGroup(
        string internalName,
        RemotePluginManifest package,
        ObservableCollection<RemotePluginManifest> plugins,
        ObservableCollection<ManagedRemotePlugin> installedPlugins,
        AppStrings strings)
    {
        InternalName = internalName;
        Name = string.IsNullOrWhiteSpace(package.PackageName) ? internalName : package.PackageName;
        Version = package.Version;
        ApiVersion = package.ApiVersion;
        Authors = package.Authors;
        Plugins = plugins;
        InstalledPlugins = installedPlugins;
        L = strings;
    }

    public string InternalName { get; }
    public string Name { get; }
    public int Version { get; }
    public int ApiVersion { get; }
    public System.Collections.Generic.IReadOnlyList<string> Authors { get; }
    public ObservableCollection<RemotePluginManifest> Plugins { get; }
    public ObservableCollection<ManagedRemotePlugin> InstalledPlugins { get; }
    public bool IsInstalled => Plugins.Count > 0 && InstalledPlugins.Count == Plugins.Count;
    public bool HasUpdate => GetPluginUpdates().Any();
    public string PluginCountText => string.Format(L.PluginsCountFormat, Plugins.Count);
    public string InstalledStatus => string.Format(L.InstalledStatusFormat, InstalledPlugins.Count, Plugins.Count);
    public AppStrings L { get; }

    public System.Collections.Generic.IEnumerable<RemotePluginManifest> GetPluginUpdates()
    {
        foreach (var plugin in Plugins)
        {
            var installed = InstalledPlugins.FirstOrDefault(x =>
                string.Equals(x.InternalName, plugin.InternalName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.PackageInternalName, InternalName, StringComparison.OrdinalIgnoreCase));

            if (installed is not null && plugin.Version > installed.Version)
            {
                yield return plugin;
            }
        }
    }
}
