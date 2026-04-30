using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Context provided to plugins at every lifecycle phase.
/// Full parity with <see cref="Libraries.LibraryContext"/> — includes Config, Localization, Events, and Logger.
/// All paths and services are scoped to this specific plugin's directory.
/// </summary>
public sealed class PluginContext
{
    /// <summary>
    /// The plugin's unique ID. Matches <see cref="PluginManifest.Id"/>.
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// Absolute path to this plugin's directory (the <c>*.Plugin/</c> folder under <c>Plugins/</c>).
    /// Example: <c>CodeLogic/Plugins/MyApp.Plugin/</c>
    /// </summary>
    public required string PluginDirectory { get; init; }

    /// <summary>
    /// Absolute path to this plugin's config directory.
    /// Config files (<c>config.json</c>, etc.) are stored here.
    /// Same as <see cref="PluginDirectory"/> by default.
    /// </summary>
    public required string ConfigDirectory { get; init; }

    /// <summary>
    /// Absolute path to this plugin's localization directory.
    /// Per-culture JSON files (e.g., <c>strings.en-US.json</c>) are stored here.
    /// </summary>
    public required string LocalizationDirectory { get; init; }

    /// <summary>
    /// Absolute path to this plugin's logs directory.
    /// The scoped <see cref="Logger"/> writes here.
    /// </summary>
    public required string LogsDirectory { get; init; }

    /// <summary>
    /// Absolute path to this plugin's data directory.
    /// Use this for persistent plugin-owned files.
    /// </summary>
    public required string DataDirectory { get; init; }

    /// <summary>
    /// A scoped logger configured for this plugin. Log entries are written to
    /// <see cref="LogsDirectory"/> and tagged with the plugin ID.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Configuration manager scoped to this plugin's <see cref="ConfigDirectory"/>.
    /// </summary>
    public required IConfigurationManager Configuration { get; init; }

    /// <summary>
    /// Localization manager scoped to this plugin's <see cref="LocalizationDirectory"/>.
    /// </summary>
    public required ILocalizationManager Localization { get; init; }

    /// <summary>
    /// The shared framework event bus. Plugins share the same event bus as libraries and the application.
    /// </summary>
    public required IEventBus Events { get; init; }
}
