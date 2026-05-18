using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.Threading;
using CodeLogic.Core.Localization;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Application;
using Manitux.Core.Framework;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Core.Services.Plugins;
using Manitux.Models;
using Manitux.Pages;
using Manitux.Player;
using Manitux.Services.Favorites;
using Manitux.Services.Localizations;
using Manitux.Services.Notifications;
using Manitux.Services.Plugins;
using Manitux.Views;
using Semi.Avalonia;
using TlsClient.Api;
using TlsClient.Native;
using Ursa.Controls;
using static Manitux.Core.Helpers.LogHelper;
using SemiTheme = Semi.Avalonia.SemiTheme;

namespace Manitux.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Manitux!";

    private readonly IToastService _toastService;
    private readonly INotificationService _notificationService;
    private readonly IPluginService _pluginService;
    private readonly ILocalizationService _localizationService;
    private readonly IRemotePluginService _remotePluginService;
    private readonly IFavoritesService _favoritesService;

    private PluginManager? _pluginManager;

    private AppConfig _config = new();
    public AppStrings L { get; }
    private ManituxFramework _framework = new ManituxFramework();
    private readonly Stack<object> _navigationStack = new();
    
    private PageItemsViewModel? _currentPageItemsViewModel;

    [ObservableProperty] private PluginBase? _currentPlugin;
    private List<PluginMenuModel>? _pluginMenus;

    public MenuViewModel Menus { get; set; } = new MenuViewModel();
    public LocaleViewModel Locales { get; }

    [ObservableProperty] private object? _content;
    [ObservableProperty] private bool _isNavigationVisible = true;
    [ObservableProperty] private bool _isReady = false;
    [ObservableProperty] private bool _isInitialized = false;
    [ObservableProperty] private bool _isPluginsLoaded = false;

    public MainViewModel(
        IToastService toastService,
        INotificationService notificationService,
        IPluginService pluginService,
        ILocalizationService localizationService,
        IRemotePluginService remotePluginService,
        IFavoritesService favoritesService)
    {
        _toastService = toastService;
        _notificationService = notificationService;
        _pluginService = pluginService;
        _localizationService = localizationService;
        _remotePluginService = remotePluginService;
        _favoritesService = favoritesService;
        Locales = new LocaleViewModel(localizationService);

        L = _localizationService.Strings;
        FooterText = L.Settings;
        _localizationService.LanguageChanged += OnLanguageChanged;

        WeakReferenceMessenger.Default.Register<MainViewModel, MenuItemChangedMessage>(this, OnNavigation);
        WeakReferenceMessenger.Default.Register<MainViewModel, PageItemChangedMessage>(this, OnNavigation);
        //WeakReferenceMessenger.Default.Register<MainViewModel, string, string>(this, "JumpTo", OnNavigation);
        //OnNavigation(this, MenuKeys.MenuKeyEmptyPage);

        InitFramework();
        InitTlsClient();
        //TestMessage();
        //TestPlugin();
    }

    private void OnNavigation(MainViewModel vm, string s)
    {
        ClearNavigationStack();
        Content = s switch
        {
            MenuKeys.MenuKeyEmptyPage => new EmptyPageViewModel(_localizationService),
            MenuKeys.MenuKeyAboutUs => new AboutUsViewModel(),
            MenuKeys.MenuKeyCategories => new CategoriesViewModel(),
            MenuKeys.MenuKeyPageItems => new PageItemsViewModel(null),
            _ => null //throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };
        UpdateNavigationChrome();

        if (Content is null)
        {
            ShowToast($"{L.PageNotFound} {s}", NotificationType.Error);
        }
    }

    private void OnNavigation(MainViewModel vm, MenuItemChangedMessage message)
    {
        string key = message.Value.Key ?? "";

        //Content = key switch
        //{
        //    MenuKeys.MenuKeyEmptyPage => new EmptyPageViewModel(),
        //    MenuKeys.MenuKeyAboutUs => new AboutUsViewModel(),
        //    MenuKeys.MenuKeyCategories => new CategoriesViewModel(),
        //    MenuKeys.MenuKeyPageItems => new PageItemsViewModel(null),
        //    _ => null //throw new ArgumentOutOfRangeException(nameof(s), s, null)
        //};

        Content = null;
        ClearNavigationStack();
        if (key != MenuKeys.MenuKeyPageItems)
        {
            _currentPageItemsViewModel = null;
        }

        switch (key)
        {
            case MenuKeys.MenuKeyEmptyPage:
                Content = new EmptyPageViewModel(_localizationService);
                break;
            case MenuKeys.MenuKeyAboutUs:
                Content = new AboutUsViewModel();
                break;
            case MenuKeys.MenuKeySettings:
                Content = new RemotePluginsViewModel(_remotePluginService, _localizationService);
                break;
            case MenuKeys.MenuKeyFavorites:
                _currentPageItemsViewModel = null;
                Content = new FavoritesViewModel(_favoritesService, _pluginService, _localizationService, _remotePluginService);
                break;
            case MenuKeys.MenuKeyPageItems:
                message.Value.PageNumber = Math.Max(1, message.Value.PageNumber);
                SetCurrentPlugin(message.Value.PluginId);
                _currentPageItemsViewModel = new PageItemsViewModel(_pluginService, _localizationService, _remotePluginService, message.Value);
                Content = _currentPageItemsViewModel;
                break;
        }

        if (Content is null)
        {
            ShowToast($"{L.PageNotFound}", NotificationType.Error);
        }

        UpdateNavigationChrome();
    }

    private async void OnNavigation(MainViewModel vm, PageItemChangedMessage message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (message.Value is not null)
            {
                ShowMediaInfo(message.Value);
            }
            else
            {
                ShowToast($"{L.PageNotFound}", NotificationType.Error);
            }
        });
    }

    private Task InitTlsClient()
    {
        if (!OperatingSystem.IsLinux())
        {
            // use native on non linux platforms
            NativeTlsClient.Initialize(null);
        }
        else
        {
            // use api on linux
            ApiTlsClient.Initialize(null);
        }

        return Task.CompletedTask;
    }

    private async void InitFramework()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync())
        {
            if (IsInitialized) break;

            _pluginManager = await _framework.InitAsync();
            await _localizationService.SelectSystemOrDefaultLanguageAsync();
            LoadPlugins();
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        FooterText = IsCollapsed ? null : L.Settings;

        if (_pluginMenus?.Any() == true)
        {
            Menus.LoadMenus(_pluginMenus, L);
        }
        else
        {
            Menus.LoadDefaultMenu(L);
        }
    }

    private async void LoadPlugins()
    {
        if (_pluginManager is null)
        {
            Debug.WriteLine("_pluginManager is null, try again");
            _pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        }

        if (_pluginManager is not null)
        {
            _pluginMenus = new List<PluginMenuModel>();

            var loadedPlugins = _pluginManager.GetLoadedPlugins();

            if (loadedPlugins is not null && loadedPlugins.Any())
            {
                Debug.WriteLine("\n  Loaded plugins:");
                foreach (var p in loadedPlugins)
                {
                    Debug.WriteLine($"[{p.State,-12}] {p.Manifest.Name} v{p.Manifest.Version} — {p.Manifest.Description}");
                    var plugin = _pluginManager?.GetPlugin<PluginBase>(p.Manifest.Id);

                    if (plugin is not null && plugin.State == PluginState.Started)
                    {
                        var model = new PluginMenuModel()
                        {
                            Plugin = plugin,
                            Categories = await plugin.GetCategories()
                        };

                        _pluginMenus.Add(model);
                    }
                }
            }

            if (_pluginMenus.Any())
            {
                IsPluginsLoaded = true;
                Menus.LoadMenus(_pluginMenus, L);
                OnNavigation(this, MenuKeys.MenuKeyPageItems);
            }
            else
            {
                IsPluginsLoaded = false;
                Menus.LoadDefaultMenu(L);
                OnNavigation(this, MenuKeys.MenuKeyEmptyPage);
            }

            IsInitialized = true;
            IsReady = true;
        }
        else
        {
            // framework is not initialized!
            IsInitialized = false;
            IsReady = false;
            ShowToast($"{L.AppNotInitialized}", NotificationType.Error);
        }
    }

    private void SetCurrentPlugin(string? pluginId)
    {
        if (pluginId is null)
        {
            return;
        }

        var plugin = _pluginManager?.GetPlugin<PluginBase>(pluginId);
        if (plugin is null || plugin.State != PluginState.Started)
        {
            return;
        }

        _pluginService.CurrentPlugin = plugin;
        CurrentPlugin = plugin;
    }

    private void ShowMediaInfo(PageItemModel pageItem)
    {
        if (!string.IsNullOrWhiteSpace(pageItem.PluginId))
        {
            SetCurrentPlugin(pageItem.PluginId);
        }

        if (CurrentPlugin is null)
        {
            ShowToast(L.PluginNotSelected, NotificationType.Warning);
            return;
        }

        PushContent(new MediaInfoViewModel(
            _pluginService,
            _localizationService,
            _favoritesService,
            pageItem,
            NavigateToPlayer,
            GoBack));
    }

    private async void NavigateToPlayer(PlayerViewModel playerViewModel)
    {
        var options = new OverlayDialogOptions
        {
            HorizontalAnchor = HorizontalPosition.Center,
            VerticalAnchor = VerticalPosition.Center,
            FullScreen = true,
            Buttons = DialogButton.None,
            Mode = DialogMode.None,
            CanDragMove = false,
            CanResize = false,
        };

        await OverlayDialog.ShowCustomModal<PlayerView, PlayerViewModel, object>(
            playerViewModel,
            null,
            options: options);
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        if (_navigationStack.Count == 0)
        {
            return;
        }

        Content = _navigationStack.Pop();
        UpdateNavigationChrome();
        GoBackCommand.NotifyCanExecuteChanged();
    }

    private bool CanGoBack()
    {
        return _navigationStack.Count > 0;
    }

    private void PushContent(object viewModel)
    {
        if (Content is not null)
        {
            _navigationStack.Push(Content);
        }

        Content = viewModel;
        UpdateNavigationChrome();
        GoBackCommand.NotifyCanExecuteChanged();
    }

    private void ClearNavigationStack()
    {
        _navigationStack.Clear();
        UpdateNavigationChrome();
        GoBackCommand.NotifyCanExecuteChanged();
    }

    private void UpdateNavigationChrome()
    {
        IsNavigationVisible = Content is not PlayerViewModel;
    }

    private void ShowTestPlayer()
    {
        //ShowPlayer(null);
        NavigateToPlayer(new PlayerViewModel(_pluginService, _localizationService, new VideoSourceModel() { Name = "Test", Url = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json", Subtitles = new() { new() { Id = "1", Name = "Test", Url = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt" } } }));
    }

    private async void TestMessage()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync())
        {
            await _localizationService.ChangeLanguageAsync("tr-TR");
            
            //ShowTestPlayer();
            //ShowMessage("test", "test 123");
            //ShowNotify("test", "test 123", NotificationType.Success);
            //ShowMessage("test", "test 123", NotificationType.Warning);
            //ShowMessage("test", "test 123", NotificationType.Error);

            //ShowToast("test 123456", NotificationType.Information, "Light");
            //ShowToast("test 123456", NotificationType.Success, "Light");
            //ShowToast(_pluginManager, NotificationType.Warning, "Light");
            //ShowToast("test 123456", NotificationType.Error, "Light");
        }
    }

    public void ShowNotify(string title, string message, NotificationType type = NotificationType.Information, string style = "Dark")
    {
        _notificationService.ShowNotify(message, title, type, true, true);
    }

    //[RelayCommand]
    public void ShowToast(string message, NotificationType type = NotificationType.Information, string style = "Dark")
    {
        _toastService.ShowToast(message, type, true);
    }



    public ObservableCollection<ThemeItem> Themes { get; } =
    [
        new("Default", ThemeVariant.Default),
        new("Light", ThemeVariant.Light),
        new("Dark", ThemeVariant.Dark),
        new("Aquatic", SemiTheme.Aquatic),
        new("Desert", SemiTheme.Desert),
        new("Dusk", SemiTheme.Dusk),
        new("NightSky", SemiTheme.NightSky)
    ];

    [ObservableProperty] private ThemeItem? _selectedTheme;

    partial void OnSelectedThemeChanged(ThemeItem? oldValue, ThemeItem? newValue)
    {
        if (newValue is null) return;
        var app = Application.Current;
        if (app is not null)
        {
            app.RequestedThemeVariant = newValue.Theme;
            // NotificationManager?.Show(
            //     new Notification("Theme changed", $"Theme changed to {newValue.Name}"),
            //     type: NotificationType.Success,
            //     classes: ["Light"]);
        }
    }

    [ObservableProperty] private string? _footerText;

    [ObservableProperty] private bool _isCollapsed;

    partial void OnIsCollapsedChanged(bool value)
    {
        FooterText = value ? null : L.Settings;
    }
}

public class ThemeItem(string name, ThemeVariant theme)
{
    public string Name { get; set; } = name;
    public ThemeVariant Theme { get; set; } = theme;
}
