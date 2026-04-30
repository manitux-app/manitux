namespace CodeLogic.Core.Logging;

/// <summary>
/// Severity levels for log messages, ordered from least to most severe.
/// Configure the minimum level via <see cref="LoggingConfig.GlobalLevel"/> in CodeLogic.json.
/// Messages below the configured level are silently discarded.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// The most verbose level — extremely detailed execution traces.
    /// Use sparingly; can generate enormous log volume.
    /// Typically only enabled when diagnosing specific bugs in isolation.
    /// </summary>
    Trace    = 0,

    /// <summary>
    /// Diagnostic information useful during development and debugging.
    /// Enabled by default in development (via CodeLogic.Development.json).
    /// Disabled in production to avoid log noise.
    /// </summary>
    Debug    = 1,

    /// <summary>
    /// Normal application events worth recording: startup, connections, job completions.
    /// Use for significant milestones that are expected during healthy operation.
    /// </summary>
    Info     = 2,

    /// <summary>
    /// Unexpected conditions that the application can recover from.
    /// Examples: a retried operation, a missing optional resource, a deprecated API call.
    /// This is the default minimum level in production (CodeLogic.json).
    /// </summary>
    Warning  = 3,

    /// <summary>
    /// A failure in the current operation that the application can continue running despite.
    /// Examples: a failed API call, a failed database query, an unhandled exception in a background task.
    /// </summary>
    Error    = 4,

    /// <summary>
    /// A severe failure that typically requires immediate attention or signals imminent shutdown.
    /// Examples: startup failure, unrecoverable database corruption, out-of-memory conditions.
    /// </summary>
    Critical = 5
}
