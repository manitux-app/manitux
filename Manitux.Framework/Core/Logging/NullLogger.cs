namespace CodeLogic.Core.Logging;

/// <summary>
/// A no-op logger that discards all log messages.
/// Use for testing or when logging is optional.
/// </summary>
public sealed class NullLogger : ILogger
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly NullLogger Instance = new();

    private NullLogger() { }

    /// <inheritdoc />
    public void Trace(string message) { }
    /// <inheritdoc />
    public void Debug(string message) { }
    /// <inheritdoc />
    public void Info(string message) { }
    /// <inheritdoc />
    public void Warning(string message) { }
    /// <inheritdoc />
    public void Error(string message, Exception? exception = null) { }
    /// <inheritdoc />
    public void Critical(string message, Exception? exception = null) { }
}
