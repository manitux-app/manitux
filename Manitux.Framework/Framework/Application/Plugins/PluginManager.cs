using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Libraries;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Manages the full lifecycle of dynamically loaded plugins.
/// </summary>
public sealed class PluginManager : IAsyncDisposable
{
    private readonly IEventBus _eventBus;
    private readonly PluginOptions _options;
    private readonly LoggingOptions _loggingOptions;
    private readonly string _defaultCulture;
    private readonly IReadOnlyList<string> _supportedCultures;
    private readonly Dictionary<string, LoadedPlugin> _plugins = new();
    private readonly Dictionary<string, DateTime> _reloadDebounce = new();
    private readonly object _reloadLock = new();
    private FileSystemWatcher? _watcher;

    /// <summary>Raised when a plugin is successfully loaded.</summary>
    public event Action<string>? OnPluginLoaded;
    /// <summary>Raised when a plugin is unloaded.</summary>
    public event Action<string>? OnPluginUnloaded;
    /// <summary>Raised when a plugin encounters an error during load, unload, or reload.</summary>
    public event Action<string, Exception>? OnPluginError;

    /// <summary>Initializes a new plugin manager with the specified dependencies.</summary>
    public PluginManager(
        IEventBus eventBus,
        PluginOptions? options = null,
        LoggingOptions? loggingOptions = null,
        string defaultCulture = "en-US",
        IReadOnlyList<string>? supportedCultures = null)
    {
        _eventBus          = eventBus;
        _options           = options ?? new PluginOptions();
        _loggingOptions    = loggingOptions ?? new LoggingOptions();
        _defaultCulture    = defaultCulture;
        _supportedCultures = supportedCultures ?? ["en-US"];

        Directory.CreateDirectory(_options.PluginsDirectory);

        if (_options.WatchForChanges)
            StartWatcher();
    }

    // ── Discovery ────────────────────────────────────────────────────────────

    /// <summary>Discovers plugin DLLs in the configured plugins directory.</summary>
    public Task<List<string>> DiscoverAsync()
    {
        var paths = new List<string>();
        if (!Directory.Exists(_options.PluginsDirectory)) return Task.FromResult(paths);

        foreach (var dir in Directory.GetDirectories(_options.PluginsDirectory, "*.Plugin"))
        {
            var name = Path.GetFileName(dir);
            var dll  = Path.Combine(dir, $"{name}.dll");
            if (File.Exists(dll)) paths.Add(dll);
        }
        return Task.FromResult(paths);
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    /// <summary>Loads and starts a single plugin from the specified assembly path.</summary>
    public async Task LoadPluginAsync(string pluginPath)
    {
        if (!File.Exists(pluginPath))
            throw new FileNotFoundException($"Plugin not found: {pluginPath}");

        try
        {
            // test for android?
            bool isAndroid = RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
            Assembly assembly;
            AssemblyLoadContext loadCtx;

            if (isAndroid)
            {
                // Android
                loadCtx = new PluginLoadContextForAndroid(pluginPath);

                byte[] assemblyData = await File.ReadAllBytesAsync(pluginPath);
                using var stream = new MemoryStream(assemblyData);
                assembly = loadCtx.LoadFromStream(stream);
            }
            else
            {
                // Desktop (Windows/Linux/MacOS)
                loadCtx = new PluginLoadContext(pluginPath);
                assembly = loadCtx.LoadFromAssemblyPath(pluginPath);
            }

            //var loadCtx = new PluginLoadContext(pluginPath);
            //var assembly = loadCtx.LoadFromAssemblyPath(pluginPath);

            var pluginType = FindPluginType(assembly)
                    ?? throw new InvalidOperationException($"No IPlugin in {Path.GetFileName(pluginPath)}");

            var plugin   = (IPlugin)Activator.CreateInstance(pluginType)!;
            var manifest = plugin.Manifest;

            var ctx = CreateContext(manifest.Id, pluginPath);

            // Full 4-phase lifecycle
            await plugin.OnConfigureAsync(ctx);
            await ctx.Configuration.GenerateAllDefaultsAsync();
            await ctx.Configuration.LoadAllAsync();
            await ctx.Localization.GenerateAllTemplatesAsync(_supportedCultures);
            await ctx.Localization.LoadAllAsync(_supportedCultures);

            await plugin.OnInitializeAsync(ctx);
            await plugin.OnStartAsync(ctx);

            var loaded = new LoadedPlugin
            {
                Instance      = plugin,
                Manifest      = manifest,
                AssemblyPath  = pluginPath,
                LoadContext   = loadCtx,
                WeakReference = new WeakReference(loadCtx),
                Context       = ctx,
                State         = PluginState.Started
            };

            _plugins[manifest.Id] = loaded;

            _eventBus.Publish(new PluginLoadedEvent(manifest.Id, manifest.Name));
            OnPluginLoaded?.Invoke(manifest.Id);

            //Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  + Plugin loaded: {manifest.Name} v{manifest.Version}");
            //Console.ResetColor();
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new PluginFailedEvent(Path.GetFileName(pluginPath), pluginPath, ex));
            OnPluginError?.Invoke(pluginPath, ex);
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  x Plugin load failed {Path.GetFileName(pluginPath)}: {ex.Message}");
            //Console.ResetColor();
            throw;
        }
    }

