using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Services.Plugins;
using Manitux.Services.Applications;
using Manitux.Services.Localizations;
using Manitux.Services.Notifications;
using Manitux.Services.Plugins;
using Manitux.ViewModels;
using Manitux.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Manitux;

public partial class App : Application
{
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

            services.AddTransient<MainViewModel>();

            var provider = services.BuildServiceProvider();

            var vm = provider.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = vm;

            desktop.MainWindow = mainWindow;
            vm.ShowToast("Manitux Desktop App", NotificationType.Information);

            desktop.ShutdownRequested += async (sender, e) =>
               {
                   Debug.WriteLine("ShutdownRequested");
                   await CodeLogic.CodeLogic.StopAsync();
               };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            //var mainView = new MainView();
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

            services.AddTransient<MainViewModel>();

            var provider = services.BuildServiceProvider();

            var vm = provider.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView.DataContext = vm;
            vm.ShowToast("Manitux Mobile App", NotificationType.Information);
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
}
