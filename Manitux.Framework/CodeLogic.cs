using CodeLogic.Core.Events;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

/// <summary>
/// Static facade for the CodeLogic framework.
/// Delegates all calls to a singleton <see cref="CodeLogicRuntime"/> instance.
/// This is the primary entry point for most applications.
/// <para>
/// Typical usage:
/// <code>
/// var result = await CodeLogic.InitializeAsync(o => o.AppVersion = "1.0.0");
/// if (result.ShouldExit) return;
/// await Libraries.LoadAsync&lt;MyLibrary&gt;();
/// CodeLogic.RegisterApplication(new MyApp());
/// await CodeLogic.ConfigureAsync();
/// await CodeLogic.StartAsync();
/// </code>
/// </para>
/// For testing or advanced scenarios, use <see cref="ICodeLogicRuntime"/> directly.
/// </summary>
public static class CodeLogic
{
    private static readonly ICodeLogicRuntime _runtime = new CodeLogicRuntime();

    /// <summary>
    /// Initializes the framework. Parses CLI args, scaffolds directories on first run,
    /// loads CodeLogic.json, and prepares the LibraryManager.
    /// CLI args are merged on top of programmatic options (CLI wins on conflicts).
    /// </summary>
    /// <param name="configure">Optional delegate to set <see cref="CodeLogicOptions"/> properties.</param>
    /// <returns>
    /// An <see cref="InitializationResult"/> — check <c>ShouldExit</c> before continuing.
    /// A result with <c>ShouldExit = true</c> means a CLI flag like <c>--version</c> was handled.
    /// </returns>
    public static Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null)
        => _runtime.InitializeAsync(configure);

    /// <summary>
    /// Registers the consuming application with the runtime.
    /// Must be called after <see cref="InitializeAsync"/> and before <see cref="ConfigureAsync"/>.
    /// </summary>
    /// <param name="application">The application instance implementing <see cref="IApplication"/>.</param>
    public static void RegisterApplication(IApplication application)
        => _runtime.RegisterApplication(application);

    /// <summary>
    /// Configures the application: discovers DLL libraries, runs <c>OnConfigureAsync</c>,
    /// and generates/loads all config and localization files.
    /// Call after registering all libraries and the application.
    /// </summary>
    public static Task ConfigureAsync()  => _runtime.ConfigureAsync();

    /// <summary>
    /// Starts all libraries and the application in the correct dependency order.
    /// Runs the full Configure → Initialize → Start sequence for libraries,
    /// then Initialize → Start for the application.
    /// </summary>
    public static Task StartAsync()      => _runtime.StartAsync();

    /// <summary>
    /// Gracefully stops the application, then all plugins, then all libraries in reverse order.
    /// Called automatically on CTRL+C and process exit when
    /// <see cref="CodeLogicOptions.HandleShutdownSignals"/> is true.
    /// </summary>
    public static Task StopAsync()       => _runtime.StopAsync();

    /// <summary>
    /// Stops everything and resets the runtime to its uninitialized state.
    /// After calling this, <see cref="InitializeAsync"/> may be called again.
    /// Primarily useful in integration tests.
    /// </summary>
    public static Task ResetAsync()      => _runtime.ResetAsync();

    /// <summary>
    /// Runs a health check across all libraries, plugins, and the application.
    /// Returns a <see cref="HealthReport"/> with per-component status and an overall flag.
    /// </summary>
    public static Task<HealthReport>      GetHealthAsync()           => _runtime.GetHealthAsync();

    /// <summary>
    /// Returns the <see cref="LibraryManager"/>, or null if not yet initialized.
    /// Prefer the <see cref="Libraries"/> facade for day-to-day library access.
    /// </summary>
    public static LibraryManager?         GetLibraryManager()        => _runtime.GetLibraryManager();

    /// <summary>
    /// Returns the registered <see cref="IApplication"/>, or null if none has been registered.
    /// </summary>
    public static IApplication?           GetApplication()           => _runtime.GetApplication();

    /// <summary>
    /// Returns the <see cref="ApplicationContext"/> created during <see cref="ConfigureAsync"/>,
    /// or null if the application has not been configured.
    /// </summary>
    public static ApplicationContext?     GetApplicationContext()     => _runtime.GetApplicationContext();

    /// <summary>
    /// Returns the shared <see cref="IEventBus"/> instance.
    /// Available immediately after construction — even before <see cref="InitializeAsync"/>.
    /// </summary>
    public static IEventBus               GetEventBus()              => _runtime.GetEventBus();

    /// <summary>
    /// Returns the effective <see cref="CodeLogicOptions"/>.
    /// Throws <see cref="InvalidOperationException"/> if called before <see cref="InitializeAsync"/>.
    /// </summary>
    public static CodeLogicOptions        GetOptions()               => _runtime.GetOptions();

    /// <summary>
    /// Returns the deserialized <see cref="CodeLogicConfiguration"/> from CodeLogic.json.
    /// Throws <see cref="InvalidOperationException"/> if called before <see cref="InitializeAsync"/>.
    /// </summary>
    public static CodeLogicConfiguration  GetConfiguration()         => _runtime.GetConfiguration();

    /// <summary>
    /// Registers an app-managed PluginManager so it participates in health
    /// checks and graceful shutdown. Call after InitializeAsync.
    /// </summary>
    public static void SetPluginManager(PluginManager manager)       => _runtime.SetPluginManager(manager);

    /// <summary>
    /// Returns the registered <see cref="PluginManager"/>, or null if none has been registered.
    /// </summary>
    public static PluginManager?          GetPluginManager()         => _runtime.GetPluginManager();

    /// <summary>
    /// Returns <c>true</c> if <see cref="InitializeAsync"/> has completed successfully.
    /// Safe to call at any time — never throws.
    /// </summary>
    public static bool IsInitialized => _runtime.GetLibraryManager() is not null;
}