    /// <summary>Discovers and loads all plugins from the plugins directory.</summary>
    public async Task LoadAllAsync()
    {
        var paths = await DiscoverAsync();
        foreach (var path in paths)
        {
            try { await LoadPluginAsync(path); }
            catch { /* already logged and evented */ }
        }
    }

    /// <summary>
    /// Registers an in-process plugin (already instantiated and running) with the PluginManager
    /// so that it participates in health checks, <see cref="GetLoadedPlugins"/>, and graceful
    /// shutdown via <see cref="UnloadAllAsync"/>.
    /// <para>
    /// Use this when hosting a plugin in the same process/assembly (e.g. demos, tests) rather
    /// than loading it from a separate DLL via <see cref="LoadPluginAsync"/>. The caller is
    /// responsible for running the full 4-phase lifecycle before calling this method.
    /// </para>
    /// </summary>
    /// <param name="plugin">The already-started plugin instance to register.</param>
    /// <param name="context">The <see cref="PluginContext"/> the plugin was started with.</param>
    public Task RegisterInProcessPluginAsync(IPlugin plugin, PluginContext context)
    {
        var manifest = plugin.Manifest;

        if (_plugins.ContainsKey(manifest.Id))
        {
            Console.WriteLine($"  ! Plugin '{manifest.Name}' already registered");
            return Task.CompletedTask;
        }

        // In-process plugins don't have an AssemblyLoadContext — use a no-op sentinel.
        //var noOpLoadCtx = new PluginLoadContext(typeof(PluginManager).Assembly.Location);
        var noOpLoadCtx = new AssemblyLoadContext(typeof(PluginManager).Assembly.Location);

        _plugins[manifest.Id] = new LoadedPlugin
        {
            Instance      = plugin,
            Manifest      = manifest,
            AssemblyPath  = typeof(PluginManager).Assembly.Location,
            LoadContext   = noOpLoadCtx,
            WeakReference = new WeakReference(noOpLoadCtx),
            Context       = context,
            State         = PluginState.Started
        };

        _eventBus.Publish(new PluginLoadedEvent(manifest.Id, manifest.Name));
        OnPluginLoaded?.Invoke(manifest.Id);

        //Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  + Plugin registered (in-process): {manifest.Name} v{manifest.Version}");
        //Console.ResetColor();

        return Task.CompletedTask;
    }

    // ── Unload ───────────────────────────────────────────────────────────────

