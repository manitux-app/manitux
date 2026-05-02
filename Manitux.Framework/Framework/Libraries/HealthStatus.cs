namespace CodeLogic.Framework.Libraries;

/// <summary>
/// The three possible health levels for a component.
/// </summary>
public enum HealthStatusLevel
{
    /// <summary>The component is fully operational.</summary>
    Healthy,

    /// <summary>The component is running but with reduced capability (e.g., high latency, partial failure).</summary>
    Degraded,

    /// <summary>The component is not functioning and requires immediate attention.</summary>
    Unhealthy
}

/// <summary>
/// Describes the health status of a single library, plugin, or application.
/// Returned by <see cref="ILibrary.HealthCheckAsync"/>, <c>IPlugin.HealthCheckAsync</c>,
/// and <c>IApplication.HealthCheckAsync</c>.
/// Use the static factory methods to construct instances.
/// </summary>
public sealed class HealthStatus
{
    /// <summary>The health level: Healthy, Degraded, or Unhealthy.</summary>
    public HealthStatusLevel Status { get; init; }

    /// <summary>A human-readable message describing the health state.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional structured data providing additional context (e.g., queue depth, connection count, latency).
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>The UTC timestamp when this health check was performed.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Returns true when <see cref="Status"/> is <see cref="HealthStatusLevel.Healthy"/>.</summary>
    public bool IsHealthy  => Status == HealthStatusLevel.Healthy;

    /// <summary>Returns true when <see cref="Status"/> is <see cref="HealthStatusLevel.Degraded"/>.</summary>
    public bool IsDegraded => Status == HealthStatusLevel.Degraded;

    /// <summary>Returns true when <see cref="Status"/> is <see cref="HealthStatusLevel.Unhealthy"/>.</summary>
    public bool IsUnhealthy => Status == HealthStatusLevel.Unhealthy;

    /// <summary>
    /// Creates a <see cref="HealthStatusLevel.Healthy"/> status with an optional message.
    /// </summary>
    /// <param name="message">A description of the healthy state. Default: "Healthy".</param>
    public static HealthStatus Healthy(string message = "Healthy") =>
        new() { Status = HealthStatusLevel.Healthy, Message = message };

    /// <summary>
    /// Creates a <see cref="HealthStatusLevel.Degraded"/> status.
    /// Use when the component is running but with reduced capability.
    /// </summary>
    /// <param name="message">A description of the degradation (e.g., "High queue depth: 5000").</param>
    public static HealthStatus Degraded(string message) =>
        new() { Status = HealthStatusLevel.Degraded, Message = message };

    /// <summary>
    /// Creates a <see cref="HealthStatusLevel.Unhealthy"/> status.
    /// Use when the component is not functioning.
    /// </summary>
    /// <param name="message">A description of the failure (e.g., "Cannot connect to database").</param>
    public static HealthStatus Unhealthy(string message) =>
        new() { Status = HealthStatusLevel.Unhealthy, Message = message };

    /// <summary>
    /// Creates an <see cref="HealthStatusLevel.Unhealthy"/> status from an exception.
    /// The exception message becomes the status message.
    /// </summary>
    /// <param name="ex">The exception that caused the unhealthy state.</param>
    public static HealthStatus FromException(Exception ex) =>
        new() { Status = HealthStatusLevel.Unhealthy, Message = ex.Message };

    /// <summary>Returns a string in the format "Status: Message".</summary>
    public override string ToString() => $"{Status}: {Message}";
}
