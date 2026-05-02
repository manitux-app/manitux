using System;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Application;
using Manitux.Core.Plugins;

namespace Manitux.Core.Framework;

public class ManituxFramework
{
    public async Task<PluginManager> InitAsync()
    {
        // ── Step 1: Initialize ────────────────────────────────────────────────────
        var initResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
        {
            opts.FrameworkRootPath = "data/codelogic";
            opts.ApplicationRootPath = "data/app";
            opts.AppVersion = "1.0.0";
            opts.HandleShutdownSignals = false; // önemli!
        });

        if (!initResult.Success || initResult.ShouldExit)
        {
            Console.Error.WriteLine($"Startup failed: {initResult.Message}");
        }

        // ── Step 2: Register libraries (BEFORE ConfigureAsync) ────────────────────
        // LibraryManager is ready after InitializeAsync. Register all libs here.
        // Example — uncomment when you have a CL.SQLite reference:
        //   await Libraries.LoadAsync<CL.SQLite.SQLiteLibrary>();
        //   await Libraries.LoadAsync<CL.Mail.MailLibrary>();

        // ── Step 3: Register the application ──────────────────────────────────────
        CodeLogic.CodeLogic.RegisterApplication(new ManituxApplication());

        // ── Step 4: Configure ─────────────────────────────────────────────────────
        // Discovers DLL libs, configures all registered libs + app in dependency order.
        await CodeLogic.CodeLogic.ConfigureAsync();

        // ── Step 5: Start ─────────────────────────────────────────────────────────
        // Runs Configure on the app and libraries, then Initialize/Start on libraries before the app.
        await CodeLogic.CodeLogic.StartAsync();

        // ── Step 6: Create PluginManager + load in-process plugins ────────────────
        // In a real app, plugins would be DLL files in a Plugins/ folder loaded
        // via pluginMgr.LoadAllAsync(). In this demo we load them directly as
        // in-process classes using LoadInProcessAsync() so the demo is self-contained.
        //
        // The PluginManager is wired to the shared event bus so plugins can pub/sub
        // with the rest of the app. It is registered with the runtime for health
        // checks and graceful shutdown.

        var pluginMgr = new PluginManager(
            CodeLogic.CodeLogic.GetEventBus(),
            new PluginOptions { PluginsDirectory = "data/plugins", EnableHotReload = false });

        // Load our demo plugins directly (no separate DLL needed for in-process plugins)
        await LoadInProcessPluginAsync(pluginMgr, new HdFilmCehennemi());
        await LoadInProcessPluginAsync(pluginMgr, new FilmMakinesi());

        //await pluginMgr.LoadAllAsync();

        // Register with the runtime — health checks + graceful shutdown
        CodeLogic.CodeLogic.SetPluginManager(pluginMgr);

        // print logs
        // var eventBus = CodeLogic.CodeLogic.GetEventBus();
        // using var eventSub = eventBus.Subscribe<LogEvent>(e =>
        // {
        //     Console.WriteLine($"  [{e.Level.ToString()}] {e.Message}");
        // });

