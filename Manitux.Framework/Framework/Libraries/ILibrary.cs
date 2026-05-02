namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Interface for a CodeLogic library — a self-contained, reusable service component.
/// Libraries participate in a strict 4-phase lifecycle managed by <see cref="LibraryManager"/>.
/// Libraries are started in dependency order and stopped in reverse order.
/// <para>
/// Lifecycle phases (in order):
/// <list type="number">
///   <item><description><see cref="OnConfigureAsync"/> — Register configs and localization models.</description></item>
///   <item><description><see cref="OnInitializeAsync"/> — Set up services using loaded config.</description></item>
///   <item><description><see cref="OnStartAsync"/> — Start background tasks, open connections.</description></item>
///   <item><description><see cref="OnStopAsync"/> — Gracefully stop and release resources.</description></item>
/// </list>
/// </para>
/// </summary>
public interface ILibrary : IDisposable
{
    /// <summary>
    /// Metadata describing this library: ID, name, version, dependencies, etc.
    /// Used by the LibraryManager for discovery, dependency resolution, and display.
    /// </summary>
    LibraryManifest Manifest { get; }

    /// <summary>
    /// Phase 1 — Configuration. Called first, before any other phase.
    /// Register config and localization models with the context managers.
    /// The framework generates missing config files and loads all registered configs
    /// immediately after this method returns.
    /// Do NOT access config values here — they are not loaded yet.
    /// </summary>
    /// <param name="context">The library context providing scoped services and paths.</param>
    Task OnConfigureAsync(LibraryContext context);

    /// <summary>
    /// Phase 2 — Initialization. Called after all configs and localizations are loaded.
    /// Set up services, validate configuration, establish connections, and prepare resources.
    /// Throw an exception here to abort startup — the library will transition to <see cref="LibraryState.Failed"/>.
    /// </summary>
    /// <param name="context">The library context with loaded config and localization.</param>
    Task OnInitializeAsync(LibraryContext context);

    /// <summary>
    /// Phase 3 — Start. Called after all libraries have been initialized.
    /// Start background services, open long-lived connections, begin processing.
    /// After this method returns, the library is considered fully operational.
    /// A <see cref="Core.Events.LibraryStartedEvent"/> is published on the event bus.
    /// </summary>
    /// <param name="context">The library context (same instance as Initialize).</param>
    Task OnStartAsync(LibraryContext context);

    /// <summary>
    /// Phase 4 — Stop. Called in reverse start order during graceful shutdown.
    /// Stop background tasks, close connections, flush buffers, release resources.
    /// Exceptions here are logged but do not prevent other libraries from stopping.
    /// A <see cref="Core.Events.LibraryStoppedEvent"/> is published on the event bus.
    /// </summary>
    Task OnStopAsync();

    /// <summary>
    /// Returns the current health status of this library.
    /// Called on a timer (see <see cref="HealthChecksConfig.IntervalSeconds"/>) and on demand
    /// via the <c>--health</c> CLI flag or <see cref="ICodeLogicRuntime.GetHealthAsync"/>.
    /// Implement this to report database connectivity, queue depth, or any other meaningful status.
    /// </summary>
    /// <returns>
    /// A <see cref="HealthStatus"/> indicating Healthy, Degraded, or Unhealthy.
    /// </returns>
    Task<HealthStatus> HealthCheckAsync();
}
