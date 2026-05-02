namespace CodeLogic.Core.Logging;

/// <summary>
/// Runtime logging configuration passed to the <see cref="Logger"/> constructor.
/// Produced from <see cref="CodeLogicConfiguration.Logging"/> via <c>ToLoggingOptions()</c>.
/// Can also be constructed manually for testing or custom components.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Log file organization mode: rolling single file or date-sorted folders.
    /// Default: <see cref="LoggingMode.SingleFile"/>.
    /// </summary>
    public LoggingMode Mode { get; set; } = LoggingMode.SingleFile;

    /// <summary>
    /// Maximum size of a single log file in megabytes before it rolls over.
    /// Applies only in <see cref="LoggingMode.SingleFile"/> mode. Default: 10.
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 10;

    /// <summary>
    /// Number of old (rolled) log files to keep. Oldest are deleted when the limit is exceeded.
    /// Applies only in <see cref="LoggingMode.SingleFile"/> mode. Default: 5.
    /// </summary>
    public int MaxRolledFiles { get; set; } = 5;

    /// <summary>
    /// File name pattern used in <see cref="LoggingMode.DateFolder"/> mode.
    /// Supports <c>{date:format}</c> and <c>{level}</c> tokens.
    /// Default: <c>"{date:yyyy}/{date:MM}/{date:dd}/{level}.log"</c>.
    /// </summary>
    public string FileNamePattern { get; set; } = "{date:yyyy}/{date:MM}/{date:dd}/{level}.log";

    /// <summary>
    /// Minimum log level written to disk. Entries below this level are discarded.
    /// Default: <see cref="LogLevel.Warning"/>.
    /// </summary>
    public LogLevel GlobalLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// When true, enables verbose debug output mode. Default: false.
    /// </summary>
    public bool EnableDebugMode { get; set; } = false;

    /// <summary>
    /// When true, all component loggers also write to a shared centralized debug log file.
    /// Default: false.
    /// </summary>
    public bool CentralizedDebugLog { get; set; } = false;

    /// <summary>
    /// Override path for the centralized debug log file.
    /// When null, defaults to <c>{FrameworkRoot}/Framework/logs/debug.log</c>.
    /// </summary>
    public string? CentralizedLogsPath { get; set; }

    /// <summary>
    /// When true, log entries are also written to the console (stdout). Default: false.
    /// </summary>
    public bool EnableConsoleOutput { get; set; } = false;

    /// <summary>
    /// Minimum log level written to the console when <see cref="EnableConsoleOutput"/> is true.
    /// Default: <see cref="LogLevel.Debug"/>.
    /// </summary>
    public LogLevel ConsoleMinimumLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Timestamp format string applied to each log entry.
    /// Default: <c>"yyyy-MM-dd HH:mm:ss.fff"</c>.
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// When true, the machine name is prepended to each log entry.
    /// Useful when aggregating logs from multiple machines. Default: true.
    /// </summary>
    public bool IncludeMachineName { get; set; } = true;

    /// <summary>
    /// Creates <see cref="LoggingOptions"/> with debug-aware defaults.
    /// When a debugger is attached: sets <see cref="GlobalLevel"/> to Debug,
    /// enables console output, and enables debug mode.
    /// When no debugger: returns quiet defaults (Warning level, no console).
    /// Individual properties can be overridden after calling this method.
    /// </summary>
    public static LoggingOptions CreateWithDebugDefaults()
    {
        var opts = new LoggingOptions();
        if (System.Diagnostics.Debugger.IsAttached)
        {
            opts.GlobalLevel = LogLevel.Debug;
            opts.EnableConsoleOutput = true;
            opts.ConsoleMinimumLevel = LogLevel.Debug;
            opts.EnableDebugMode = true;
        }
        return opts;
    }
}
