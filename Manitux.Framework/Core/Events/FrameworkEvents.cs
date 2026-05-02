namespace CodeLogic.Core.Events;

// ── Lifecycle ─────────────────────────────────────────────────────────────

/// <summary>Published when a library completes OnStartAsync successfully.</summary>
public record LibraryStartedEvent(string LibraryId, string LibraryName) : IEvent;

/// <summary>Published when a library completes OnStopAsync.</summary>
public record LibraryStoppedEvent(string LibraryId, string LibraryName) : IEvent;

/// <summary>Published when a library throws during any lifecycle phase.</summary>
public record LibraryFailedEvent(string LibraryId, string LibraryName, Exception Error) : IEvent;

/// <summary>Published when a plugin is successfully loaded.</summary>
public record PluginLoadedEvent(string PluginId, string PluginName) : IEvent;

/// <summary>Published when a plugin is unloaded.</summary>
public record PluginUnloadedEvent(string PluginId, string PluginName) : IEvent;

/// <summary>Published when a plugin throws during load or unload.</summary>
public record PluginFailedEvent(string PluginId, string PluginName, Exception Error) : IEvent;

// ── Configuration / Localization ──────────────────────────────────────────

/// <summary>Published after a specific config type is reloaded from disk.</summary>
public record ConfigReloadedEvent(string ComponentId, Type ConfigType) : IEvent;

/// <summary>Published after all localizations are reloaded from disk.</summary>
public record LocalizationReloadedEvent(string ComponentId) : IEvent;

// ── Health ────────────────────────────────────────────────────────────────

/// <summary>Published after a scheduled health check round completes.</summary>
public record HealthCheckCompletedEvent(string ComponentId, bool IsHealthy, string Message) : IEvent;

// ── Shutdown ──────────────────────────────────────────────────────────────

/// <summary>Published when the framework begins shutting down.</summary>
public record ShutdownRequestedEvent(string Reason) : IEvent;

// ── Generic bridge — lib-to-lib communication without cross-references ────

/// <summary>
/// Generic bridge event for lib-to-lib signals where a direct type reference
/// would create unwanted coupling. Scope with ComponentId + AlertType.
/// Example: ComponentId="cl.sqlite", AlertType="connection.lost"
/// </summary>
public record ComponentAlertEvent(
    string ComponentId,
    string AlertType,
    string Message,
    object? Payload = null) : IEvent;
