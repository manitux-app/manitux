using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Core.Services.Plugins;
using Manitux.Models;
using Manitux.Services.Localizations;
using Manitux.Services.Plugins;

namespace Manitux.ViewModels;

public partial class PageItemsViewModel :  ViewModelBase
{
    private readonly IPluginService? _pluginService;
    private PluginManager? _pluginManager;
    private MenuItemViewModel? _navigation;
    
    [ObservableProperty]
    private ObservableCollection<PageItemModel>? _pageItems;

    //private PluginManager? pluginManager;
    private bool _suppressPageChange;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private bool _isPaginationVisible = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private PluginTopBarViewModel? _topBar;

    [ObservableProperty] private bool _isVisible = false;

    public event Action? OnDataRefreshed;

    public PageItemsViewModel(List<PageItemModel>? pageItems, int currentPage = 1, bool isPaginationVisible = true)
    {
        //pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        //WeakReferenceMessenger.Default.Register<PageItemsViewModel, MenuItemChangedMessage>(this, OnNavigation);

        _suppressPageChange = true;
        CurrentPage = Math.Max(1, currentPage);
        _suppressPageChange = false;
        IsPaginationVisible = isPaginationVisible;

        UpdatePageItems(pageItems);
    }

    public PageItemsViewModel(
        IPluginService pluginService,
        ILocalizationService localizationService,
        IRemotePluginService remotePluginService,
        MenuItemViewModel? navigation)
    {
        _pluginService = pluginService;
        _navigation = navigation;
        TopBar = new PluginTopBarViewModel(
            pluginService,
            localizationService,
            remotePluginService,
            Search,
            RefreshPageItems);

        _suppressPageChange = true;
        CurrentPage = Math.Max(1, navigation?.PageNumber ?? 1);
        _suppressPageChange = false;
        IsPaginationVisible = true;

        _ = LoadPageItems(CurrentPage);
    }

    public void OnActivate(PageItemModel pageItem)
    {
        if (pageItem is null) return;
        WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(pageItem));
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private void GoPreviousPage()
    {
        CurrentPage--;
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private void GoFirstPage()
    {
        CurrentPage = 1;
    }

    [RelayCommand]
    private void GoNextPage()
    {
        CurrentPage++;
    }

    partial void OnCurrentPageChanged(int value)
    {
        GoPreviousPageCommand.NotifyCanExecuteChanged();
        GoFirstPageCommand.NotifyCanExecuteChanged();

        if (_suppressPageChange) return;

        var pageNumber = Math.Max(1, value);
        if (pageNumber != value)
        {
            CurrentPage = pageNumber;
            return;
        }

        _ = LoadPageItems(pageNumber);
    }

    private bool CanGoPreviousPage()
    {
        return CurrentPage > 1;
    }

    public void UpdatePageItems(List<PageItemModel>? pageItems)
    {
        PageItems = pageItems is null
            ? null
            : new ObservableCollection<PageItemModel>(pageItems);

        IsVisible = PageItems is null? false: true;

        OnPropertyChanged(nameof(PageItems));
        OnDataRefreshed?.Invoke();
    }

    public async Task<bool> Search(string? query)
    {
        query = query?.Trim();
        if (string.IsNullOrWhiteSpace(query) || _pluginService?.CurrentPlugin is null)
        {
            return false;
        }

        IsLoading = true;

        try
        {
            var results = await _pluginService.CurrentPlugin.GetSearchResults(query);
            if (results is null || !results.Any())
            {
                return false;
            }

            EnrichWithCurrentPlugin(results);
            _navigation = null;
            IsPaginationVisible = false;
            UpdatePageItems(results);
            return true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshPageItems()
    {
        if (_navigation is null)
        {
            return;
        }

        await LoadPageItems(CurrentPage);
    }

    private async Task LoadPageItems(int pageNumber)
    {
        if (_pluginService is null || _navigation is null)
        {
            return;
        }

        var pluginId = _navigation.PluginId;
        var category = _navigation.Category;
        if (pluginId is null || category is null)
        {
            UpdatePageItems(null);
            return;
        }

        _pluginManager ??= CodeLogic.CodeLogic.GetPluginManager();
        var plugin = _pluginManager?.GetPlugin<PluginBase>(pluginId);

        if (plugin is null || plugin.State != PluginState.Started)
        {
            UpdatePageItems(null);
            return;
        }

        IsLoading = true;
        _pluginService.CurrentPlugin = plugin;
        TopBar?.UpdatePluginInfo();
        _navigation.PageNumber = pageNumber;

        try
        {
            Debug.WriteLine($"Plugin: {JsonSerializer.Serialize(plugin.Manifest)}" + Environment.NewLine);
            var pageItems = await plugin.GetPageItems(pageNumber, category);
            EnrichWithCurrentPlugin(pageItems);
            UpdatePageItems(pageItems);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void EnrichWithCurrentPlugin(List<PageItemModel>? pageItems)
    {
        var plugin = _pluginService?.CurrentPlugin;
        if (plugin is null || pageItems is null)
        {
            return;
        }

        foreach (var item in pageItems)
        {
            item.PluginId ??= plugin.Manifest.Id;
            item.PluginName ??= plugin.Manifest.Name;
            item.PluginFavicon ??= plugin.Config.Favicon;
        }
    }



    //[RelayCommand] {Binding AvtivateCommand}
    //public void Activate(PageItemModel pageItem)
    //{
    //    if (pageItem == null) return;
    //    WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(pageItem));
    //}

    //private async void OnNavigation(PageItemsViewModel vm, MenuItemChangedMessage message)
    //{
    //    string key = message.Value.Key ?? "";
    //    string? pluginId = message.Value.PluginId ?? null;

    //    if (pluginId is not null)
    //    {
    //        var plugin = pluginManager?.GetPlugin<PluginBase>(pluginId);

    //        if (plugin is not null && plugin.State == PluginState.Started)
    //        {
    //            var cat = message.Value.Category;
    //            if (cat is null) return;
    //            var pageItems = await plugin.GetPageItems(1, cat);
    //            if (pageItems is null) return;
    //            Debug.WriteLine($"PageItems: {JsonSerializer.Serialize(pageItems)}" + Environment.NewLine);
    //            PageItems = new ObservableCollection<PageItemModel>(pageItems);

    //            //foreach(var item in pageItems)
    //            //{
    //            //    PageItems.Add(item);
    //            //}

    //            OnPropertyChanged(nameof(PageItems));
    //        }
    //    }
    //}
}
