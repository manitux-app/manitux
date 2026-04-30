namespace CodeLogic.Core.Logging;

/// <summary>
/// Provides structured log writing for a single named component (library, application, or plugin).
/// Each component receives its own scoped logger from its context object.
/// Log entries are written to disk (and optionally to the console) based on configured levels.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a trace-level message. The most verbose level — for fine-grained
    /// execution flow tracing. Disabled by default in all environments.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Trace(string message);

    /// <summary>
    /// Logs a debug-level message. Intended for diagnostic information during development.
    /// Enabled by default in development (CodeLogic.Development.json sets GlobalLevel to Debug).
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);

    /// <summary>
    /// Logs an informational message about normal application flow.
    /// Use for significant milestones (startup, connections established, jobs completed).
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Info(string message);

    /// <summary>
    /// Logs a warning — an unexpected condition that the application can recover from.
    /// Warnings do not halt execution but should be investigated.
    /// This is the default minimum log level in production.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Warning(string message);

    /// <summary>
    /// Logs an error — a failure in the current operation.
    /// The application can continue but the operation did not succeed.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include in the log entry.</param>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// Logs a critical error — a severe failure that may require immediate attention
    /// or will cause the application to shut down.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">Optional exception to include in the log entry.</param>
    void Critical(string message, Exception? exception = null);

    // Convenience overloads with structured data

    /// <summary>
    /// Logs a formatted debug message using <see cref="string.Format(string, object?[])"/>.
    /// </summary>
    /// <param name="message">A composite format string.</param>
    /// <param name="args">Arguments to substitute into the format string.</param>
    void Debug(string message, params object?[] args) => Debug(string.Format(message, args));

    /// <summary>
    /// Logs a formatted informational message using <see cref="string.Format(string, object?[])"/>.
    /// </summary>
    /// <param name="message">A composite format string.</param>
    /// <param name="args">Arguments to substitute into the format string.</param>
    void Info(string message, params object?[] args) => Info(string.Format(message, args));

    /// <summary>
    /// Logs a formatted warning message using <see cref="string.Format(string, object?[])"/>.
    /// </summary>
    /// <param name="message">A composite format string.</param>
    /// <param name="args">Arguments to substitute into the format string.</param>
    void Warning(string message, params object?[] args) => Warning(string.Format(message, args));

    /// <summary>
    /// Logs a formatted error message using <see cref="string.Format(string, object?[])"/>.
    /// </summary>
    /// <param name="message">A composite format string.</param>
    /// <param name="args">Arguments to substitute into the format string.</param>
    void Error(string message, params object?[] args) => Error(string.Format(message, args));
}
