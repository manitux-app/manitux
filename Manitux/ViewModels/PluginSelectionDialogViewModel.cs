using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.Core.Application;
using Manitux.Core.Plugins;
using Manitux.Services.Localizations;

namespace Manitux.ViewModels;

public partial class PluginSelectionDialogViewModel : ViewModelBase, IDialogContext
{
    public PluginSelectionDialogViewModel(
        ILocalizationService localizationService,
        IEnumerable<PluginBase> plugins,
        string? currentPluginId)
    {
        L = localizationService.Strings;
        CurrentPluginId = currentPluginId;

        foreach (var plugin in plugins.OrderBy(x => x.Manifest.Name))
        {
            Plugins.Add(new PluginSelectionItemViewModel(
                plugin.Manifest.Id,
                plugin.Manifest.Name,
                plugin.Config.Language,
                plugin.Config.Favicon,
                string.Equals(plugin.Manifest.Id, currentPluginId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public AppStrings L { get; }
    public string? CurrentPluginId { get; }
    public ObservableCollection<PluginSelectionItemViewModel> Plugins { get; } = [];

    public event EventHandler<object?>? RequestClose;

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    [RelayCommand]
    private void Select(PluginSelectionItemViewModel? plugin)
    {
        if (plugin is null)
        {
            return;
        }

        RequestClose?.Invoke(this, plugin.Id);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, null);
    }
}

public sealed class PluginSelectionItemViewModel(
    string id,
    string name,
    string? language,
    string? favicon,
    bool isSelected)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string? Language { get; } = language;
    public string? Favicon { get; } = favicon;
    public bool IsSelected { get; } = isSelected;
    public string LanguageDisplay => string.IsNullOrWhiteSpace(Language) ? string.Empty : Language.ToUpperInvariant();
}
