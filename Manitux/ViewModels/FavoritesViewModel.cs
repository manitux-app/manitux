using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Application;
using Manitux.Core.Models;
using Manitux.Core.Services.Plugins;
using Manitux.Models;
using Manitux.Services.Favorites;
using Manitux.Services.Localizations;
using Manitux.Services.Plugins;

namespace Manitux.ViewModels;

public partial class FavoritesViewModel : ViewModelBase
{
    private readonly IFavoritesService _favoritesService;

    public AppStrings L { get; }

    [ObservableProperty]
    private ObservableCollection<PageItemModel>? _pageItems;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private PluginTopBarViewModel? _topBar;

    [ObservableProperty]
    private bool _isVisible;

    public event Action? OnDataRefreshed;

    public FavoritesViewModel(
        IFavoritesService favoritesService,
        IPluginService pluginService,
        ILocalizationService localizationService,
        IRemotePluginService remotePluginService)
    {
        _favoritesService = favoritesService;
        L = localizationService.Strings;
        TopBar = new PluginTopBarViewModel(
            pluginService,
            localizationService,
            remotePluginService,
            Search,
            RefreshPageItems,
            L.Favorites,
            null,
            isPluginConfigVisible: false,
            isRefreshVisible: false);

        _ = RefreshPageItems();
    }

    public void OnActivate(PageItemModel pageItem)
    {
        WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(pageItem));
    }

    [RelayCommand]
    private async Task ClearSearch()
    {
        if (TopBar is not null)
        {
            TopBar.SearchText = null;
        }

        await RefreshPageItems();
    }

    public async Task<bool> Search(string? query)
    {
        IsLoading = true;

        try
        {
            var results = await _favoritesService.SearchAsync(query);
            UpdatePageItems(results.Select(x => x.ToPageItem()).ToList());
            return PageItems?.Any() == true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshPageItems()
    {
        IsLoading = true;

        try
        {
            var favorites = await _favoritesService.GetFavoritesAsync();
            UpdatePageItems(favorites.Select(x => x.ToPageItem()).ToList());
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdatePageItems(List<PageItemModel>? pageItems)
    {
        PageItems = pageItems is null
            ? null
            : new ObservableCollection<PageItemModel>(pageItems);

        IsVisible = PageItems?.Any() == true;
        OnDataRefreshed?.Invoke();
    }
}
