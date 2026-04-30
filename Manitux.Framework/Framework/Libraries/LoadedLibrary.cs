namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Internal record of a library registered with LibraryManager.
/// </summary>
public sealed class LoadedLibrary
{
    /// <summary>The library instance.</summary>
    public required ILibrary Instance { get; init; }
    /// <summary>The library manifest describing its identity and dependencies.</summary>
    public required LibraryManifest Manifest { get; init; }
    /// <summary>Filesystem path the library assembly was loaded from.</summary>
    public required string AssemblyPath { get; init; }
    /// <summary>Timestamp when the library was loaded.</summary>
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Context assigned after OnConfigureAsync completes.</summary>
    public LibraryContext? Context { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public LibraryState State { get; set; } = LibraryState.Loaded;

    /// <summary>Exception if State == Failed.</summary>
    public Exception? FailureException { get; set; }
}