        return pluginMgr;
    }
    
    public async Task Init()
    {
        // ── Step 1: Initialize ────────────────────────────────────────────────────
        var initResult = await CodeLogic.CodeLogic.InitializeAsync(opts =>
        {
            opts.FrameworkRootPath = "data/codelogic";
            opts.ApplicationRootPath = "data/app";
            opts.AppVersion = "1.0.0";
            opts.HandleShutdownSignals = false; // önemli!
        });

        if (!initResult.Success || initResult.ShouldExit)
        {
            Console.Error.WriteLine($"Startup failed: {initResult.Message}");
            return;
        }

        // ── Step 2: Register libraries (BEFORE ConfigureAsync) ────────────────────
        // LibraryManager is ready after InitializeAsync. Register all libs here.
        // Example — uncomment when you have a CL.SQLite reference:
        //   await Libraries.LoadAsync<CL.SQLite.SQLiteLibrary>();
        //   await Libraries.LoadAsync<CL.Mail.MailLibrary>();

        // ── Step 3: Register the application ──────────────────────────────────────
        CodeLogic.CodeLogic.RegisterApplication(new ManituxApplication());

        // ── Step 4: Configure ─────────────────────────────────────────────────────
        // Discovers DLL libs, configures all registered libs + app in dependency order.
        await CodeLogic.CodeLogic.ConfigureAsync();

        // ── Step 5: Start ─────────────────────────────────────────────────────────
        // Runs Configure on the app and libraries, then Initialize/Start on libraries before the app.
        await CodeLogic.CodeLogic.StartAsync();

        // ── Step 6: Create PluginManager + load in-process plugins ────────────────
        // In a real app, plugins would be DLL files in a Plugins/ folder loaded
        // via pluginMgr.LoadAllAsync(). In this demo we load them directly as
        // in-process classes using LoadInProcessAsync() so the demo is self-contained.
        //
        // The PluginManager is wired to the shared event bus so plugins can pub/sub
        // with the rest of the app. It is registered with the runtime for health
        // checks and graceful shutdown.

        var pluginMgr = new PluginManager(
            CodeLogic.CodeLogic.GetEventBus(),
            new PluginOptions { PluginsDirectory = "data/plugins", EnableHotReload = false });

        // Load our demo plugins directly (no separate DLL needed for in-process plugins)
        await LoadInProcessPluginAsync(pluginMgr, new HdFilmCehennemi());
        await LoadInProcessPluginAsync(pluginMgr, new FilmMakinesi());

        //await pluginMgr.LoadAllAsync();

        // Register with the runtime — health checks + graceful shutdown
        CodeLogic.CodeLogic.SetPluginManager(pluginMgr);

        // print logs
        // var eventBus = CodeLogic.CodeLogic.GetEventBus();
        // using var eventSub = eventBus.Subscribe<LogEvent>(e =>
        // {
        //     Console.WriteLine($"  [{e.Level.ToString()}] {e.Message}");
        // });
    }

    // ── Helper: load an in-process plugin through the full 4-phase lifecycle ──
    // Normally PluginManager loads plugins from separate DLL files.
    // This helper lets us run in-process plugin classes through the same lifecycle
    // so the demo is self-contained without needing extra projects/DLLs.
    static async Task LoadInProcessPluginAsync(PluginManager manager, IPlugin plugin)
    {
        var ctx = CodeLogic.CodeLogic.GetApplicationContext()
            ?? throw new InvalidOperationException("Application context not available.");

        // Build a PluginContext that reuses the app's paths/services
        var pluginDir = Path.Combine("data/plugins", plugin.Manifest.Id);
        Directory.CreateDirectory(pluginDir);

        var pluginCtx = new PluginContext
        {
            PluginId = plugin.Manifest.Id,
            PluginDirectory = pluginDir,
            ConfigDirectory = pluginDir,
            LocalizationDirectory = Path.Combine(pluginDir, "localization"),
            LogsDirectory = Path.Combine(pluginDir, "logs"),
            DataDirectory = Path.Combine(pluginDir, "data"),
            Logger = ctx.Logger,   // share app logger for demo simplicity
            Configuration = new CodeLogic.Core.Configuration.ConfigurationManager(pluginDir),
            Localization = ctx.Localization,
            Events = ctx.Events
        };

        // Run the 4-phase lifecycle manually
        await plugin.OnConfigureAsync(pluginCtx);

        // this are canceled - manitux
        //await pluginCtx.Configuration.GenerateAllDefaultsAsync(); 
        //await pluginCtx.Configuration.LoadAllAsync();

        await plugin.OnInitializeAsync(pluginCtx);
        await plugin.OnStartAsync(pluginCtx);

        // Register with the PluginManager so the plugin appears in GetLoadedPlugins(),
        // health checks, and is gracefully unloaded on shutdown via UnloadAllAsync().
        await manager.RegisterInProcessPluginAsync(plugin, pluginCtx);

        Console.WriteLine($"  [Plugins] {plugin.Manifest.Id} loaded and started");

        //await CodeLogic.CodeLogic.StopAsync();
    }
}
