using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;

namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Context provided to each library at every lifecycle phase.
/// All paths and services are scoped to this specific library.
/// The same instance is passed to <see cref="ILibrary.OnInitializeAsync"/> and
/// <see cref="ILibrary.OnStartAsync"/> — store it if you need it during OnStopAsync.
/// </summary>
public sealed class LibraryContext
{
    /// <summary>
    /// The library's unique ID (e.g., "CL.SQLite"). Matches <see cref="LibraryManifest.Id"/>.
    /// </summary>
    public required string LibraryId { get; init; }

    /// <summary>
    /// Absolute path to this library's root directory under <c>Libraries/</c>.
    /// Example: <c>CodeLogic/Libraries/CL.SQLite/</c>
    /// </summary>
    public required string LibraryDirectory { get; init; }

    /// <summary>
    /// Absolute path to this library's config directory.
    /// Config files (<c>config.json</c>, <c>config.db.json</c>, etc.) are stored here.
    /// Same as <see cref="LibraryDirectory"/> by default.
    /// </summary>
    public required string ConfigDirectory { get; init; }

    /// <summary>
    /// Absolute path to this library's localization directory.
    /// Per-culture JSON files (e.g., <c>strings.en-US.json</c>) are stored here.
    /// Example: <c>CodeLogic/Libraries/CL.SQLite/localization/</c>
    /// </summary>
    public required string LocalizationDirectory { get; init; }

    /// <summary>
    /// Absolute path to this library's logs directory.
    /// The scoped <see cref="Logger"/> writes here.
    /// Example: <c>CodeLogic/Libraries/CL.SQLite/logs/</c>
    /// </summary>
    public required string LogsDirectory { get; init; }

    /// <summary>
    /// Absolute path to this library's data directory.
    /// Use this for persistent library-owned files (databases, caches, exports, etc.).
    /// Example: <c>CodeLogic/Libraries/CL.SQLite/data/</c>
    /// </summary>
    public required string DataDirectory { get; init; }

    /// <summary>
    /// A scoped logger configured for this library. Log entries are written to
    /// <see cref="LogsDirectory"/> and tagged with the library ID.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Configuration manager scoped to this library's <see cref="ConfigDirectory"/>.
    /// Use this to register, load, save, and reload config models.
    /// </summary>
    public required IConfigurationManager Configuration { get; init; }

    /// <summary>
    /// Localization manager scoped to this library's <see cref="LocalizationDirectory"/>.
    /// Use this to register and retrieve localized string models.
    /// </summary>
    public required ILocalizationManager Localization { get; init; }

    /// <summary>
    /// The shared framework event bus. All libraries and the application share the same instance.
    /// Use this to publish and subscribe to events.
    /// </summary>
    public required IEventBus Events { get; init; }  // shared instance
}
