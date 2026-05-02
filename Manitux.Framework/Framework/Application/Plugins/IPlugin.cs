using CodeLogic.Framework.Libraries;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Interface for hot-loadable CodeLogic plugins.
/// Plugins are app-managed: the consuming application decides what to load via <see cref="PluginManager"/>.
/// Plugins follow the same 4-phase lifecycle as libraries but are loaded into isolated
/// <see cref="PluginLoadContext"/> instances, enabling true hot-reload without restarting the host.
/// <para>
/// Plugin assemblies are discovered from <c>{FrameworkRoot}/Plugins/*.Plugin/*.Plugin.dll</c>.
/// </para>
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Plugin metadata: ID, name, version, description, author, and minimum framework version.
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Current lifecycle state of this plugin instance.
    /// Transitions from <see cref="PluginState.Discovered"/> through to <see cref="PluginState.Started"/>.
    /// </summary>
    PluginState State { get; }

    /// <summary>
    /// Phase 1 — Configuration. Called first when the plugin is loaded.
    /// Register config and localization models with the context managers.
    /// The framework generates missing config files and loads all registered configs
    /// immediately after this method returns. Do NOT read config values here.
    /// </summary>
    /// <param name="context">The plugin context providing scoped services and paths.</param>
    Task OnConfigureAsync(PluginContext context);

    /// <summary>
    /// Phase 2 — Initialization. Called after configs and localization are loaded.
    /// Set up plugin services, validate configuration, and prepare resources.
    /// Throw an exception here to abort loading — the plugin will transition to <see cref="PluginState.Failed"/>.
    /// </summary>
    /// <param name="context">The plugin context with loaded config and localization.</param>
    Task OnInitializeAsync(PluginContext context);

    /// <summary>
    /// Phase 3 — Start. Called after initialization completes.
    /// Start background services, subscribe to events, and begin processing.
    /// After this returns, the plugin is considered fully operational.
    /// A <see cref="Core.Events.PluginLoadedEvent"/> is published on the event bus.
    /// </summary>
    /// <param name="context">The plugin context (same instance as Initialize).</param>
    Task OnStartAsync(PluginContext context);

    /// <summary>
    /// Phase 4 — Unload. Called when the plugin is being unloaded (shutdown, hot-reload, or manual unload).
    /// Stop background tasks, close connections, flush buffers, and release resources.
    /// After this returns, the plugin's assembly will be unloaded from memory.
    /// </summary>
    Task OnUnloadAsync();

    /// <summary>
    /// Returns the current health status of this plugin.
    /// Called during scheduled health checks and the <c>--health</c> CLI flag.
    /// </summary>
    Task<HealthStatus> HealthCheckAsync();
}
