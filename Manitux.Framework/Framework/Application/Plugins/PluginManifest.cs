namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Describes a plugin's identity and framework requirements.
/// Returned by <see cref="IPlugin.Manifest"/> and used by <see cref="PluginManager"/>
/// for tracking, display, and version compatibility checks.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>
    /// Unique identifier for this plugin (e.g., "myapp.dashboard", "homepoint.z-wave").
    /// Used as the key in the PluginManager's internal registry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name (e.g., "Dashboard Plugin").
    /// Shown in console output and health reports.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version string (e.g., "1.0.0").
    /// Shown in console output when the plugin is loaded.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Optional description of what this plugin provides.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional author or team name.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Minimum CodeLogic framework version required to run this plugin (e.g., "3.0.0").
    /// When set, the PluginManager may validate this against the running framework version.
    /// Currently informational — enforcement is the plugin author's responsibility.
    /// </summary>
    public string? MinFrameworkVersion { get; init; }
}
