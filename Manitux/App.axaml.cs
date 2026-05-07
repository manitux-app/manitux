using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Manitux.ViewModels;
using Manitux.Views;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using Manitux.Services.Notifications;
using Avalonia.Controls.Notifications;
using System.Diagnostics;
using Manitux.Services.Applications;

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
            
            //services.AddSingleton<LocalizationService>();
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
            var mainView = new MainView();
            singleViewPlatform.MainView = mainView;

            var topLevel = TopLevel.GetTopLevel(mainView);
            if (topLevel is null)
            {
                throw new InvalidOperationException("Main view top level could not be resolved.");
            }

            services.AddNotificationServices(topLevel);

            //services.AddPluginManagerAsync().ConfigureAwait(false);

            services.AddTransient<MainViewModel>();

            var provider = services.BuildServiceProvider();

            var vm = provider.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView.DataContext = vm;
            vm.ShowToast("Manitux Mobile App", NotificationType.Information);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
