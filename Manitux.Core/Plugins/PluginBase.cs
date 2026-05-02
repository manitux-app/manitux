using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;
using Manitux.Core.Application;
using Manitux.Core.Extractors;
using Manitux.Core.Helpers;
using Manitux.Core.Models;

namespace Manitux.Core.Plugins;

public abstract class PluginBase : HttpHelper, IPlugin
{
    public abstract PluginManifest Manifest { get; }
    public abstract PluginConfig Config { get; set; }

    public PluginState State { get; private set; } = PluginState.Loaded;

    //public PluginConfig Config = new();

    public ILogger? Logger { get; set; }

    private IEventBus _eventBus = CodeLogic.CodeLogic.GetEventBus();
    //private IEventSubscription? _eventSub;

    public async Task OnConfigureAsync(PluginContext context)
    {
        context.Configuration.Register<PluginConfig>();

        context.Logger.Debug($"{Manifest.Id} configured");
        State = PluginState.Configured;
        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync(PluginContext context)
    {

        //var path = context.Configuration.GetRegisteredFilePaths();
        //await context.Configuration.ReloadAsync<PluginConfig>();
        await context.Configuration.SaveFirstTimeAsync<PluginConfig>(Config);
        await context.Configuration.LoadAsync<PluginConfig>();

        Config = context.Configuration.Get<PluginConfig>();
        //System.Console.WriteLine("MainUrl: " + Config.MainUrl);

        context.Logger.Debug($"{Manifest.Id} initialized");
        Logger = context.Logger;
        State = PluginState.Initialized;
        await Task.CompletedTask;
    }

    public async Task OnStartAsync(PluginContext context)
    {
        //_evetSub = context.Events.Subscribe<LogEvent>(e =>
        /* _eventSub = _eventBus.Subscribe<LogEvent>(e =>
        {
            switch (e.Level)
            {
                case LogLevel.Info:
                   context.Logger.Info($"{context.PluginId}: {e.Message}");
                   break;
                case LogLevel.Warning:
                   context.Logger.Warning($"{context.PluginId}: {e.Message}");
                   break;
                case LogLevel.Error:
                   context.Logger.Error($"{context.PluginId}: {e.Message}");
                   break;
            }
        }); */

        context.Logger.Debug($"{Manifest.Id} started");
        State = PluginState.Started;
        await Task.CompletedTask;
    }

    public async Task OnUnloadAsync()
    {
        //_eventSub?.Dispose();
        State = PluginState.Stopped;
        await Task.CompletedTask;
    }

    public Task<HealthStatus> HealthCheckAsync()
    {
        return Task.FromResult(HealthStatus.Healthy());
    }

    // public virtual void Dispose()
    // {
    //     //_eventSub?.Dispose();
    // }

    public virtual async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string referer)
    {
        var extractor = ExtractorManager.GetExtractorByUrl(videoSource.Url);
        if (extractor is not null)
        {
            return await extractor.ExtractAsync(videoSource, referer);
        }
        else
        {
            return videoSource;
        }
    }

    public void Log(LogLevel logLevel, string message)
    {
        if (Logger is not null)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    Logger.Info($"{Manifest.Id} {message}");
                    break;
                case LogLevel.Warning:
                    Logger.Warning($"{Manifest.Id} {message}");
                    break;
                case LogLevel.Error:
                    Logger.Error($"{Manifest.Id} {message}");
                    break;
                case LogLevel.Debug:
                    Logger.Debug($"{Manifest.Id} {message}");
                    break;
            }
        }
        else
        {
            LogHelper.Plugin.Log(logLevel, Manifest.Id, message);
        }
    }

    //public abstract Task<string> GetMainPage(string url);
    public abstract Task<List<PageItemModel>?> GetSearchResults(string query);
    public abstract Task<List<CategoryModel>?> GetCategories();
    public abstract Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category);
    public abstract Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem);
    public abstract Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource);

    public void Dispose()
    {
        LogHelper.Plugin.Log(LogLevel.Debug, Manifest.Id, "Disposed");
    }
}
