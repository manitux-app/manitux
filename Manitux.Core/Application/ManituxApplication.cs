using System;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application;

namespace Manitux.Core.Application;

public class ManituxApplication : IApplication
{
     public ApplicationManifest Manifest { get; } = new()
    {
        Id          = "manitux",
        Name        = "Manitux App",
        Version     = "1.0.0",
        Description = "Manitux Media Stream Hub",
        Author      = "Team Manitux"
    };

    private AppConfig _config = new();
    private AppStrings _strings = new();

    public async Task OnConfigureAsync(ApplicationContext context)
    {
        context.Configuration.Register<AppConfig>();
        context.Localization.Register<AppStrings>();

        context.Logger.Debug("OnConfigureAsync: registered Config and Strings");
        context.Logger.Debug($"OnConfigureAsync: localization directory = {context.LocalizationDirectory}");
        context.Logger.Info($"{Manifest.Name} configured");

        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync(ApplicationContext context)
    {
        _config  = context.Configuration.Get<AppConfig>();
        _strings = context.Localization.Get<AppStrings>("tr-TR");

        context.Logger.Debug("OnInitializeAsync: config and localization loaded");
        context.Logger.Debug($"OnInitializeAsync: AppTitle= {_config.AppTitle}");
        context.Logger.Info($"{Manifest.Name} initialized");

        await Task.CompletedTask;
    }

    public async Task OnStartAsync(ApplicationContext context)
    {
        context.Logger.Debug("OnStartAsync: subscribing to events");

        //context.Events.Subscribe<PluginEvent>(e =>
            //context.Logger.Trace($"Plugin message received: message='{e.Message}'"));

        //context.Events.Subscribe<LogEvent>(e =>
            //context.Logger.Trace($"Log received: log='{e.Message}'"));

        context.Events.Subscribe<LogEvent>(e =>
        {
            switch (e.Level)
            {
                case LogLevel.Info:
                   context.Logger.Info($"{e.EventId} {e.Message}");
                   break;
                case LogLevel.Warning:
                   context.Logger.Warning($"{e.EventId} {e.Message}");
                   break;
                case LogLevel.Error:
                   context.Logger.Error($"{e.EventId} {e.Message}");
                   break;
                case LogLevel.Debug:
                   context.Logger.Debug($"{e.EventId} {e.Message}");
                   break;
            }
        });

        //context.Events.Publish(new LogEvent(LogLevel.None, "", "startup"));

        System.Console.WriteLine(string.Format(_strings.Welcome, _config.AppTitle));
        context.Logger.Info($"{Manifest.Name} started");

        await Task.CompletedTask;
    }

    public async Task OnStopAsync()
    {
        System.Console.WriteLine(_strings.Goodbye);
        await Task.CompletedTask;
    }
}
