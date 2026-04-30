using System.Reflection;

namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Describes a library's identity and requirements.
/// Returned by <see cref="ILibrary.Manifest"/> and used by <see cref="LibraryManager"/>
/// for discovery, dependency resolution, and display.
/// </summary>
public sealed class LibraryManifest
{
    /// <summary>
    /// Unique identifier for this library. By convention, prefixed with <c>CL.</c>
    /// (e.g., <c>"CL.SQLite"</c>, <c>"CL.Mail"</c>).
    /// The ID determines the library's directory name under <c>Libraries/</c>.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name (e.g., "SQLite Library").
    /// Shown in console output, health reports, and logs.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version string (e.g., "1.0.0").
    /// Checked during dependency validation when a <see cref="LibraryDependency.MinVersion"/> is specified.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Optional description of what this library provides. Shown in <c>--info</c> output.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional author or team name.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Other libraries this library depends on. Used for topological sort during startup.
    /// Required dependencies that are missing will prevent startup.
    /// Optional dependencies that are missing are silently skipped.
    /// </summary>
    public LibraryDependency[] Dependencies { get; init; } = [];

    /// <summary>
    /// Optional tags for categorization and filtering. Not used by the framework at runtime.
    /// </summary>
    public string[] Tags { get; init; } = [];
}

/// <summary>
/// Declares a dependency on another library within a <see cref="LibraryManifest"/>.
/// Use the factory methods (<see cref="Required(string)"/>, <see cref="Optional(string)"/>)
/// rather than constructing directly.
/// </summary>
public sealed record LibraryDependency
{
    /// <summary>
    /// The ID of the required library (e.g., <c>"CL.SQLite"</c>).
    /// Must match the <see cref="LibraryManifest.Id"/> of the depended-upon library exactly.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Minimum acceptable version of the dependency (e.g., <c>"1.2.0"</c>).
    /// When set, the framework validates that the loaded library meets this requirement.
    /// When null, any version is accepted.
    /// </summary>
    public string? MinVersion { get; init; }

    /// <summary>
    /// When true, a missing dependency does not block startup.
    /// When false (default), a missing dependency throws during dependency validation.
    /// </summary>
    public bool IsOptional { get; init; } = false;

    /// <summary>Creates a required dependency with no minimum version constraint.</summary>
    /// <param name="id">The library ID this library depends on.</param>
    public static LibraryDependency Required(string id) => new() { Id = id };

    /// <summary>Creates a required dependency with a minimum version constraint.</summary>
    /// <param name="id">The library ID this library depends on.</param>
    /// <param name="minVersion">The minimum acceptable version (e.g., "1.2.0").</param>
    public static LibraryDependency Required(string id, string minVersion) =>
        new() { Id = id, MinVersion = minVersion };

    /// <summary>Creates an optional dependency — startup continues even if the library is absent.</summary>
    /// <param name="id">The library ID this library optionally depends on.</param>
    public static LibraryDependency Optional(string id) => new() { Id = id, IsOptional = true };

    /// <summary>Creates an optional dependency with a minimum version constraint.</summary>
    /// <param name="id">The library ID this library optionally depends on.</param>
    /// <param name="minVersion">The minimum acceptable version if the library is present.</param>
    public static LibraryDependency Optional(string id, string minVersion) =>
        new() { Id = id, MinVersion = minVersion, IsOptional = true };
}

/// <summary>
/// Applied to library classes to declare dependencies on other libraries.
/// Equivalent to adding entries to <see cref="LibraryManifest.Dependencies"/>.
/// Can be applied multiple times for multiple dependencies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class LibraryDependencyAttribute : Attribute
{
    /// <summary>The ID of the library this library depends on (e.g., "CL.SQLite").</summary>
    public required string Id { get; set; }

    /// <summary>Minimum required version, or null for any version.</summary>
    public string? MinVersion { get; set; }

    /// <summary>When true, startup continues even if this dependency is not loaded.</summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>Converts this attribute to a <see cref="LibraryDependency"/> record.</summary>
    public LibraryDependency ToDependency() => new()
    {
        Id = Id, MinVersion = MinVersion, IsOptional = IsOptional
    };
}
