namespace CodeLogic.Core.Localization;

/// <summary>
/// Base class for all localization string models.
/// Define string properties with English default values.
/// The localization system serializes these to JSON files per culture.
/// </summary>
public abstract class LocalizationModelBase
{
    /// <summary>Culture code for this localization instance (e.g. "en-US", "da-DK").</summary>
    public string Culture { get; set; } = "en-US";
}

/// <summary>
/// Specifies the section name used in localization file naming.
/// Example: [LocalizationSection("homepoint")] → homepoint.en-US.json
/// Without this attribute, the type name is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LocalizationSectionAttribute : Attribute
{
    /// <summary>
    /// The prefix used in the localization file name.
    /// Example: "homepoint" → file is named <c>homepoint.en-US.json</c>.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Initializes the attribute with a section name.
    /// </summary>
    /// <param name="sectionName">The file name prefix (e.g., "homepoint").</param>
    public LocalizationSectionAttribute(string sectionName) => SectionName = sectionName;
}

/// <summary>
/// Documents a localized string property for translators.
/// Has no runtime effect — purely informational.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LocalizedStringAttribute : Attribute
{
    /// <summary>Optional description for translators explaining the string's context.</summary>
    public string? Description { get; init; }
}
