using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Services.Plugins;
using Manitux.Services.Applications;
using Manitux.Services.Downloads;
using Manitux.Services.Favorites;
using Manitux.Services.Localizations;
using Manitux.Services.Notifications;
using Manitux.Services.Plugins;
using Manitux.Services.Settings;
using Manitux.Services.Updates;
using Manitux.Services.WatchedEpisodes;
using Manitux.ViewModels;
using Manitux.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Manitux;

public partial class App : Application
{
    private bool _isShuttingDown;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ApplicationViewModel();
    }

    // public override void OnFrameworkInitializationCompleted()
    // {
    //     if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    //     {
    //         desktop.MainWindow = new MainWindow
    //         {
    //             DataContext = new MainViewModel()
    //         };
    //     }
    //     else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
    //     {
    //         singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = new MainViewModel() };
    //     }
    //     else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
    //     {
    //         singleViewPlatform.MainView = new MainView
    //         {
    //             DataContext = new MainViewModel()
    //         };
    //     }

    //     base.OnFrameworkInitializationCompleted();
    // }


    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();

            services.AddNotificationServices(mainWindow);

            //services.AddPluginManager();
            //await services.AddPluginManagerAsync();
            //services.AddPluginManagerAsync().ConfigureAwait(true);

            services.AddSingleton<IPluginService, PluginService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IRemotePluginService, RemotePluginService>();
            services.AddSingleton<IFavoritesService, FavoritesService>();
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IWatchedEpisodesService, WatchedEpisodesService>();
            services.AddSingleton<IApplicationUpdateService, ApplicationUpdateService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            services.AddTransient<MainViewModel>();

            var provider = services.BuildServiceProvider();

            var vm = provider.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = vm;

            desktop.MainWindow = mainWindow;
            vm.ShowToast(vm.L.ManituxDesktopApp, NotificationType.Information);

            desktop.ShutdownRequested += async (sender, e) =>
            {
                if (_isShuttingDown)
                {
                    return;
                }

                e.Cancel = true;
                _isShuttingDown = true;

                Debug.WriteLine("ShutdownRequested");
                await SaveApplicationSettingsAsync(provider);
                await CodeLogic.CodeLogic.StopAsync();
                desktop.Shutdown();
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new SingleView();

            var topLevel = TopLevel.GetTopLevel(singleViewPlatform.MainView);
            if (topLevel is null)
            {
                throw new InvalidOperationException("Main view top level could not be resolved.");
            }

            services.AddNotificationServices(topLevel);

            //services.AddPluginManagerAsync().ConfigureAwait(false);

            services.AddSingleton<IPluginService, PluginService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IRemotePluginService, RemotePluginService>();
            services.AddSingleton<IFavoritesService, FavoritesService>();
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IWatchedEpisodesService, WatchedEpisodesService>();
            services.AddSingleton<IApplicationUpdateService, ApplicationUpdateService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            services.AddTransient<MainViewModel>();

            var provider = services.BuildServiceProvider();

            var vm = provider.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView.DataContext = vm;
            vm.ShowToast(vm.L.ManituxMobileApp, NotificationType.Information);
        }

        Dispatcher.UIThread.UnhandledException += (sender, e) =>
        {
            e.Handled = true;
            Debug.WriteLine($"UnhandledException: {e.Exception.Message}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            Debug.WriteLine($"UnobservedTaskException: {e.Exception.Message}");
        };

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task SaveApplicationSettingsAsync(IServiceProvider provider)
    {
        try
        {
            var settingsService = provider.GetRequiredService<IAppSettingsService>();
            var localizationService = provider.GetRequiredService<ILocalizationService>();
            var pluginService = provider.GetRequiredService<IPluginService>();
            var remotePluginService = provider.GetRequiredService<IRemotePluginService>();
            var remoteSettings = await remotePluginService.GetSettingsAsync();
            var theme = Application.Current?.RequestedThemeVariant;
            var themeName = theme == ThemeVariant.Light
                ? "Light"
                : theme == ThemeVariant.Dark
                    ? "Dark"
                    : theme == ThemeVariant.Default
                        ? "Default"
                        : settingsService.Settings.Theme;

            await settingsService.SaveCurrentAsync(
                localizationService.CurrentCulture,
                themeName,
                pluginService.CurrentPlugin,
                remoteSettings.InstalledPlugins);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Application settings could not be saved: {ex}");
        }
    }
}
