using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
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

public partial class PageItemsViewModel : ViewModelBase, IDisposable
{
    private readonly IPluginService? _pluginService;
    private PluginManager? _pluginManager;
    private MenuItemViewModel? _navigation;
    
    [ObservableProperty]
    private ObservableCollection<PageItemModel>? _pageItems;

    [ObservableProperty]
    private ObservableCollection<PageItemCategoryViewModel> _categoryRows = [];

    //private PluginManager? pluginManager;
    private bool _suppressPageChange;
    private int _loadVersion;
    private bool _hasRefreshedCurrentLoad;
    private readonly SemaphoreSlim _categoryLoadLimiter = new(2, 2);
    private CancellationTokenSource? _loadCancellation;

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

    public ICommand ActivateCommand { get; }

    public PageItemsViewModel(List<PageItemModel>? pageItems, int currentPage = 1, bool isPaginationVisible = true)
    {
        ActivateCommand = new RelayCommand<PageItemModel>(OnActivate);
        //pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        //WeakReferenceMessenger.Default.Register<PageItemsViewModel, MenuItemChangedMessage>(this, OnNavigation);

        _suppressPageChange = true;
        CurrentPage = Math.Max(1, currentPage);
        _suppressPageChange = false;
        IsPaginationVisible = isPaginationVisible;

        UpdatePageItems(pageItems, navigationTitle: null);
    }

    public PageItemsViewModel(
        IPluginService pluginService,
        ILocalizationService localizationService,
        IRemotePluginService remotePluginService,
        MenuItemViewModel? navigation)
    {
        ActivateCommand = new RelayCommand<PageItemModel>(OnActivate);
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

    public void OnActivate(PageItemModel? pageItem)
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

    public void UpdatePageItems(List<PageItemModel>? pageItems, string? navigationTitle = null)
    {
        PageItems = pageItems is null
            ? null
            : new ObservableCollection<PageItemModel>(pageItems);

        CategoryRows = PageItems is null
            ? []
            : [new PageItemCategoryViewModel(navigationTitle ?? _navigation?.MenuHeader ?? string.Empty, PageItems)];

        IsVisible = CategoryRows.Any();

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

        CancelCurrentLoad();
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
            UpdatePageItems(results, "Search");
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
        if (pluginId is null)
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

        CancelCurrentLoad();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        IsLoading = true;
        _pluginService.CurrentPlugin = plugin;
        TopBar?.UpdatePluginInfo();
        _navigation.PageNumber = pageNumber;

        try
        {
            Debug.WriteLine($"Plugin: {JsonSerializer.Serialize(plugin.Manifest)}" + Environment.NewLine);
            if (category is null)
            {
                await LoadPluginCategoryRows(plugin, loadVersion, cancellationToken);
                return;
            }

            var pageItems = await plugin.GetPageItems(pageNumber, category);
            if (loadVersion != _loadVersion)
            {
                return;
            }

            EnrichWithCurrentPlugin(pageItems);
            UpdatePageItems(pageItems, category.Title);
        }
        finally
        {
            if (loadVersion == _loadVersion)
            {
                IsLoading = false;
            }
        }
    }

    private async Task LoadPluginCategoryRows(
        PluginBase plugin,
        int loadVersion,
        CancellationToken cancellationToken)
    {
        var categories = await plugin.GetCategories();
        if (loadVersion != _loadVersion)
        {
            return;
        }

        if (categories is null || categories.Count == 0)
        {
            UpdatePageItems(null);
            return;
        }

        CategoryRows = new ObservableCollection<PageItemCategoryViewModel>(categories.Select(category =>
        {
            PageItemCategoryViewModel? row = null;
            row = new PageItemCategoryViewModel(
                category.Title ?? string.Empty,
                () => LoadCategoryRow(plugin, category, row!, loadVersion, cancellationToken));
            return row;
        }));
        PageItems = [];
        IsPaginationVisible = false;
        IsVisible = true;
        IsLoading = false;
        _hasRefreshedCurrentLoad = false;

        OnPropertyChanged(nameof(PageItems));

        // İlk raflar ekrana gelir gelmez hazırlanır; kalan raflar görsel ağaca
        // girdiklerinde PageItemShelf tarafından tembel olarak yüklenir.
        foreach (var row in CategoryRows.Take(2))
        {
            _ = row.EnsureLoadedAsync();
        }
    }

    private async Task LoadCategoryRow(
        PluginBase plugin,
        CategoryModel category,
        PageItemCategoryViewModel row,
        int loadVersion,
        CancellationToken cancellationToken)
    {
        await _categoryLoadLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var pageItems = await plugin.GetPageItems(1, category);
            cancellationToken.ThrowIfCancellationRequested();
            if (loadVersion != _loadVersion)
            {
                return;
            }

            EnrichWithCurrentPlugin(pageItems);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                row.SetPageItems(pageItems);

                if (!_hasRefreshedCurrentLoad &&
                    pageItems?.Count > 0 &&
                    ReferenceEquals(row, CategoryRows.FirstOrDefault()))
                {
                    _hasRefreshedCurrentLoad = true;
                    OnDataRefreshed?.Invoke();
                }

                if (pageItems is not null)
                {
                    foreach (var pageItem in pageItems)
                    {
                        PageItems?.Add(pageItem);
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Category '{category.Title}' could not be loaded: {exception}");
        }
        finally
        {
            _categoryLoadLimiter.Release();
        }
    }

    public void Dispose()
    {
        CancelCurrentLoad();
    }

    private void CancelCurrentLoad()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        Interlocked.Increment(ref _loadVersion);
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

public partial class PageItemCategoryViewModel : ObservableObject
{
    private readonly Func<Task>? _loader;
    private int _loadStarted;

    public PageItemCategoryViewModel(
        string title,
        ObservableCollection<PageItemModel>? pageItems = null,
        Func<Task>? loader = null)
    {
        Title = title;
        PageItems = pageItems ?? [];
        _loader = loader;
        IsLoading = loader is not null;
    }

    public PageItemCategoryViewModel(string title, Func<Task> loader)
        : this(title, null, loader)
    {
    }

    public string Title { get; }
    public ObservableCollection<PageItemModel> PageItems { get; }

    [ObservableProperty]
    private bool _isLoading;

    public async Task EnsureLoadedAsync()
    {
        if (_loader is null || Interlocked.Exchange(ref _loadStarted, 1) != 0)
        {
            return;
        }

        try
        {
            await _loader();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetPageItems(IEnumerable<PageItemModel>? pageItems)
    {
        PageItems.Clear();
        if (pageItems is null)
        {
            return;
        }

        foreach (var pageItem in pageItems)
        {
            PageItems.Add(pageItem);
        }
    }
}
