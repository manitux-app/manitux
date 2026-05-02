using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;

namespace CodeLogic.Framework.Application;

/// <summary>
/// Context provided to the consuming application during its lifecycle phases.
/// Mirrors <see cref="Libraries.LibraryContext"/> but scoped to the Application directory.
/// The same instance is passed through all lifecycle phases — store it for use during OnStopAsync.
/// </summary>
public sealed class ApplicationContext
{
    /// <summary>
    /// The application's unique ID. Matches <see cref="ApplicationManifest.Id"/>.
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// Absolute path to the application root directory.
    /// Defaults to <c>{FrameworkRoot}/Application/</c>, or <see cref="CodeLogicOptions.ApplicationRootPath"/> if set.
    /// </summary>
    public required string ApplicationDirectory { get; init; }

    /// <summary>
    /// Absolute path to the application config directory.
    /// Config files (<c>config.json</c>, <c>config.database.json</c>, etc.) are stored here.
    /// Same as <see cref="ApplicationDirectory"/> by default.
    /// </summary>
    public required string ConfigDirectory { get; init; }

    /// <summary>
    /// Absolute path to the application localization directory.
    /// Per-culture JSON files (e.g., <c>strings.en-US.json</c>) are stored here.
    /// </summary>
    public required string LocalizationDirectory { get; init; }

    /// <summary>
    /// Absolute path to the application logs directory.
    /// The scoped <see cref="Logger"/> writes here, tagged "APPLICATION".
    /// </summary>
    public required string LogsDirectory { get; init; }

    /// <summary>
    /// Absolute path to the application data directory.
    /// Use this for persistent application-owned files (databases, exports, etc.).
    /// </summary>
    public required string DataDirectory { get; init; }

    /// <summary>
    /// A scoped logger configured for this application. Log entries are written to
    /// <see cref="LogsDirectory"/> and tagged "APPLICATION".
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Configuration manager scoped to this application's <see cref="ConfigDirectory"/>.
    /// Use this to register, load, save, and reload config models.
    /// </summary>
    public required IConfigurationManager Configuration { get; init; }

    /// <summary>
    /// Localization manager scoped to this application's <see cref="LocalizationDirectory"/>.
    /// Use this to register and retrieve localized string models.
    /// </summary>
    public required ILocalizationManager Localization { get; init; }

    /// <summary>
    /// The shared framework event bus. All libraries, the application, and plugins share this instance.
    /// Subscribe to library events or publish application events here.
    /// </summary>
    public required IEventBus Events { get; init; }
}
