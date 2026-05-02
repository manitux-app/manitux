namespace CodeLogic.Core.Results;

/// <summary>
/// Represents a structured error with a machine-readable code, human-readable message,
/// optional details, and optional inner error for chaining.
/// Use the static factory methods rather than constructing directly.
/// </summary>
public sealed class Error
{
    /// <summary>
    /// Machine-readable error code in <c>"category.specific_issue"</c> format.
    /// Examples: <c>"user.not_found"</c>, <c>"db.connection_failed"</c>.
    /// Use constants from <see cref="ErrorCode"/> for framework errors.
    /// Libraries should define their own codes following the same convention.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Human-readable description of the error. Suitable for logging and display.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional additional context (e.g., the field name, the invalid value, the file path).
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// Optional chained inner error that caused this error.
    /// Enables building error chains similar to exception inner exceptions.
    /// </summary>
    public Error? InnerError { get; }

    private Error(string code, string message, string? details = null, Error? innerError = null)
    {
        Code = code;
        Message = message;
        Details = details;
        InnerError = innerError;
    }

    // Factory methods

    /// <summary>
    /// Creates an error representing a resource that could not be found.
    /// HTTP analog: 404 Not Found.
    /// </summary>
    public static Error NotFound(string code, string message, string? details = null)
        => new(code, message, details);

    /// <summary>
    /// Creates an error representing a validation failure (invalid input data).
    /// HTTP analog: 422 Unprocessable Entity.
    /// </summary>
    public static Error Validation(string code, string message, string? details = null)
        => new(code, message, details);

    /// <summary>
    /// Creates an error representing an internal/unexpected failure.
    /// HTTP analog: 500 Internal Server Error.
    /// </summary>
    /// <param name="code">Error code identifying the failure.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="details">Optional additional details.</param>
    /// <param name="innerError">Optional chained error that caused this failure.</param>
    public static Error Internal(string code, string message, string? details = null, Error? innerError = null)
        => new(code, message, details, innerError);

    /// <summary>
    /// Creates an error representing an authentication failure.
    /// HTTP analog: 401 Unauthorized.
    /// </summary>
    public static Error Unauthorized(string code, string message, string? details = null)
        => new(code, message, details);

    /// <summary>
    /// Creates an error representing a conflict with current state (duplicate, concurrent modification).
    /// HTTP analog: 409 Conflict.
    /// </summary>
    public static Error Conflict(string code, string message, string? details = null)
        => new(code, message, details);

    /// <summary>
    /// Creates an error representing an operation that exceeded its time limit.
    /// HTTP analog: 408 Request Timeout / 504 Gateway Timeout.
    /// </summary>
    public static Error Timeout(string code, string message, string? details = null)
        => new(code, message, details);

    /// <summary>
    /// Creates an error representing a service or dependency that is temporarily unavailable.
    /// HTTP analog: 503 Service Unavailable.
    /// </summary>
    public static Error Unavailable(string code, string message, string? details = null)
        => new(code, message, details);

    /// <summary>
    /// Wraps an exception as an internal error.
    /// The exception message becomes the error message; the exception type name becomes the details.
    /// </summary>
    /// <param name="ex">The exception to wrap.</param>
    /// <param name="code">Error code to use. Default: "internal.exception".</param>
    public static Error FromException(Exception ex, string code = "internal.exception")
        => new(code, ex.Message, ex.GetType().Name, null);

    /// <summary>
    /// Returns a new Error with the given inner error attached.
    /// Use this to chain errors without mutating the original.
    /// </summary>
    /// <param name="inner">The root cause error.</param>
    public Error WithInner(Error inner) => new(Code, Message, Details, inner);

    /// <summary>
    /// Returns a new Error with the given details string.
    /// Use this to add context without mutating the original.
    /// </summary>
    /// <param name="details">Additional context to attach.</param>
    public Error WithDetails(string details) => new(Code, Message, details, InnerError);

    /// <summary>
    /// Returns a string representation: <c>[code] message (details) → innerError</c>.
    /// </summary>
    public override string ToString() =>
        InnerError is null
            ? $"[{Code}] {Message}{(Details is null ? "" : $" ({Details})")}"
            : $"[{Code}] {Message}{(Details is null ? "" : $" ({Details})")} \u2192 {InnerError}";
}
