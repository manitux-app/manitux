namespace CodeLogic;

/// <summary>
/// The result of <see cref="ICodeLogicRuntime.InitializeAsync"/>.
/// Always check <see cref="ShouldExit"/> immediately after initialization —
/// CLI flags like <c>--version</c> and <c>--info</c> set this to true.
/// </summary>
public sealed class InitializationResult
{
    /// <summary>
    /// True when initialization completed without errors.
    /// False indicates a fatal startup failure — check <see cref="Message"/> for details.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// True when the framework directory structure was just scaffolded for the first time.
    /// Informational — first-run scaffolding does not prevent normal startup.
    /// </summary>
    public bool IsFirstRun { get; init; }

    /// <summary>
    /// True when the process should exit without continuing startup.
    /// Set by <c>--version</c>, <c>--info</c>, <c>--generate-configs</c> with <c>ExitAfterGenerate=true</c>,
    /// and initialization failures.
    /// </summary>
    public bool ShouldExit { get; init; }

    /// <summary>
    /// A human-readable description of the result. On failure, contains the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// When true, the caller should run the full startup sequence, print a health report
    /// after <c>StartAsync()</c>, then exit. Set by the <c>--health</c> CLI flag.
    /// </summary>
    public bool RunHealthCheck { get; init; }

    /// <summary>
    /// Creates a successful result. <see cref="ShouldExit"/> is false — startup continues normally.
    /// </summary>
    /// <param name="isFirstRun">True if the directory structure was just created.</param>
    /// <param name="runHealthCheck">True if the caller should run a health check after startup.</param>
    public static InitializationResult Succeeded(bool isFirstRun = false, bool runHealthCheck = false) => new()
    {
        Success = true, IsFirstRun = isFirstRun, Message = "Framework initialized successfully",
        RunHealthCheck = runHealthCheck
    };

    /// <summary>
    /// Creates a failed result. <see cref="ShouldExit"/> is true and <see cref="Success"/> is false.
    /// </summary>
    /// <param name="message">A description of why initialization failed.</param>
    public static InitializationResult Failed(string message) => new()
    {
        Success = false, ShouldExit = true, Message = message
    };

    /// <summary>
    /// Creates a successful-but-exit result (e.g., after handling <c>--version</c> or <c>--info</c>).
    /// <see cref="Success"/> is true and <see cref="ShouldExit"/> is true.
    /// </summary>
    /// <param name="message">A description of why the process should exit (e.g., "--version").</param>
    public static InitializationResult Exit(string message) => new()
    {
        Success = true, ShouldExit = true, Message = message
    };
}
