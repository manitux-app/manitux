using CodeLogic.Core.Configuration;

namespace CodeLogic.Framework.Libraries;

/// <summary>
/// One row of the "every config section across every loaded library" view.
/// Combines library identity with per-section info so admin UIs, CLI tools,
/// and headless validators can list and group settings without knowing how
/// each library registered its configs.
/// </summary>
/// <remarks>
/// Produced by <see cref="LibraryManager.GetAllConfigSections"/> (and the
/// <c>CodeLogic.Libraries.AllConfigSections()</c> static shorthand).
/// </remarks>
public sealed class ConfigSectionOverview
{
    /// <summary>The library's unique ID (e.g., <c>"CL.MySQL2"</c>). Matches <see cref="LibraryManifest.Id"/>.</summary>
    public required string LibraryId { get; init; }

    /// <summary>The library's display name from its manifest (e.g., <c>"MySQL2 Library"</c>).</summary>
    public required string LibraryName { get; init; }

    /// <summary>
    /// Section identifier within this library. Matches the <c>subConfigName</c>
    /// passed to <see cref="IConfigurationManager.Register{T}"/>. Empty string
    /// means the main <c>config.json</c>.
    /// </summary>
    public required string SectionName { get; init; }

    /// <summary>Display title (the config type's class name by default).</summary>
    public required string Title { get; init; }

    /// <summary>Optional description from the config type's XML doc.</summary>
    public string? Description { get; init; }

    /// <summary>Absolute path to the backing JSON file on disk.</summary>
    public required string FilePath { get; init; }

    /// <summary>Whether the backing file exists right now.</summary>
    public bool FileExists { get; init; }

    /// <summary>Whether the section has been successfully loaded into memory.</summary>
    public bool IsLoaded { get; init; }
}
