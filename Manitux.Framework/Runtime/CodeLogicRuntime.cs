using System.Diagnostics;
using System.Text.Json;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Logging;
using CodeLogic.Core.Utilities;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

/// <summary>
/// Core runtime that orchestrates the CodeLogic framework lifecycle — initialization, configuration, library/plugin management, and health monitoring.
/// </summary>
public sealed class CodeLogicRuntime : ICodeLogicRuntime
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly EventBus _eventBus = new();

    // Framework-level logger that writes to Framework/logs/framework.log.
    // It is created after config is loaded so it respects CodeLogic.json settings.
    private ILogger _frameworkLogger = NullLogger.Instance;

    // App-managed PluginManager, registered via SetPluginManager().
    private PluginManager? _pluginManager;

    private CodeLogicOptions? _options;
    private CodeLogicConfiguration? _config;
    private LibraryManager? _libraryManager;
    private IApplication? _application;
    private ApplicationContext? _applicationContext;
    private System.Threading.Timer? _healthTimer;
    private bool _initialized;
    private bool _shutdownRegistered;

    /// <summary>
    /// Initializes the CodeLogic runtime, loading configuration, discovering libraries, and preparing the framework.
    /// </summary>
    /// <param name="configure">Optional callback to configure runtime options before initialization.</param>
    public async Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null)
    {
        await _lock.WaitAsync();
        try
        {
            if (_initialized)
                return InitializationResult.Failed("CodeLogic already initialized.");

            _options = new CodeLogicOptions();
            configure?.Invoke(_options);

            var cli = CliArgParser.Parse();
            if (cli.GenerateConfigs)            _options.GenerateConfigs = true;
            if (cli.GenerateConfigsForce)       _options.GenerateConfigsForce = true;
            if (cli.GenerateConfigsFor != null) _options.GenerateConfigsFor = cli.GenerateConfigsFor;
            if (cli.DryRun)                     _options.DryRun = true;

            if (cli.ShowVersion)
            {
                Console.WriteLine($"CodeLogic 3.0.0 | App {_options.AppVersion}");
                return InitializationResult.Exit("--version");
            }

            if (cli.ShowInfo)
            {
                Console.WriteLine("CodeLogic 3.0.0");
                Console.WriteLine($"  App version  : {_options.AppVersion}");
                Console.WriteLine($"  Machine      : {Environment.MachineName}");
                Console.WriteLine($"  Framework    : {_options.GetFrameworkPath()}");
                Console.WriteLine($"  Application  : {_options.GetApplicationPath()}");
                Console.WriteLine($"  Libraries    : {_options.GetLibrariesPath()}");
                Console.WriteLine($"  Development  : {IsDevelopmentMode()}");
                return InitializationResult.Exit("--info");
            }

            CodeLogicEnvironment.AppVersion = _options.AppVersion;

            var frameworkRoot = _options.GetFrameworkPath();
            var appRoot = _options.GetApplicationPath();

            if (FirstRunManager.IsFirstRun(frameworkRoot))
            {
                Console.WriteLine("\n  First run detected - scaffolding directory structure...");
                var scaffoldResult = await FirstRunManager.ScaffoldAsync(frameworkRoot, appRoot);
                if (!scaffoldResult.Success)
                    return InitializationResult.Failed($"First-run scaffold failed: {scaffoldResult.Error}");

                Console.WriteLine($"  Created {scaffoldResult.DirectoriesCreated} directories\n");
            }

            await LoadConfigurationAsync();
            ApplyDebugDefaults();

            _frameworkLogger = CreateFrameworkLogger();
            _frameworkLogger.Info("Framework initialized");
            _frameworkLogger.Info($"  Machine  : {CodeLogicEnvironment.MachineName}");
            _frameworkLogger.Info($"  App      : {CodeLogicEnvironment.AppVersion}");
            _frameworkLogger.Info($"  Debug    : {CodeLogicEnvironment.IsDebugging}");
            _frameworkLogger.Info($"  Root     : {GetOptionsOrThrow().GetFrameworkPath()}");
            _frameworkLogger.Info($"  App path : {GetOptionsOrThrow().GetApplicationPath()}");

            var cfg = GetConfigOrThrow();
            var opt = GetOptionsOrThrow();
            _libraryManager = new LibraryManager(_eventBus)
            {
                LoggingOptions = cfg.Logging.ToLoggingOptions(),
                FrameworkRootPath = opt.GetFrameworkPath(),
                DefaultCulture = cfg.Localization.DefaultCulture,
                SupportedCultures = cfg.Localization.SupportedCultures,
                EnableDependencyResolution = cfg.Libraries.EnableDependencyResolution,
                LibraryDiscoveryPattern = cfg.Libraries.DiscoveryPattern
            };
            _frameworkLogger.Info("Library manager created - ready for Libraries.LoadAsync<T>()");

            _initialized = true;

            if (_options.HandleShutdownSignals && !_shutdownRegistered)
            {
                _shutdownRegistered = true;
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    _eventBus.Publish(new ShutdownRequestedEvent("CTRL+C"));
                    _ = StopAsync();
                };
                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    _eventBus.Publish(new ShutdownRequestedEvent("ProcessExit"));
                    StopAsync().GetAwaiter().GetResult();
                };
            }

            return InitializationResult.Succeeded(runHealthCheck: cli.ShowHealth);
        }
        catch (Exception ex)
        {
            return InitializationResult.Failed($"Initialization failed: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Registers the application instance with the runtime.
    /// </summary>
    public void RegisterApplication(IApplication application)
    {
        EnsureInitialized();
        _lock.Wait();
        try
        {
            _application = application;
            _frameworkLogger.Info($"Application registered: {application.Manifest.Name} v{application.Manifest.Version}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Configures the application and all libraries, generating missing config files if needed.
    /// </summary>
    public async Task ConfigureAsync()
    {
        EnsureInitialized();
        await _lock.WaitAsync();
        try
        {
            var opts = GetOptionsOrThrow();
            var config = GetConfigOrThrow();

            var validator = new StartupValidator();
            var validation = validator.Validate(opts.GetFrameworkPath());
            foreach (var warning in validator.GetWarnings())
                _frameworkLogger.Warning(warning);

            if (!validation.IsSuccess)
                throw new InvalidOperationException($"Startup validation failed: {validation.ErrorMessage}");

            var discovered = _libraryManager!.Discover();
            if (discovered.Count > 0)
                await _libraryManager.LoadLibrariesAsync(discovered);

            if (_application != null)
            {
                _frameworkLogger.Info($"Configuring application: {_application.Manifest.Name}");
                var appCtx = CreateApplicationContext();

                await _application.OnConfigureAsync(appCtx);
                await PrepareApplicationConfigurationAsync(appCtx, opts, config);

                _applicationContext = appCtx;
                _frameworkLogger.Info($"Application configured: {_application.Manifest.Name}");
            }

            if (_libraryManager != null)
            {
                await _libraryManager.ConfigureAllAsync(
                    generateMissingConfigs: opts.GenerateConfigs,
                    forceGenerateConfigs: opts.GenerateConfigsForce,
                    configScopeToIds: opts.GenerateConfigsFor,
                    dryRun: opts.DryRun);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Starts the runtime — initializes and starts all libraries, then the application.
    /// </summary>
    public async Task StartAsync()
    {
        EnsureInitialized();
        await _lock.WaitAsync();
        try
        {
            var config = GetConfigOrThrow();
            var opts = GetOptionsOrThrow();

            if (opts.DryRun)
            {
                _frameworkLogger.Info("Dry-run mode - skipping Initialize/Start phases.");
                Console.WriteLine("Dry run complete - all config files validated successfully.");
                return;
            }

            if (opts.GenerateConfigs && opts.ExitAfterGenerate)
            {
                _frameworkLogger.Info("Config generation complete - exiting (--generate-configs).");
                Console.WriteLine("Config files generated. Edit them and restart.");
                return;
            }

            if (_libraryManager != null)
            {
                await _libraryManager.InitializeAllAsync();
                await _libraryManager.StartAllAsync();
            }

            if (_application != null && _applicationContext != null)
            {
                _frameworkLogger.Info($"Starting application: {_application.Manifest.Name}");
                await _application.OnInitializeAsync(_applicationContext);
                await _application.OnStartAsync(_applicationContext);
                _frameworkLogger.Info($"Application started: {_application.Manifest.Name}");
            }

            if (config.HealthChecks.Enabled)
                StartHealthTimer(config.HealthChecks.IntervalSeconds);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void StartHealthTimer(int intervalSeconds)
    {
        _healthTimer?.Dispose();
        var interval = TimeSpan.FromSeconds(intervalSeconds);
        _healthTimer = new System.Threading.Timer(
            _ => _ = RunScheduledHealthCheckAsync(),
            null,
            interval,
            interval);
    }

    private async Task RunScheduledHealthCheckAsync()
    {
        try
        {
            var report = await GetHealthAsync();

            _eventBus.Publish(new HealthCheckCompletedEvent(
                "runtime", report.IsHealthy, report.IsHealthy ? "Healthy" : "Degraded or unhealthy"));

            foreach (var (id, status) in report.Libraries)
                _eventBus.Publish(new HealthCheckCompletedEvent(id, status.IsHealthy, status.Message));

            foreach (var (id, status) in report.Plugins)
                _eventBus.Publish(new HealthCheckCompletedEvent(id, status.IsHealthy, status.Message));

            if (report.Application != null)
            {
                _eventBus.Publish(new HealthCheckCompletedEvent(
                    _application?.Manifest.Id ?? "application",
                    report.Application.IsHealthy,
                    report.Application.Message));
            }
        }
        catch (Exception ex)
        {
            _frameworkLogger.Error($"Scheduled health check error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gracefully stops the application, plugins, and all libraries in reverse order.
    /// </summary>
    public async Task StopAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _healthTimer?.Dispose();
            _healthTimer = null;

            _eventBus.Publish(new ShutdownRequestedEvent("StopAsync called"));

            if (_application != null)
            {
                _frameworkLogger.Info($"Stopping application: {_application.Manifest.Name}");
                try { await _application.OnStopAsync(); }
                catch (Exception ex) { _frameworkLogger.Error($"Application stop error: {ex.Message}", ex); }
            }

            if (_pluginManager != null)
            {
                _frameworkLogger.Info("Unloading plugins...");
                try { await _pluginManager.UnloadAllAsync(); }
                catch (Exception ex) { _frameworkLogger.Error($"Plugin unload error: {ex.Message}", ex); }
            }

            if (_libraryManager != null)
                await _libraryManager.StopAllAsync();

            _frameworkLogger.Info("Framework stopped");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Resets the runtime to its pre-initialized state, stopping everything and clearing all references.
    /// </summary>
    public async Task ResetAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _healthTimer?.Dispose();
            _healthTimer = null;

            if (_application != null)
                try { await _application.OnStopAsync(); } catch { }

            if (_libraryManager != null)
            {
                try { await _libraryManager.StopAllAsync(); } catch { }
                _libraryManager.Dispose();
            }

            _options = null;
            _config = null;
            _libraryManager = null;
            _application = null;
            _applicationContext = null;
            _initialized = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Collects health status from all libraries, plugins, and the application.
    /// </summary>
    public async Task<HealthReport> GetHealthAsync()
    {
        var libraries = _libraryManager != null ? await _libraryManager.GetHealthAsync() : new Dictionary<string, HealthStatus>();
        var plugins = _pluginManager != null ? await _pluginManager.GetHealthAsync() : new Dictionary<string, HealthStatus>();

        HealthStatus? appHealth = null;
        if (_application != null)
        {
            try { appHealth = await _application.HealthCheckAsync(); }
            catch (Exception ex) { appHealth = HealthStatus.FromException(ex); }
        }

        var overall = libraries.Values.All(h => h.IsHealthy)
            && plugins.Values.All(h => h.IsHealthy)
            && (appHealth?.IsHealthy ?? true);

        return new HealthReport
        {
            IsHealthy = overall,
            Libraries = libraries,
            Plugins = plugins,
            Application = appHealth,
        };
    }

    /// <summary>Returns the library manager, or null if not initialized.</summary>
    public LibraryManager? GetLibraryManager() => _libraryManager;
    /// <summary>Returns the registered application, or null if none.</summary>
    public IApplication? GetApplication() => _application;
    /// <summary>Returns the application context, or null if not configured.</summary>
    public ApplicationContext? GetApplicationContext() => _applicationContext;
    /// <summary>Returns the framework event bus.</summary>
    public IEventBus GetEventBus() => _eventBus;

    /// <summary>
    /// Registers the plugin manager with the runtime.
    /// </summary>
    public void SetPluginManager(PluginManager manager)
    {
        _pluginManager = manager;
        _frameworkLogger.Info("PluginManager registered");
    }

    /// <summary>Returns the plugin manager, or null if not set.</summary>
    public PluginManager? GetPluginManager() => _pluginManager;
    /// <summary>Returns the runtime options. Throws if not initialized.</summary>
    public CodeLogicOptions GetOptions() => GetOptionsOrThrow();
    /// <summary>Returns the loaded framework configuration. Throws if not initialized.</summary>
    public CodeLogicConfiguration GetConfiguration() => GetConfigOrThrow();

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("CodeLogic not initialized. Call InitializeAsync() first.");
    }

    private CodeLogicOptions GetOptionsOrThrow() =>
        _options ?? throw new InvalidOperationException("CodeLogic options not available.");

    private CodeLogicConfiguration GetConfigOrThrow() =>
        _config ?? throw new InvalidOperationException("CodeLogic configuration not loaded.");

    private void ApplyDebugDefaults()
    {
        // No-op. Development mode is handled via CodeLogic.Development.json.
    }

    private static bool IsDevelopmentMode()
    {
        if (Debugger.IsAttached) return true;
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    private ILogger CreateFrameworkLogger()
    {
        var opts = GetOptionsOrThrow();
        var config = GetConfigOrThrow();
        var logsDir = Path.Combine(opts.GetFrameworkPath(), "Framework", "logs");
        Directory.CreateDirectory(logsDir);

        var loggingOpts = config.Logging.ToLoggingOptions();
        if (loggingOpts.GlobalLevel > LogLevel.Info)
            loggingOpts.GlobalLevel = LogLevel.Info;

        loggingOpts.EnableConsoleOutput = true;
        loggingOpts.ConsoleMinimumLevel = LogLevel.Info;

        return new Logger("CODELOGIC", logsDir, LogLevel.Info, loggingOpts);
    }

    private async Task LoadConfigurationAsync()
    {
        var opts = GetOptionsOrThrow();
        var devPath = opts.GetCodeLogicDevelopmentConfigPath();
        var basePath = opts.GetCodeLogicConfigPath();

        string configPath;
        if (IsDevelopmentMode() && File.Exists(devPath))
        {
            configPath = devPath;
            var reason = Debugger.IsAttached ? "debugger attached" : "DEBUG build";
            Console.WriteLine($"[CodeLogic] Using {Path.GetFileName(devPath)} ({reason})");
        }
        else
        {
            configPath = basePath;
        }

        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Config not found at: {configPath}");

        var json = await File.ReadAllTextAsync(configPath);
        _config = JsonSerializer.Deserialize<CodeLogicConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException($"Failed to deserialize {Path.GetFileName(configPath)}");
    }

    private ApplicationContext CreateApplicationContext()
    {
        var opts = GetOptionsOrThrow();
        var config = GetConfigOrThrow();

        var appDir = opts.GetApplicationPath();
        var logDir = opts.GetApplicationLogsPath();
        var locDir = opts.GetApplicationLocalizationPath();
        var dataDir = opts.GetApplicationDataPath();

        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(logDir);
        Directory.CreateDirectory(locDir);
        Directory.CreateDirectory(dataDir);

        var loggingOpts = config.Logging.ToLoggingOptions();
        var logger = new Core.Logging.Logger(
            "APPLICATION", logDir, loggingOpts.GlobalLevel, loggingOpts);

        return new ApplicationContext
        {
            ApplicationId = _application!.Manifest.Id,
            ApplicationDirectory = appDir,
            ConfigDirectory = appDir,
            LocalizationDirectory = locDir,
            LogsDirectory = logDir,
            DataDirectory = dataDir,
            Logger = logger,
            Configuration = new ConfigurationManager(appDir),
            Localization = new Core.Localization.LocalizationManager(locDir, config.Localization.DefaultCulture),
            Events = _eventBus
        };
    }

    private async Task PrepareApplicationConfigurationAsync(
        ApplicationContext appCtx,
        CodeLogicOptions options,
        CodeLogicConfiguration config)
    {
        var configuration = appCtx.Configuration as ConfigurationManager
            ?? throw new InvalidOperationException(
                "Application configuration manager must be CodeLogic.Core.Configuration.ConfigurationManager.");
        var canGenerateMissingConfigs = options.GenerateConfigs || options.GenerateConfigsForce;

        if (options.DryRun)
        {
            ReportDryRunConfigActions(configuration, options.GenerateConfigsForce);

            if (!options.GenerateConfigsForce)
                await configuration.ValidateAllAsync(allowMissingFiles: canGenerateMissingConfigs);

            await appCtx.Localization.LoadAllAsync(config.Localization.SupportedCultures, generateIfMissing: false);
            return;
        }

        if (options.GenerateConfigsForce)
            await appCtx.Configuration.GenerateAllDefaultsAsync(force: true);

        await appCtx.Configuration.LoadAllAsync(generateIfMissing: canGenerateMissingConfigs);
        await appCtx.Localization.GenerateAllTemplatesAsync(config.Localization.SupportedCultures);
        await appCtx.Localization.LoadAllAsync(config.Localization.SupportedCultures);
    }

    private static void ReportDryRunConfigActions(ConfigurationManager configuration, bool force)
    {
        foreach (var path in configuration.GetRegisteredFilePaths())
        {
            if (force)
            {
                var action = File.Exists(path) ? "overwrite" : "generate";
                Console.WriteLine($"[dry-run] Would {action}: {GetDisplayPath(path)}");
            }
            else if (!File.Exists(path))
            {
                Console.WriteLine($"[dry-run] Would generate: {GetDisplayPath(path)}");
            }
        }
    }

    private static string GetDisplayPath(string path) =>
        Path.GetRelativePath(AppContext.BaseDirectory, path);
}
