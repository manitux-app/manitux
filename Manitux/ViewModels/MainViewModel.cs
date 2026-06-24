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
using Manitux.Services.Downloads;
using Manitux.Services.Favorites;
using Manitux.Services.Localizations;
using Manitux.Services.Notifications;
using Manitux.Services.Plugins;
using Manitux.Services.Settings;
using Manitux.Services.Updates;
using Manitux.Services.WatchedEpisodes;
using Manitux.Views;
using Semi.Avalonia;
using TlsClient.Api;
using TlsClient.Native;
using Ursa.Controls;
using static Manitux.Core.Helpers.LogHelper;
using SemiTheme = Semi.Avalonia.SemiTheme;
using UrsaWindowNotificationManager = Ursa.Controls.WindowNotificationManager;

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
    private readonly IDownloadService _downloadService;
    private readonly IWatchedEpisodesService _watchedEpisodesService;
    private readonly IApplicationUpdateService _applicationUpdateService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly UrsaWindowNotificationManager _notificationManager;
    private bool _applicationUpdateCheckStarted;
    private bool _isApplyingSettings;

    private PluginManager? _pluginManager;

    private AppConfig _config = new();
    public AppStrings L { get; }
    public string ApplicationVersion => _applicationUpdateService.CurrentVersion;
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
    [ObservableProperty] private string _selectedNavigationKey = NavigationBarKeys.Home;

    public MainViewModel(
        IToastService toastService,
        INotificationService notificationService,
        IPluginService pluginService,
        ILocalizationService localizationService,
        IRemotePluginService remotePluginService,
        IFavoritesService favoritesService,
        IDownloadService downloadService,
        IWatchedEpisodesService watchedEpisodesService,
        IApplicationUpdateService applicationUpdateService,
        IAppSettingsService appSettingsService,
        UrsaWindowNotificationManager notificationManager)
    {
        _toastService = toastService;
        _notificationService = notificationService;
        _pluginService = pluginService;
        _localizationService = localizationService;
        _remotePluginService = remotePluginService;
        _favoritesService = favoritesService;
        _downloadService = downloadService;
        _watchedEpisodesService = watchedEpisodesService;
        _applicationUpdateService = applicationUpdateService;
        _appSettingsService = appSettingsService;
        _notificationManager = notificationManager;
        Locales = new LocaleViewModel(localizationService);

        L = _localizationService.Strings;
        FooterText = L.Settings;
        _localizationService.LanguageChanged += OnLanguageChanged;

        WeakReferenceMessenger.Default.Register<MainViewModel, MenuItemChangedMessage>(this, OnNavigation);
        WeakReferenceMessenger.Default.Register<MainViewModel, PageItemChangedMessage>(this, OnNavigation);
        WeakReferenceMessenger.Default.Register<MainViewModel, PluginCatalogReloadingMessage>(this, OnPluginCatalogReloading);
        WeakReferenceMessenger.Default.Register<MainViewModel, PluginCatalogChangedMessage>(this, OnPluginCatalogChanged);
        WeakReferenceMessenger.Default.Register<MainViewModel, PluginSelectionChangedMessage>(this, OnPluginSelectionChanged);
        _applicationUpdateService.UpdateAvailable += ApplicationUpdateServiceOnUpdateAvailable;
        //WeakReferenceMessenger.Default.Register<MainViewModel, string, string>(this, "JumpTo", OnNavigation);
        //OnNavigation(this, MenuKeys.MenuKeyEmptyPage);

        InitFramework();
        InitTlsClient();
        //TestMessage();
        //TestPlugin();
    }

    private void OnNavigation(MainViewModel vm, string s)
    {
        UpdateSelectedNavigationKey(s);
        ClearNavigationStack();
        Content = s switch
        {
            MenuKeys.MenuKeyEmptyPage => new EmptyPageViewModel(_localizationService),
            MenuKeys.MenuKeyAboutUs => new AboutUsViewModel(),
            MenuKeys.MenuKeyCategories => new CategoriesViewModel(),
            MenuKeys.MenuKeyPageItems => new PageItemsViewModel(null),
            MenuKeys.MenuKeySettings => new RemotePluginsViewModel(_remotePluginService, _localizationService, _pluginService),
            MenuKeys.MenuKeyApplicationUpdate => new UpdateViewModel(_applicationUpdateService, _localizationService, _notificationService),
            MenuKeys.MenuKeyDownloads => new DownloadsViewModel(_downloadService, _localizationService),
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
        UpdateSelectedNavigationKey(key);

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
                Content = new RemotePluginsViewModel(_remotePluginService, _localizationService, _pluginService);
                break;
            case MenuKeys.MenuKeyApplicationUpdate:
                Content = new UpdateViewModel(_applicationUpdateService, _localizationService, _notificationService);
                break;
            case MenuKeys.MenuKeyFavorites:
                _currentPageItemsViewModel = null;
                Content = new FavoritesViewModel(_favoritesService, _pluginService, _localizationService, _remotePluginService);
                break;
            case MenuKeys.MenuKeyDownloads:
                _currentPageItemsViewModel = null;
                Content = new DownloadsViewModel(_downloadService, _localizationService);
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

    private void OnPluginCatalogChanged(MainViewModel vm, PluginCatalogChangedMessage message)
    {
        LoadPlugins(navigateToFirstPlugin: false);
    }

    private void OnPluginCatalogReloading(MainViewModel vm, PluginCatalogReloadingMessage message)
    {
        _pluginService.CurrentPlugin = null;
        CurrentPlugin = null;
        _pluginMenus = null;
        _currentPageItemsViewModel = null;
        ClearNavigationStack();
        Menus.LoadDefaultMenu(L);
    }

    public bool IsNavigationSelected(string key)
    {
        return string.Equals(SelectedNavigationKey, key, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSelectedNavigationKeyChanged(string value)
    {
        OnPropertyChanged(nameof(IsHomeSelected));
        OnPropertyChanged(nameof(IsFavoritesSelected));
        OnPropertyChanged(nameof(IsPluginsSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    public bool IsHomeSelected => IsNavigationSelected(NavigationBarKeys.Home);
    public bool IsFavoritesSelected => IsNavigationSelected(NavigationBarKeys.Favorites);
    public bool IsPluginsSelected => IsNavigationSelected(NavigationBarKeys.Plugins);
    public bool IsSettingsSelected => IsNavigationSelected(NavigationBarKeys.Settings);

    private void UpdateSelectedNavigationKey(string key)
    {
        SelectedNavigationKey = key switch
        {
            MenuKeys.MenuKeyPageItems or MenuKeys.MenuKeyMediaInfo or MenuKeys.MenuKeySearch => NavigationBarKeys.Home,
            MenuKeys.MenuKeyFavorites => NavigationBarKeys.Favorites,
            MenuKeys.MenuKeySettings => NavigationBarKeys.Plugins,
            MenuKeys.MenuKeyApplicationUpdate => NavigationBarKeys.Settings,
            _ => SelectedNavigationKey
        };
    }

    private void OnPluginSelectionChanged(MainViewModel vm, PluginSelectionChangedMessage message)
    {
        var navigation = CreatePluginHomeNavigation(message.Value);
        if (navigation is null)
        {
            ShowToast(L.PluginWasNotFound, NotificationType.Warning);
            return;
        }

        OnNavigation(this, new MenuItemChangedMessage(navigation));
    }

    [RelayCommand]
    private void ShowHome()
    {
        SelectedNavigationKey = NavigationBarKeys.Home;
        var navigation = CreatePluginHomeNavigation(preferCurrentPlugin: true);
        if (navigation is not null)
        {
            OnNavigation(this, new MenuItemChangedMessage(navigation));
            return;
        }

        OnNavigation(this, IsPluginsLoaded ? MenuKeys.MenuKeyPageItems : MenuKeys.MenuKeySettings);
    }

    [RelayCommand]
    private void ShowPlugins()
    {
        SelectedNavigationKey = NavigationBarKeys.Plugins;
        OnNavigation(this, MenuKeys.MenuKeySettings);
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
            _config = await _appSettingsService.LoadAsync();
            await ApplyStartupSettingsAsync();
            LoadPlugins();
        }
    }

    private async Task ApplyStartupSettingsAsync()
    {
        _isApplyingSettings = true;
        try
        {
            ApplyTheme(_config.Theme);

            if (!string.IsNullOrWhiteSpace(_config.Language))
            {
                await _localizationService.ChangeLanguageAsync(_config.Language);
            }
            else
            {
                await _localizationService.SelectSystemOrDefaultLanguageAsync();
            }
        }
        finally
        {
            _isApplyingSettings = false;
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

        if (!_isApplyingSettings)
        {
            _ = SaveCurrentSettingsAsync();
        }
    }

    private async void LoadPlugins(bool navigateToFirstPlugin = true)
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
                if (navigateToFirstPlugin)
                {
                    var initialNavigation = CreateInitialPluginNavigation();
                    if (initialNavigation is not null)
                    {
                        OnNavigation(this, new MenuItemChangedMessage(initialNavigation));
                    }
                    else
                    {
                        OnNavigation(this, MenuKeys.MenuKeyPageItems);
                    }
                }
            }
            else
            {
                IsPluginsLoaded = false;
                Menus.LoadDefaultMenu(L);
                if (navigateToFirstPlugin)
                {
                    OnNavigation(this, MenuKeys.MenuKeySettings);
                }
            }

            IsInitialized = true;
            IsReady = true;
            StartApplicationUpdateCheck();
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
        _ = SaveCurrentSettingsAsync();
    }

    private MenuItemViewModel? CreateInitialPluginNavigation()
    {
        return CreatePluginHomeNavigation(preferCurrentPlugin: false);
    }

    private MenuItemViewModel? CreatePluginHomeNavigation(bool preferCurrentPlugin)
    {
        if (_pluginMenus is null || _pluginMenus.Count == 0)
        {
            return null;
        }

        var selectedPlugin = preferCurrentPlugin && CurrentPlugin is not null
            ? _pluginMenus.FirstOrDefault(x =>
                string.Equals(x.Plugin.Manifest.Id, CurrentPlugin.Manifest.Id, StringComparison.OrdinalIgnoreCase))
            : null;

        selectedPlugin ??= _pluginMenus.FirstOrDefault(x =>
                string.Equals(x.Plugin.Manifest.Id, _config.CurrentPluginId, StringComparison.OrdinalIgnoreCase))
            ?? _pluginMenus.FirstOrDefault(x =>
                _config.SelectedPlugins.Any(selected =>
                    selected.IsCurrent &&
                    string.Equals(selected.Id, x.Plugin.Manifest.Id, StringComparison.OrdinalIgnoreCase)))
            ?? _pluginMenus.FirstOrDefault();

        if (selectedPlugin is null)
        {
            return null;
        }

        return new MenuItemViewModel
        {
            MenuHeader = selectedPlugin.Plugin.Manifest.Name,
            Key = MenuKeys.MenuKeyPageItems,
            PluginId = selectedPlugin.Plugin.Manifest.Id,
            PageNumber = 1
        };
    }

    private MenuItemViewModel? CreatePluginHomeNavigation(string pluginId)
    {
        var selectedPlugin = _pluginMenus?.FirstOrDefault(x =>
            string.Equals(x.Plugin.Manifest.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        if (selectedPlugin is null)
        {
            return null;
        }

        return new MenuItemViewModel
        {
            MenuHeader = selectedPlugin.Plugin.Manifest.Name,
            Key = MenuKeys.MenuKeyPageItems,
            PluginId = selectedPlugin.Plugin.Manifest.Id,
            PageNumber = 1
        };
    }

    private async Task SaveCurrentSettingsAsync()
    {
        try
        {
            var remoteSettings = await _remotePluginService.GetSettingsAsync();
            await _appSettingsService.SaveCurrentAsync(
                _localizationService.CurrentCulture,
                GetCurrentThemeName(),
                _pluginService.CurrentPlugin,
                remoteSettings.InstalledPlugins);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Application settings could not be saved: {ex}");
        }
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
            _downloadService,
            _watchedEpisodesService,
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

    private void StartApplicationUpdateCheck()
    {
        if (_applicationUpdateCheckStarted)
        {
            return;
        }

        _applicationUpdateCheckStarted = true;
        _ = Task.Run(() => _applicationUpdateService.CheckForUpdatesAsync());
    }

    private void ApplicationUpdateServiceOnUpdateAvailable(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(ShowApplicationUpdateNotification);
    }

    private void ShowApplicationUpdateNotification()
    {
        var updateVersion = _applicationUpdateService.LatestVersion
                            ?? _applicationUpdateService.LatestReleaseName
                            ?? string.Empty;
        var message = string.Format(L.ApplicationUpdateReadyFormat, updateVersion);

        var detailsButton = new Button
        {
            Content = L.Open,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        detailsButton.Click += (_, _) => OnNavigation(this, MenuKeys.MenuKeyApplicationUpdate);

        var updateButton = new Button
        {
            Content = L.StartApplicationUpdate,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        updateButton.Click += async (_, _) =>
        {
            ShowToast(L.ApplicationUpdateStarting, NotificationType.Information);
            await _applicationUpdateService.DownloadAndInstallUpdateAsync();
        };

        var content = new StackPanel
        {
            Spacing = 8,
            MaxWidth = 360
        };
        content.Children.Add(new TextBlock
        {
            Text = L.ApplicationUpdate,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });
        content.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var actions = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        actions.Children.Add(detailsButton);
        actions.Children.Add(updateButton);
        content.Children.Add(actions);

        _notificationManager.Show(
            content,
            showIcon: true,
            showClose: true,
            type: NotificationType.Information,
            classes: ["Dark"],
            expiration: TimeSpan.FromSeconds(30));
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
        ApplyTheme(newValue.Name);

        if (!_isApplyingSettings)
        {
            _ = SaveCurrentSettingsAsync();
        }
    }

    public void ToggleTheme()
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var nextTheme = app.ActualThemeVariant == ThemeVariant.Dark ? "Light" : "Dark";
        ApplyTheme(nextTheme);
        _ = SaveCurrentSettingsAsync();
    }

    private void ApplyTheme(string? themeName)
    {
        var theme = ResolveTheme(themeName);
        var app = Application.Current;
        if (app is not null)
        {
            app.RequestedThemeVariant = theme.Theme;
        }

        SelectedTheme = Themes.FirstOrDefault(x =>
            string.Equals(x.Name, theme.Name, StringComparison.OrdinalIgnoreCase));
    }

    private ThemeItem ResolveTheme(string? themeName)
    {
        return Themes.FirstOrDefault(x =>
                   string.Equals(x.Name, themeName, StringComparison.OrdinalIgnoreCase))
               ?? Themes.First(x => x.Name == "Dark");
    }

    private string GetCurrentThemeName()
    {
        var requestedTheme = Application.Current?.RequestedThemeVariant;
        var selectedTheme = Themes.FirstOrDefault(x => x.Theme == requestedTheme);
        return selectedTheme?.Name ?? SelectedTheme?.Name ?? "Dark";
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

public static class NavigationBarKeys
{
    public const string Home = "Home";
    public const string Favorites = "Favorites";
    public const string Plugins = "Plugins";
    public const string Settings = "Settings";
}
