using System.Diagnostics;

namespace CodeLogic;

/// <summary>
/// Provides read-only runtime environment information.
/// AppVersion is set by the consuming application via CodeLogicOptions.AppVersion.
/// </summary>
public static class CodeLogicEnvironment
{
    /// <summary>The machine name from the operating system.</summary>
    public static string MachineName => Environment.MachineName;

    /// <summary>The application base directory (where the executable lives).</summary>
    public static string AppRootPath => AppContext.BaseDirectory;

    /// <summary>
    /// The application version. Set automatically from CodeLogicOptions.AppVersion
    /// during InitializeAsync. Defaults to "0.0.0".
    /// </summary>
    public static string AppVersion { get; internal set; } = "0.0.0";

    /// <summary>
    /// Returns true if a debugger is currently attached to this process.
    /// </summary>
    public static bool IsDebugging => Debugger.IsAttached;

    /// <summary>
    /// Returns true when running in development mode.
    /// True if debugger is attached OR if this is a DEBUG build.
    /// Mirrors the framework's config file selection logic:
    ///   IsDevelopment = true  → CodeLogic.Development.json
    ///   IsDevelopment = false → CodeLogic.json
    /// </summary>
    public static bool IsDevelopment
    {
        get
        {
            if (Debugger.IsAttached) return true;
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
