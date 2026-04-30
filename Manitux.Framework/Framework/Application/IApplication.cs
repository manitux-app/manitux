using CodeLogic.Framework.Libraries;

namespace CodeLogic.Framework.Application;

/// <summary>
/// Interface for the consuming application to participate in the CodeLogic lifecycle.
/// Register via <see cref="CodeLogic.RegisterApplication"/> before <c>ConfigureAsync()</c>.
/// <para>
/// The application follows the same 4-phase lifecycle as libraries, but with important timing:
/// <list type="bullet">
///   <item><description><see cref="OnConfigureAsync"/> is called during <c>ConfigureAsync()</c>, before libraries start.</description></item>
///   <item><description><see cref="OnInitializeAsync"/> and <see cref="OnStartAsync"/> are called during <c>StartAsync()</c>, AFTER all libraries are running.</description></item>
///   <item><description><see cref="OnStopAsync"/> is called during <c>StopAsync()</c>, BEFORE libraries are stopped.</description></item>
/// </list>
/// This ordering guarantees that library services are available when the application starts,
/// and the application is fully stopped before its dependencies are torn down.
/// </para>
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Application metadata: ID, name, version, description, and author.
    /// Used for display in logs and health reports.
    /// </summary>
    ApplicationManifest Manifest { get; }

    /// <summary>
    /// Phase 1 — Configuration. Called during <c>ConfigureAsync()</c>, before libraries start.
    /// Register config and localization models with the context managers.
    /// The framework generates missing config files and loads all registered configs
    /// immediately after this method returns. Do NOT read config values here.
    /// </summary>
    /// <param name="context">The application context providing scoped services and paths.</param>
    Task OnConfigureAsync(ApplicationContext context);

    /// <summary>
    /// Phase 2 — Initialization. Called during <c>StartAsync()</c>, after all libraries are running.
    /// At this point, all library services are available. Set up application services,
    /// resolve dependencies from libraries, and prepare for operation.
    /// Throw an exception to abort startup.
    /// </summary>
    /// <param name="context">The application context with loaded config and localization.</param>
    Task OnInitializeAsync(ApplicationContext context);

    /// <summary>
    /// Phase 3 — Start. Called during <c>StartAsync()</c>, immediately after <see cref="OnInitializeAsync"/>.
    /// Start background services, subscribe to events, open connections, begin processing.
    /// </summary>
    /// <param name="context">The application context (same instance as Initialize).</param>
    Task OnStartAsync(ApplicationContext context);

    /// <summary>
    /// Phase 4 — Stop. Called during <c>StopAsync()</c> BEFORE libraries are stopped.
    /// Flush pending work, stop background tasks, close connections, and release resources.
    /// After this returns, the framework will stop all libraries.
    /// </summary>
    Task OnStopAsync();

    /// <summary>
    /// Returns the current health of the application.
    /// Called by the framework during scheduled health checks and the <c>--health</c> CLI flag.
    /// The default implementation always returns Healthy — override to add real checks.
    /// </summary>
    /// <returns>A <see cref="HealthStatus"/> reflecting the application's current operational state.</returns>
    Task<Libraries.HealthStatus> HealthCheckAsync() =>
        Task.FromResult(Libraries.HealthStatus.Healthy($"{Manifest.Name} is running"));
}
