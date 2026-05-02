using CodeLogic.Core.Events;
using CodeLogic.Framework.Application;
using CodeLogic.Framework.Application.Plugins;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

/// <summary>
/// The core runtime contract for the CodeLogic framework.
/// Implemented by <see cref="CodeLogicRuntime"/> and exposed via the <see cref="CodeLogic"/> static facade.
/// For testing or advanced multi-runtime scenarios, depend on this interface directly.
/// </summary>
public interface ICodeLogicRuntime
{
    /// <summary>
    /// Initializes the framework: applies options, parses CLI args, scaffolds the directory
    /// structure on first run, loads CodeLogic.json (or CodeLogic.Development.json), and
    /// creates the LibraryManager. Must be called before all other methods.
    /// </summary>
    /// <param name="configure">Optional delegate to configure <see cref="CodeLogicOptions"/> before startup.</param>
    /// <returns>
    /// An <see cref="InitializationResult"/> indicating success, failure, or whether the
    /// process should exit (e.g., after <c>--version</c> or <c>--generate-configs</c>).
    /// </returns>
    Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null);

    /// <summary>
    /// Registers the consuming application with the runtime.
    /// Must be called after <see cref="InitializeAsync"/> and before <see cref="ConfigureAsync"/>.
    /// The application's <c>OnConfigureAsync</c> will be called during <see cref="ConfigureAsync"/>.
    /// </summary>
    /// <param name="application">The application instance to register.</param>
    void RegisterApplication(IApplication application);

    /// <summary>
    /// Configures the application: discovers DLL-based libraries, runs the application's
    /// <c>OnConfigureAsync</c>, generates and loads config and localization files.
    /// Must be called after <see cref="InitializeAsync"/> and before <see cref="StartAsync"/>.
    /// </summary>
    Task ConfigureAsync();

    /// <summary>
    /// Starts all registered libraries (Configure → Initialize → Start phases),
    /// then starts the application (Initialize → Start phases).
    /// Libraries are started in dependency order. Must be called after <see cref="ConfigureAsync"/>.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Gracefully stops the application first, then all plugins, then all libraries (in reverse start order).
    /// Called automatically when shutdown signals are received if <see cref="CodeLogicOptions.HandleShutdownSignals"/> is true.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Stops everything and resets the runtime to uninitialized state.
    /// Allows calling <see cref="InitializeAsync"/> again — useful for integration tests.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Collects health status from all running libraries, plugins, and the application.
    /// Returns a <see cref="HealthReport"/> with an overall <c>IsHealthy</c> flag.
    /// </summary>
    Task<HealthReport> GetHealthAsync();

    /// <summary>
    /// Returns the <see cref="LibraryManager"/> created during <see cref="InitializeAsync"/>,
    /// or null if the runtime has not been initialized.
    /// </summary>
    LibraryManager? GetLibraryManager();

    /// <summary>
    /// Returns the registered <see cref="IApplication"/> instance, or null if none registered.
    /// </summary>
    IApplication? GetApplication();

    /// <summary>
    /// Returns the <see cref="ApplicationContext"/> created during <see cref="ConfigureAsync"/>,
    /// or null if the application has not been configured yet.
    /// </summary>
    ApplicationContext? GetApplicationContext();

    /// <summary>
    /// Returns the shared <see cref="IEventBus"/> instance used across all libraries and the application.
    /// Always available after construction (before initialization).
    /// </summary>
    IEventBus GetEventBus();

    /// <summary>
    /// Returns the <see cref="CodeLogicOptions"/> applied during <see cref="InitializeAsync"/>.
    /// Throws <see cref="InvalidOperationException"/> if not yet initialized.
    /// </summary>
    CodeLogicOptions GetOptions();

    /// <summary>
    /// Returns the deserialized <see cref="CodeLogicConfiguration"/> from CodeLogic.json.
    /// Throws <see cref="InvalidOperationException"/> if not yet initialized.
    /// </summary>
    CodeLogicConfiguration GetConfiguration();

    /// <summary>
    /// Registers an app-managed PluginManager with the runtime so it participates
    /// in health checks and graceful shutdown. Call after InitializeAsync.
    /// </summary>
    void SetPluginManager(PluginManager manager);

    /// <summary>Returns the registered PluginManager, or null if none registered.</summary>
    PluginManager? GetPluginManager();
}
