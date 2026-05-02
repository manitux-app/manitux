namespace CodeLogic;

/// <summary>
/// Configuration options for the CodeLogic framework, passed to <see cref="CodeLogic.InitializeAsync"/>.
/// Control paths, app identity, config-generation behavior, and shutdown handling.
/// </summary>
public sealed class CodeLogicOptions
{
    // === Paths ===

    /// <summary>
    /// Root directory for framework files (CodeLogic.json, Libraries/, logs/).
    /// Relative to the application base directory (<see cref="AppContext.BaseDirectory"/>).
    /// Default: "CodeLogic".
    /// </summary>
    public string FrameworkRootPath { get; set; } = "CodeLogic";

    /// <summary>
    /// Root directory for application files (config, localization, logs, data).
    /// Defaults to <c>{FrameworkRootPath}/Application</c> when null.
    /// Set this to an absolute or relative path to decouple application data from the framework root.
    /// </summary>
    public string? ApplicationRootPath { get; set; } = null;

    // === App identity ===

    /// <summary>
    /// Version string reported in logs, health checks, and <c>--version</c> CLI output.
    /// Set this to your application's semantic version (e.g., "1.2.3").
    /// Stored in <see cref="CodeLogicEnvironment.AppVersion"/> after initialization.
    /// </summary>
    public string AppVersion { get; set; } = "0.0.0";

    // === Config generation ===

    /// <summary>
    /// When true, missing config files are auto-generated with defaults on startup.
    /// Set to false to require all config files to exist before startup (strict mode).
    /// Default: true.
    /// </summary>
    public bool GenerateConfigs { get; set; } = true;

    /// <summary>
    /// When true, ALL config files are regenerated even if they already exist.
    /// This is destructive — existing configs will be overwritten with defaults.
    /// Use with caution; typically triggered by the <c>--generate-configs-force</c> CLI flag.
    /// </summary>
    public bool GenerateConfigsForce { get; set; } = false;

    /// <summary>
    /// Scope config generation to specific library IDs. When null, all libraries are included.
    /// Example: <c>new[] { "CL.SQLite", "CL.Mail" }</c>
    /// Typically set from the <c>--generate-configs CL.SQLite CL.Mail</c> CLI syntax.
    /// </summary>
    public string[]? GenerateConfigsFor { get; set; } = null; // null = all

    /// <summary>
    /// When true, the process exits after generating configs instead of continuing startup.
    /// Default: false. Useful for CI pipelines that generate configs and then exit.
    /// </summary>
    public bool ExitAfterGenerate { get; set; } = false;

    /// <summary>
    /// When true, the framework runs only the Configure phase, validates existing config and
    /// localization files, prints any config files that would be generated or overwritten, and
    /// then exits without starting any libraries or the application.
    /// Useful for CI/CD pipelines that validate startup inputs without mutating the working tree.
    /// Triggered by the <c>--dry-run</c> CLI flag.
    /// </summary>
    public bool DryRun { get; set; } = false;

    // === Shutdown ===

    /// <summary>
    /// When true, hooks <see cref="Console.CancelKeyPress"/> and
    /// <see cref="AppDomain.ProcessExit"/> to call <c>StopAsync()</c> automatically.
    /// Set to false if your host manages the application lifetime externally (e.g., a Windows Service).
    /// Default: true.
    /// </summary>
    public bool HandleShutdownSignals { get; set; } = true;

    // === Path helpers ===

    /// <summary>
    /// Returns the absolute path to the framework root directory.
    /// Combines <see cref="AppContext.BaseDirectory"/> with <see cref="FrameworkRootPath"/>.
    /// </summary>
    public string GetFrameworkPath() =>
        Path.Combine(AppContext.BaseDirectory, FrameworkRootPath);

    /// <summary>
    /// Returns the absolute path to the main framework configuration file (CodeLogic.json).
    /// Located at <c>{FrameworkRoot}/Framework/CodeLogic.json</c>.
    /// </summary>
    public string GetCodeLogicConfigPath() =>
        Path.Combine(GetFrameworkPath(), "Framework", "CodeLogic.json");

    /// <summary>
    /// Path to the development config replacement file (CodeLogic.Development.json).
    /// Loaded instead of CodeLogic.json when the runtime is in development mode.
    /// Add this file to .gitignore — it is per-machine and should never be committed.
    /// </summary>
    public string GetCodeLogicDevelopmentConfigPath() =>
        Path.Combine(GetFrameworkPath(), "Framework", "CodeLogic.Development.json");

    /// <summary>
    /// Returns the absolute path to the Libraries root directory.
    /// Each library lives in a subdirectory named by its ID (e.g., <c>Libraries/CL.SQLite/</c>).
    /// </summary>
    public string GetLibrariesPath() =>
        Path.Combine(GetFrameworkPath(), "Libraries");

    /// <summary>
    /// Returns the absolute path to a specific library's directory.
    /// Normalizes the ID so both "SQLite" and "CL.SQLite" resolve to the same path.
    /// </summary>
    /// <param name="libraryId">The library ID with or without the "CL." prefix.</param>
    public string GetLibraryPath(string libraryId) =>
        Path.Combine(GetLibrariesPath(), NormalizeLibraryId(libraryId));

    /// <summary>
    /// Returns the absolute path to the application root directory.
    /// Uses <see cref="ApplicationRootPath"/> if set; otherwise defaults to
    /// <c>{FrameworkRoot}/Application</c>.
    /// </summary>
    public string GetApplicationPath() =>
        ApplicationRootPath != null
            ? Path.Combine(AppContext.BaseDirectory, ApplicationRootPath)
            : Path.Combine(GetFrameworkPath(), "Application");

    /// <summary>
    /// Returns the absolute path to the application config directory.
    /// Config files (config.json, config.db.json, etc.) are stored here.
    /// </summary>
    public string GetApplicationConfigPath()       => GetApplicationPath();

    /// <summary>
    /// Returns the absolute path to the application localization directory.
    /// Per-culture JSON files (e.g., <c>strings.en-US.json</c>) are stored here.
    /// </summary>
    public string GetApplicationLocalizationPath() => Path.Combine(GetApplicationPath(), "localization");

    /// <summary>
    /// Returns the absolute path to the application logs directory.
    /// The application logger writes to this directory.
    /// </summary>
    public string GetApplicationLogsPath()         => Path.Combine(GetApplicationPath(), "logs");

    /// <summary>
    /// Returns the absolute path to the application data directory.
    /// Use this for persistent application-owned files (databases, caches, etc.).
    /// </summary>
    public string GetApplicationDataPath()         => Path.Combine(GetApplicationPath(), "data");

    /// <summary>
    /// Returns the absolute path to the Plugins directory.
    /// Plugin subdirectories matching <c>*.Plugin</c> are discovered here.
    /// </summary>
    public string GetPluginsPath() =>
        Path.Combine(GetFrameworkPath(), "Plugins");

    private static string NormalizeLibraryId(string id) =>
        id.StartsWith("CL.", StringComparison.OrdinalIgnoreCase) ? $"CL.{id[3..]}" : $"CL.{id}";
}
