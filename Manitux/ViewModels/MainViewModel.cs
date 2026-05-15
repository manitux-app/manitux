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
using Manitux.Models;
using Manitux.Pages;
using Manitux.Player;
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

    private PluginManager? _pluginManager;

    private AppConfig _config = new();
    public AppStrings L { get; }
    private ManituxFramework _framework = new ManituxFramework();
    
    private PageItemsViewModel? _currentPageItemsViewModel;

    [ObservableProperty] private PluginBase? _currentPlugin;
    private List<PluginMenuModel>? _pluginMenus;

    public MenuViewModel Menus { get; set; } = new MenuViewModel();
    public LocaleViewModel Locales { get; set; } = new LocaleViewModel();

    [ObservableProperty] private object? _content;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private bool _isReady = false;
    [ObservableProperty] private bool _isInitialized = false;
    [ObservableProperty] private bool _isPluginsLoaded = false;

    public MainViewModel(IToastService toastService, INotificationService notificationService, IPluginService pluginService, ILocalizationService localizationService)
    {
        _toastService = toastService;
        _notificationService = notificationService;
        _pluginService = pluginService;
        _localizationService = localizationService;

        L = _localizationService.Strings;

        WeakReferenceMessenger.Default.Register<MainViewModel, MenuItemChangedMessage>(this, OnNavigation);
        WeakReferenceMessenger.Default.Register<MainViewModel, PageItemChangedMessage>(this, OnNavigation);
        //WeakReferenceMessenger.Default.Register<MainViewModel, string, string>(this, "JumpTo", OnNavigation);
        //OnNavigation(this, MenuKeys.MenuKeyEmptyPage);

        InitFramework();
        InitTlsClient();
        TestMessage();
        //TestPlugin();
    }

    [RelayCommand]
    private async Task Search()
    {
        if (_currentPageItemsViewModel is null || _pluginService.CurrentPlugin is null)
        {
            ShowToast("Plugin not selected", NotificationType.Warning);
            return;
        }

        var query = SearchText?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;

        var hasResults = await _currentPageItemsViewModel.Search(query);
        if (!hasResults)
        {
            ShowToast($"{L.PageNotFound}", NotificationType.Error);
            return;
        }

        Content = _currentPageItemsViewModel;
    }


    private void OnNavigation(MainViewModel vm, string s)
    {
        Content = s switch
        {
            MenuKeys.MenuKeyEmptyPage => new EmptyPageViewModel(),
            MenuKeys.MenuKeyAboutUs => new AboutUsViewModel(),
            MenuKeys.MenuKeyCategories => new CategoriesViewModel(),
            MenuKeys.MenuKeyPageItems => new PageItemsViewModel(null),
            _ => null //throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

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
        if (key != MenuKeys.MenuKeyPageItems)
        {
            _currentPageItemsViewModel = null;
        }

        switch (key)
        {
            case MenuKeys.MenuKeyEmptyPage:
                Content = new EmptyPageViewModel();
                break;
            case MenuKeys.MenuKeyAboutUs:
                Content = new AboutUsViewModel();
                break;
            case MenuKeys.MenuKeySettings:
                ShowTestPlayer();
                break;
            case MenuKeys.MenuKeyPageItems:
                message.Value.PageNumber = Math.Max(1, message.Value.PageNumber);
                SetCurrentPlugin(message.Value.PluginId);
                _currentPageItemsViewModel = new PageItemsViewModel(_pluginService, message.Value);
                Content = _currentPageItemsViewModel;
                break;
        }

        if (Content is null)
        {
            ShowToast($"{L.PageNotFound}", NotificationType.Error);
        }
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
            LoadPlugins();
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

    private async void ShowMediaInfo(PageItemModel pageItem)
    {
        if (CurrentPlugin is null)
        {
            ShowToast("Plugin not selected", NotificationType.Warning);
            return;
        }

        var options = new OverlayDialogOptions()
        {
            FullScreen = true,
            Buttons = DialogButton.None,
            Title = pageItem.Title,
            Mode = DialogMode.None,
            CanDragMove = false,
            CanResize = false,
            //TopLevelHashCode = this.GetHashCode(),
            //OnDialogControlClosed = (object? _, object? _) => target.Focus()
        };

        await OverlayDialog.ShowCustomModal<MediaInfo, MediaInfoViewModel, object>(new MediaInfoViewModel(_pluginService, _localizationService, pageItem), null, options: options);
        //OverlayDialog.Show<MediaInfo, MediaInfoViewModel>(new MediaInfoViewModel(), null, options: options);
        //await OverlayDialog.ShowModal<MediaInfo, MediaInfoViewModel>(new MediaInfoViewModel(mediaInfo), null, options: options);
    }

    private async void ShowPlayer(VideoSourceModel? videoSource)
    {
        var options = new OverlayDialogOptions()
        {
            HorizontalAnchor = HorizontalPosition.Center,
            VerticalAnchor = VerticalPosition.Center,
            FullScreen = true,
            Buttons = DialogButton.None,
            Mode = DialogMode.None,
            CanDragMove = false,
            CanResize = false,
        };

        var content = new PlayerView
        {
            DataContext = new PlayerViewModel(_pluginService, _localizationService, videoSource)
        };

        await OverlayDialog.ShowCustomModal<RootPage, RootPageViewModel, object>(new RootPageViewModel(content), null, options: options);
    }

    private void ShowTestPlayer()
    {
        //ShowPlayer(null);
        ShowPlayer(new VideoSourceModel() { Name = "Test", Url = "https://server15700.contentdm.oclc.org/dmwebservices/index.php?q=dmGetStreamingFile/p15700coll2/15.mp4/byte/json", Subtitles = new() { new() { Id = "1", Name = "Test", Url = "https://cdmdemo.contentdm.oclc.org/utils/getfile/collection/p15700coll2/id/18/filename/video2.vtt" } } });
    }

    private async void TestMessage()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync())
        {
            _localizationService.ChangeLanguage("tr-TR");
            
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

    [ObservableProperty] private string? _footerText = "Settings";

    [ObservableProperty] private bool _isCollapsed;

    partial void OnIsCollapsedChanged(bool value)
    {
        FooterText = value ? null : "Settings";
    }
}

public class ThemeItem(string name, ThemeVariant theme)
{
    public string Name { get; set; } = name;
    public ThemeVariant Theme { get; set; } = theme;
}