    /// <summary>Unloads a plugin by its manifest ID and releases its assembly.</summary>
    public async Task UnloadPluginAsync(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var loaded))
        {
            Console.WriteLine($"  ! Plugin '{pluginId}' not loaded");
            return;
        }

        try
        {
            await loaded.Instance.OnUnloadAsync();
            loaded.Instance.Dispose();
            loaded.LoadContext.Unload();
            loaded.State = PluginState.Stopped;
            _plugins.Remove(pluginId);

            // Allow GC to collect the unloaded assembly
            for (int i = 0; i < 10 && (loaded.WeakReference?.IsAlive ?? false); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(10);
            }

            _eventBus.Publish(new PluginUnloadedEvent(pluginId, loaded.Manifest.Name));
            OnPluginUnloaded?.Invoke(pluginId);
            Console.WriteLine($"  + Plugin unloaded: {loaded.Manifest.Name}");
        }
        catch (Exception ex)
        {
            loaded.State            = PluginState.Failed;
            loaded.FailureException = ex;
            _eventBus.Publish(new PluginFailedEvent(pluginId, loaded.Manifest.Name, ex));
            OnPluginError?.Invoke(pluginId, ex);
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  x Plugin unload error '{pluginId}': {ex.Message}");
            //Console.ResetColor();
        }
    }

    /// <summary>Unloads all currently loaded plugins.</summary>
    public async Task UnloadAllAsync()
    {
        foreach (var id in _plugins.Keys.ToList())
            await UnloadPluginAsync(id);
    }

    /// <summary>Unloads and reloads a plugin from disk.</summary>
    public async Task ReloadPluginAsync(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var loaded))
            throw new InvalidOperationException($"Plugin '{pluginId}' not loaded");

        var path = loaded.AssemblyPath;
        await UnloadPluginAsync(pluginId);
        await Task.Delay(200); // brief pause for file system
        await LoadPluginAsync(path);
    }

    // ── Accessors ────────────────────────────────────────────────────────────

    /// <summary>Gets a loaded plugin instance cast to the specified type.</summary>
    public T? GetPlugin<T>(string pluginId) where T : class, IPlugin =>
        _plugins.TryGetValue(pluginId, out var p) ? p.Instance as T : null;

    /// <summary>Returns all loaded plugin instances.</summary>
    public IEnumerable<IPlugin> GetAllPlugins() => _plugins.Values.Select(p => p.Instance);
    /// <summary>Returns all loaded plugin records including state and metadata.</summary>
    public IEnumerable<LoadedPlugin> GetLoadedPlugins() => _plugins.Values;

    /// <summary>Runs health checks on all started plugins and returns their status.</summary>
    public async Task<Dictionary<string, HealthStatus>> GetHealthAsync()
    {
        var results = new Dictionary<string, HealthStatus>();
        foreach (var (id, loaded) in _plugins)
        {
            if (loaded.State != PluginState.Started) continue;
            try { results[id] = await loaded.Instance.HealthCheckAsync(); }
            catch (Exception ex) { results[id] = HealthStatus.FromException(ex); }
        }
        return results;
    }

    // ── File watcher (hot reload) ─────────────────────────────────────────────

    private void StartWatcher()
    {
        _watcher = new FileSystemWatcher(_options.PluginsDirectory, "*.dll")
        {
            NotifyFilter          = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = true,
            EnableRaisingEvents   = true
        };
        _watcher.Changed += (_, e) => _ = SafeOnFileChangedAsync(e.FullPath);
        _watcher.Created += (_, e) => _ = SafeOnFileChangedAsync(e.FullPath);
        _watcher.Renamed += (_, e) => _ = SafeOnFileChangedAsync(e.FullPath);
    }

    private async Task SafeOnFileChangedAsync(string filePath)
    {
        try { await OnFileChangedAsync(filePath); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PluginManager] File watcher error: {ex.Message}");
            OnPluginError?.Invoke(filePath, ex);
        }
    }

    private async Task OnFileChangedAsync(string filePath)
    {
        if (!_options.EnableHotReload) return;
        if (!filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return;

        var pluginDir = Path.GetDirectoryName(filePath);
        if (pluginDir == null || !pluginDir.EndsWith(".Plugin", StringComparison.OrdinalIgnoreCase)) return;

        // Debounce
        lock (_reloadLock)
        {
            var now = DateTime.UtcNow;
            if (_reloadDebounce.TryGetValue(filePath, out var last) && now - last < _options.ReloadDebounce)
                return;
            _reloadDebounce[filePath] = now;
        }

        await Task.Delay(250); // wait for file write to complete

        var plugin = _plugins.Values.FirstOrDefault(p => p.AssemblyPath == filePath);
        if (plugin == null) return;

        Console.WriteLine($"  ~ Hot-reload: {Path.GetFileName(filePath)}");
        try { await ReloadPluginAsync(plugin.Manifest.Id); }
        catch (Exception ex)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  x Hot-reload failed: {ex.Message}");
            //Console.ResetColor();
        }
    }

    // ── Context creation ─────────────────────────────────────────────────────

    private PluginContext CreateContext(string pluginId, string pluginPath)
    {
        var pluginDir = Path.GetDirectoryName(pluginPath)!;
        var logsDir   = Path.Combine(pluginDir, "logs");
        var dataDir   = Path.Combine(pluginDir, "data");
        var locDir    = Path.Combine(pluginDir, "localization");

        Directory.CreateDirectory(logsDir);
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(locDir);

        var logger = new Logger(pluginId.ToUpper(), logsDir, _loggingOptions.GlobalLevel, _loggingOptions);

        return new PluginContext
        {
            PluginId              = pluginId,
            PluginDirectory       = pluginDir,
            ConfigDirectory       = pluginDir,
            LocalizationDirectory = locDir,
            LogsDirectory         = logsDir,
            DataDirectory         = dataDir,
            Logger                = logger,
            Configuration         = new ConfigurationManager(pluginDir),
            Localization          = new LocalizationManager(locDir, _defaultCulture),
            Events                = _eventBus
        };
    }

    private static Type? FindPluginType(Assembly assembly) =>
        assembly.GetTypes().FirstOrDefault(t =>
            typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    // ── Disposal ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _watcher?.Dispose();
        await UnloadAllAsync();
    }
}
