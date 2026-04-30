using System.Text.Json;
using CodeLogic.Framework.Libraries;

namespace CodeLogic;

/// <summary>
/// Aggregated health report from all running libraries, plugins, and the application.
/// Produced by <see cref="ICodeLogicRuntime.GetHealthAsync"/>.
/// <see cref="IsHealthy"/> is true only when ALL components are Healthy.
/// </summary>
public sealed class HealthReport
{
    /// <summary>
    /// Overall health flag. True only when every library, plugin, and the application are Healthy.
    /// False if any component is Degraded or Unhealthy.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>UTC timestamp when this report was generated.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>The machine name from <see cref="CodeLogicEnvironment.MachineName"/>.</summary>
    public string MachineName { get; init; } = Environment.MachineName;

    /// <summary>The application version from <see cref="CodeLogicEnvironment.AppVersion"/>.</summary>
    public string AppVersion { get; init; } = CodeLogicEnvironment.AppVersion;

    /// <summary>
    /// Health status for each library, keyed by library ID (e.g., "CL.SQLite").
    /// Only includes libraries in the <see cref="LibraryState.Started"/> state.
    /// </summary>
    public Dictionary<string, HealthStatus> Libraries { get; init; } = new();

    /// <summary>
    /// Health status for each plugin, keyed by plugin ID.
    /// Only includes plugins in the <see cref="Framework.Application.Plugins.PluginState.Started"/> state.
    /// </summary>
    public Dictionary<string, HealthStatus> Plugins { get; init; } = new();

    /// <summary>
    /// Health status from the registered application, or null if no application is registered.
    /// </summary>
    public HealthStatus? Application { get; init; }

    /// <summary>
    /// Serializes the report to indented JSON, suitable for monitoring systems or REST endpoints.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(new
    {
        isHealthy   = IsHealthy,
        checkedAt   = CheckedAt,
        machineName = MachineName,
        appVersion  = AppVersion,
        libraries   = Libraries.ToDictionary(k => k.Key, v => new { status = v.Value.Status.ToString(), v.Value.Message }),
        plugins     = Plugins.ToDictionary(k => k.Key, v => new { status = v.Value.Status.ToString(), v.Value.Message }),
        application = Application == null ? null : new { status = Application.Status.ToString(), Application.Message }
    }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    /// <summary>
    /// Formats the report as a human-readable console table.
    /// Suitable for terminal output after the <c>--health</c> CLI flag.
    /// </summary>
    public string ToConsoleString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Health Report — {CheckedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Machine: {MachineName}  App: {AppVersion}");
        sb.AppendLine($"Overall: {(IsHealthy ? "HEALTHY" : "UNHEALTHY")}");
        sb.AppendLine();

        if (Libraries.Count > 0)
        {
            sb.AppendLine("Libraries:");
            foreach (var (id, s) in Libraries)
                sb.AppendLine($"  {s.Status,-10} {id}: {s.Message}");
        }
        if (Plugins.Count > 0)
        {
            sb.AppendLine("Plugins:");
            foreach (var (id, s) in Plugins)
                sb.AppendLine($"  {s.Status,-10} {id}: {s.Message}");
        }
        if (Application != null)
            sb.AppendLine($"Application: {Application.Status} — {Application.Message}");

        return sb.ToString();
    }
}
