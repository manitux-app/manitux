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
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Application;
using Manitux.Core.Framework;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;
using Manitux.Pages;
using Manitux.Services.Notifications;
using Manitux.Views;
using Semi.Avalonia;
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
    private PluginManager? _pluginManager;

    private AppConfig _config = new();
    [ObservableProperty] private AppStrings _localize = new();
    private ManituxFramework _framework = new ManituxFramework();

    [ObservableProperty] private PluginBase? _currentPlugin;
    private List<PluginMenuModel>? _pluginMenus;

    public MenuViewModel Menus { get; set; } = new MenuViewModel();
    public LocaleViewModel Locales { get; set; } = new LocaleViewModel();

    [ObservableProperty] private object? _content;
    [ObservableProperty] private bool _isReady = false;
    [ObservableProperty] private bool _isInitialized = false;
    [ObservableProperty] private bool _isPluginsLoaded = false;

    public MainViewModel(IToastService toastService, INotificationService notificationService)
    {
        _toastService = toastService;
        _notificationService = notificationService;
        //_pluginManager = pluginManager;

        WeakReferenceMessenger.Default.Register<MainViewModel, MenuItemChangedMessage>(this, OnNavigation);
        WeakReferenceMessenger.Default.Register<MainViewModel, PageItemChangedMessage>(this, OnNavigation);
        //WeakReferenceMessenger.Default.Register<MainViewModel, string, string>(this, "JumpTo", OnNavigation);
        //OnNavigation(this, MenuKeys.MenuKeyEmptyPage);

        InitFramework();
        TestMessage();
        //TestPlugin();
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
            ShowToast($"{Localize.PageNotFound} {s}", NotificationType.Error);
        }
    }

    private async void OnNavigation(MainViewModel vm, MenuItemChangedMessage message)
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

        switch (key)
        {
            case MenuKeys.MenuKeyEmptyPage:
                Content = new EmptyPageViewModel();
                break;
            case MenuKeys.MenuKeyAboutUs:
                Content = new AboutUsViewModel();
                break;
            case MenuKeys.MenuKeySettings:
                break;
            case MenuKeys.MenuKeyPageItems:
                var items = await GetPageItems(message);
                if(items is not null) Content = new PageItemsViewModel(items);
                break;
        }

        if (Content is null)
        {
            ShowToast($"{Localize.PageNotFound}", NotificationType.Error);
        }
    }

    private async void OnNavigation(MainViewModel vm, PageItemChangedMessage message)
    {
        var mediaInfo = await GetMediaInfo(message);
        if (mediaInfo is not null)
        {
            ShowMediaInfo(mediaInfo);
        }
        else
        {
            ShowToast($"{Localize.PageNotFound}", NotificationType.Error);
        }
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
            var strings = CodeLogic.CodeLogic.GetApplicationContext()?.Localization.Get<AppStrings>("tr-TR");
            if (strings is not null) Localize = strings;
            //Console.WriteLine($"AppStrings: {JsonSerializer.Serialize(Localize)}" + Environment.NewLine);

            _pluginMenus = new List<PluginMenuModel>();

            var loadedPlugins = _pluginManager.GetLoadedPlugins();

            if (loadedPlugins is not null && loadedPlugins.Any())
            {
                Console.WriteLine("\n  Loaded plugins:");
                foreach (var p in loadedPlugins)
                {
                    Console.WriteLine($"[{p.State,-12}] {p.Manifest.Name} v{p.Manifest.Version} — {p.Manifest.Description}");
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
                Menus.LoadMenus(_pluginMenus, Localize);
                OnNavigation(this, MenuKeys.MenuKeyPageItems);
            }
            else
            {
                IsPluginsLoaded = false;
                Menus.LoadDefaultMenu(Localize);
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
            ShowToast($"{Localize.NotInitialized}", NotificationType.Error);
        }
    }

    private async Task<List<PageItemModel>?> GetPageItems(MenuItemChangedMessage message)
    {
        string key = message.Value.Key ?? "";
        string? pluginId = message.Value.PluginId ?? null;
        int pageNumber = message.Value.PageNumber;

        if (pluginId is not null)
        {
            var plugin = _pluginManager?.GetPlugin<PluginBase>(pluginId);

            if (plugin is not null && plugin.State == PluginState.Started)
            {
                CurrentPlugin = plugin;

                Debug.WriteLine($"Plugin: {JsonSerializer.Serialize(plugin.Manifest)}" + Environment.NewLine);
                var cat = message.Value.Category;
                Debug.WriteLine($"Category: {JsonSerializer.Serialize(cat)}" + Environment.NewLine);
                if (cat is null) return null;
                var pageItems = await plugin.GetPageItems(pageNumber, cat);
                if (pageItems is null) return null;
                Debug.WriteLine($"PageItems: {JsonSerializer.Serialize(pageItems)}" + Environment.NewLine);
                return pageItems;
            }
        }

        return null;
    }

    private async Task<MediaInfoModel?> GetMediaInfo(PageItemChangedMessage message)
    {
        var pageItem = message.Value;

        if (pageItem is null) return null;

        var mediaInfo = new MediaInfoModel()
        {
            Url = pageItem.Url,
            Title = pageItem.Title,
            Poster = pageItem.Poster
        };

        if (CurrentPlugin is not null)
        {
            Debug.WriteLine($"Plugin: {JsonSerializer.Serialize(CurrentPlugin.Manifest)}" + Environment.NewLine);
            mediaInfo = await CurrentPlugin.GetMediaInfo(pageItem);
            Debug.WriteLine($"MediaInfo: {JsonSerializer.Serialize(mediaInfo)}" + Environment.NewLine);
        }

        return mediaInfo;
    }
    private async void ShowMediaInfo(MediaInfoModel mediaInfo)
    {
        var options = new OverlayDialogOptions()
        {
            FullScreen = true,
            Buttons = DialogButton.None,
            Title = mediaInfo.Title,
            Mode = DialogMode.None,
            CanDragMove = false,
            CanResize = false,
            //TopLevelHashCode = this.GetHashCode(),
            //OnDialogControlClosed = (object? _, object? _) => target.Focus()
        };

        await OverlayDialog.ShowCustomModal<MediaInfo, MediaInfoViewModel, object>(new MediaInfoViewModel(CurrentPlugin, mediaInfo, Localize), null, options: options);
        //OverlayDialog.Show<MediaInfo, MediaInfoViewModel>(new MediaInfoViewModel(), null, options: options);
        //await OverlayDialog.ShowModal<MediaInfo, MediaInfoViewModel>(new MediaInfoViewModel(mediaInfo), null, options: options);
    }

    private async void TestMessage()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync())
        {
            //ShowMessage("test", "test 123");
            //ShowNotify("test", "test 123", NotificationType.Success);
            //ShowMessage("test", "test 123", NotificationType.Warning);
            //ShowMessage("test", "test 123", NotificationType.Error);

            ShowToast("test 123456", NotificationType.Information, "Light");
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
