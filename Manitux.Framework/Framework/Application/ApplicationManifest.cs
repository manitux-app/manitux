namespace CodeLogic.Framework.Application;

/// <summary>
/// Describes an application's identity. Returned by <see cref="IApplication.Manifest"/>.
/// Used in logs, health reports, and the <c>--info</c> CLI output.
/// </summary>
public sealed class ApplicationManifest
{
    /// <summary>
    /// Unique identifier for this application (e.g., "homepoint", "myapp").
    /// Used internally to scope logs and context objects.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name (e.g., "HomePoint").
    /// Shown in console output, health reports, and logs.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version string (e.g., "1.0.0").
    /// Shown in the <c>--version</c> output and health reports.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Optional description of the application's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional author or team name.
    /// </summary>
    public string? Author { get; init; }
}
