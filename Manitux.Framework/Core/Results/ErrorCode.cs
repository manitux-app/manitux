namespace CodeLogic.Core.Results;

/// <summary>
/// Well-known error code constants. Format: <c>"category.specific_issue"</c>.
/// Libraries may define their own codes following the same convention.
/// These constants are intended to be used with the <see cref="Error"/> factory methods.
/// </summary>
public static class ErrorCode
{
    // General

    /// <summary>The requested resource does not exist.</summary>
    public const string NotFound        = "not_found";

    /// <summary>A resource with this identity already exists.</summary>
    public const string AlreadyExists   = "already_exists";

    /// <summary>An argument provided to an operation is invalid.</summary>
    public const string InvalidArgument = "invalid_argument";

    /// <summary>The operation cannot be performed in the current state.</summary>
    public const string InvalidState    = "invalid_state";

    /// <summary>The requested operation is not supported by this implementation.</summary>
    public const string NotSupported    = "not_supported";

    /// <summary>The operation was cancelled before completion.</summary>
    public const string Cancelled       = "cancelled";

    /// <summary>The operation exceeded its time limit.</summary>
    public const string Timeout         = "timeout";

    /// <summary>An unexpected internal error occurred.</summary>
    public const string Internal        = "internal";

    /// <summary>The caller is not authenticated.</summary>
    public const string Unauthorized    = "unauthorized";

    /// <summary>The caller is authenticated but does not have permission for this operation.</summary>
    public const string Forbidden       = "forbidden";

    // Validation

    /// <summary>One or more validation rules failed.</summary>
    public const string ValidationFailed  = "validation.failed";

    /// <summary>A required field is missing or null.</summary>
    public const string RequiredMissing   = "validation.required_missing";

    /// <summary>A field value does not match the expected format.</summary>
    public const string InvalidFormat     = "validation.invalid_format";

    /// <summary>A numeric value is outside the acceptable range.</summary>
    public const string OutOfRange        = "validation.out_of_range";

    // Configuration

    /// <summary>A required configuration file was not found on disk.</summary>
    public const string ConfigNotFound    = "config.not_found";

    /// <summary>A configuration file exists but contains invalid data.</summary>
    public const string ConfigInvalid     = "config.invalid";

    /// <summary>Failed to read or deserialize a configuration file.</summary>
    public const string ConfigLoadFailed  = "config.load_failed";

    // IO

    /// <summary>A required file was not found on disk.</summary>
    public const string FileNotFound      = "io.file_not_found";

    /// <summary>Failed to read a file from disk.</summary>
    public const string FileReadFailed    = "io.read_failed";

    /// <summary>Failed to write a file to disk.</summary>
    public const string FileWriteFailed   = "io.write_failed";

    // Network / Connection

    /// <summary>Could not establish a connection to a remote endpoint.</summary>
    public const string ConnectionFailed  = "connection.failed";

    /// <summary>An established connection was lost unexpectedly.</summary>
    public const string ConnectionLost    = "connection.lost";

    /// <summary>A connection attempt timed out.</summary>
    public const string ConnectionTimeout = "connection.timeout";

    // Framework

    /// <summary>An operation was attempted before <c>InitializeAsync()</c> was called.</summary>
    public const string NotInitialized   = "framework.not_initialized";

    /// <summary>The framework or a component has already been started.</summary>
    public const string AlreadyStarted   = "framework.already_started";

    /// <summary>A required library was not found in the LibraryManager.</summary>
    public const string LibraryNotFound  = "framework.library_not_found";

    /// <summary>A required plugin was not found in the PluginManager.</summary>
    public const string PluginNotFound   = "framework.plugin_not_found";
}
