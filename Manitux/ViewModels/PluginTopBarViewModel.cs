using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manitux.Core.Application;
using Manitux.Core.Plugins;
using Manitux.Core.Services.Plugins;
using Manitux.Pages;
using Manitux.Services.Localizations;
using Manitux.Services.Plugins;
using Ursa.Controls;

namespace Manitux.ViewModels;

public partial class PluginTopBarViewModel : ViewModelBase
{
    private readonly IPluginService _pluginService;
    private readonly ILocalizationService _localizationService;
    private readonly IRemotePluginService _remotePluginService;
    private readonly Func<string?, Task<bool>> _searchHandler;
    private readonly Func<Task> _refreshHandler;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private string? _pluginName;
    [ObservableProperty] private string? _pluginFavicon;
    [ObservableProperty] private bool _isVisible = false;

    public AppStrings L { get; }

    public PluginTopBarViewModel(
        IPluginService pluginService,
        ILocalizationService localizationService,
        IRemotePluginService remotePluginService,
        Func<string?, Task<bool>> searchHandler,
        Func<Task> refreshHandler)
    {
        _pluginService = pluginService;
        _localizationService = localizationService;
        _remotePluginService = remotePluginService;
        _searchHandler = searchHandler;
        _refreshHandler = refreshHandler;
        L = _localizationService.Strings;

        UpdatePluginInfo();
    }

    [RelayCommand]
    private async Task Search()
    {
        await _searchHandler(SearchText);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        IsLoading = true;
        try
        {
            await UpdateCurrentPluginIfAvailable();
            await _refreshHandler();
            UpdatePluginInfo();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowPluginConfig()
    {
        if (_pluginService.CurrentPlugin is null)
        {
            return;
        }

        var options = new OverlayDialogOptions
        {
            Buttons = DialogButton.None,
            Mode = DialogMode.None,
            CanDragMove = false,
            CanResize = false,
            FullScreen = false,
            Title = $"{_pluginService.CurrentPlugin.Manifest.Name} config.json"
        };

        await OverlayDialog.ShowCustomModal<PluginConfigEditor, PluginConfigEditorViewModel, object>(
            new PluginConfigEditorViewModel(_pluginService.CurrentPlugin),
            null,
            options: options);

        UpdatePluginInfo();
    }

    public void UpdatePluginInfo()
    {
        PluginName = _pluginService.CurrentPlugin?.Manifest.Name;
        PluginFavicon = _pluginService.CurrentPlugin?.Config.Favicon;
        IsVisible = _pluginService.CurrentPlugin is null? false: true;
    }

    private async Task UpdateCurrentPluginIfAvailable()
    {
        if (_pluginService.CurrentPlugin is null)
        {
            return;
        }

        try
        {
            var plugin = _pluginService.CurrentPlugin;
            var settings = await _remotePluginService.GetSettingsAsync();
            var installed = settings.InstalledPlugins.FirstOrDefault(x =>
                string.Equals(x.InternalName, plugin.Manifest.Id, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.InternalName, plugin.Manifest.Name, StringComparison.OrdinalIgnoreCase));

            if (installed is null)
            {
                return;
            }

            var result = await _remotePluginService.UpdateAsync(installed.InternalName);
            Debug.WriteLine($"[PluginTopBarViewModel] Plugin update check: {installed.InternalName} - {result.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginTopBarViewModel] Plugin update check failed: {ex}");
        }
    }
}
