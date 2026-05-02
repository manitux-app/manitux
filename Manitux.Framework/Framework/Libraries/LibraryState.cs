namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Tracks the lifecycle state of a loaded library.
/// States advance sequentially through the normal lifecycle.
/// <see cref="Failed"/> can be set from any phase when an exception occurs.
/// </summary>
public enum LibraryState
{
    /// <summary>Added to <see cref="LibraryManager"/> but <see cref="ILibrary.OnConfigureAsync"/> has not run yet.</summary>
    Loaded,

    /// <summary><see cref="ILibrary.OnConfigureAsync"/> completed and config/localization files were loaded.</summary>
    Configured,

    /// <summary><see cref="ILibrary.OnInitializeAsync"/> completed successfully.</summary>
    Initialized,

    /// <summary><see cref="ILibrary.OnStartAsync"/> completed — the library is fully operational.</summary>
    Started,

    /// <summary><see cref="ILibrary.OnStopAsync"/> completed — the library has shut down cleanly.</summary>
    Stopped,

    /// <summary>An exception occurred during any lifecycle phase. The library is not operational.</summary>
    Failed
}
