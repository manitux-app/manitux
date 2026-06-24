using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Manitux.Core.Application;
using Manitux.Core.Plugins;
using Manitux.Core.Services.Plugins;
using Manitux.Services.Storage;

namespace Manitux.Services.Settings;

public interface IAppSettingsService
{
    string ConfigPath { get; }
    AppConfig Settings { get; }
    Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppConfig settings, CancellationToken cancellationToken = default);
    Task SaveCurrentAsync(
        string language,
        string theme,
        PluginBase? currentPlugin,
        IReadOnlyCollection<ManagedRemotePlugin>? installedPlugins = null,
        CancellationToken cancellationToken = default);
}

public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string ConfigPath => AppDataPath.GetAppPath("config.json");

    public AppConfig Settings { get; private set; } = new();

    public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigPath))
        {
            Settings = new AppConfig();
            return Settings;
        }

        try
        {
            await using var stream = File.OpenRead(ConfigPath);
            Settings = await JsonSerializer.DeserializeAsync<AppConfig>(stream, JsonOptions, cancellationToken)
                       ?? new AppConfig();
        }
        catch
        {
            Settings = new AppConfig();
        }

        return Settings;
    }

    public async Task SaveAsync(AppConfig settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        Settings = settings;
    }

    public async Task SaveCurrentAsync(
        string language,
        string theme,
        PluginBase? currentPlugin,
        IReadOnlyCollection<ManagedRemotePlugin>? installedPlugins = null,
        CancellationToken cancellationToken = default)
    {
        var settings = Settings ?? new AppConfig();
        settings.Language = string.IsNullOrWhiteSpace(language) ? settings.Language : language;
        settings.Theme = string.IsNullOrWhiteSpace(theme) ? settings.Theme : theme;
        settings.CurrentPluginId = currentPlugin?.Manifest.Id ?? settings.CurrentPluginId;

        if (installedPlugins is not null)
        {
            settings.SelectedPlugins = installedPlugins
                .OrderBy(x => x.Name)
                .Select(plugin => CreateSelectedPlugin(plugin, currentPlugin))
                .ToList();
        }
        else if (currentPlugin is not null)
        {
            UpsertCurrentPlugin(settings.SelectedPlugins, currentPlugin);
        }

        await SaveAsync(settings, cancellationToken);
    }

    private static AppSelectedPluginConfig CreateSelectedPlugin(ManagedRemotePlugin plugin, PluginBase? currentPlugin)
    {
        var isCurrent = currentPlugin is not null
                        && string.Equals(plugin.InternalName, currentPlugin.Manifest.Id, StringComparison.OrdinalIgnoreCase);

        return new AppSelectedPluginConfig
        {
            Id = plugin.InternalName,
            Name = plugin.Name,
            PackageInternalName = plugin.PackageInternalName,
            Version = plugin.Version.ToString(),
            Language = plugin.Manifest?.Language,
            Favicon = plugin.Manifest?.IconUrl,
            IsEnabled = plugin.IsEnabled,
            IsCurrent = isCurrent
        };
    }

    private static void UpsertCurrentPlugin(List<AppSelectedPluginConfig> selectedPlugins, PluginBase currentPlugin)
    {
        foreach (var plugin in selectedPlugins)
        {
            plugin.IsCurrent = false;
        }

        var existing = selectedPlugins.FirstOrDefault(x =>
            string.Equals(x.Id, currentPlugin.Manifest.Id, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            selectedPlugins.Add(new AppSelectedPluginConfig
            {
                Id = currentPlugin.Manifest.Id,
                Name = currentPlugin.Manifest.Name,
                Version = currentPlugin.Manifest.Version.ToString(),
                Language = currentPlugin.Config.Language,
                Favicon = currentPlugin.Config.Favicon,
                IsEnabled = true,
                IsCurrent = true
            });
            return;
        }

        existing.Name = currentPlugin.Manifest.Name;
        existing.Version = currentPlugin.Manifest.Version.ToString();
        existing.Language = currentPlugin.Config.Language;
        existing.Favicon = currentPlugin.Config.Favicon;
        existing.IsCurrent = true;
    }
}
