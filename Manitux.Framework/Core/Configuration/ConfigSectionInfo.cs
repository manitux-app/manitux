namespace CodeLogic.Core.Configuration;

/// <summary>
/// Lightweight summary of one registered configuration section.
/// Returned by <see cref="IConfigurationManager.GetSections"/> to let admin UIs
/// list what is editable without pulling full schemas for each section.
/// </summary>
public sealed class ConfigSectionInfo
{
    /// <summary>Section identifier (empty string = main <c>config.json</c>).</summary>
    public required string SectionName { get; init; }

    /// <summary>Display title — the class name by default.</summary>
    public required string Title { get; init; }

    /// <summary>Optional description of this section.</summary>
    public string? Description { get; init; }

    /// <summary>Full path of the backing config file on disk.</summary>
    public required string FilePath { get; init; }

    /// <summary>Whether the backing file exists on disk right now.</summary>
    public bool FileExists { get; init; }

    /// <summary>Whether the section has been successfully loaded into memory.</summary>
    public bool IsLoaded { get; init; }
}
