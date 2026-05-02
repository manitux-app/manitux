namespace CodeLogic.Core.Localization;

/// <summary>
/// Manages localization files for a single component.
/// Each component has per-culture JSON files in its localization/ directory.
/// Non-default cultures fall back to the default culture for missing keys.
/// </summary>
public interface ILocalizationManager
{
    /// <summary>Registers a localization model type.</summary>
    void Register<T>() where T : LocalizationModelBase, new();

    /// <summary>
    /// Gets the localization instance for the given culture.
    /// Falls back to the default culture if the requested one is not available.
    /// </summary>
    T Get<T>(string? culture = null) where T : LocalizationModelBase, new();

    /// <summary>Generates template files for each culture if they don't exist.</summary>
    Task GenerateTemplatesAsync<T>(IReadOnlyList<string> cultures) where T : LocalizationModelBase, new();

    /// <summary>Loads localization files for the given cultures. Generates missing templates only when <paramref name="generateIfMissing"/> is true. Merges non-default cultures with the default.</summary>
    Task LoadAsync<T>(IReadOnlyList<string> cultures, bool generateIfMissing = true) where T : LocalizationModelBase, new();

    /// <summary>Generates templates for all registered types and all cultures.</summary>
    Task GenerateAllTemplatesAsync(IReadOnlyList<string> cultures);

    /// <summary>Loads all registered types for all cultures. Generates missing templates only when <paramref name="generateIfMissing"/> is true.</summary>
    Task LoadAllAsync(IReadOnlyList<string> cultures, bool generateIfMissing = true);

    /// <summary>
    /// Reloads all localizations from disk. Always safe — pure string data.
    /// Useful for live translation updates without restart.
    /// </summary>
    Task ReloadAllAsync(IReadOnlyList<string> cultures);

    /// <summary>Returns the list of culture codes that have been successfully loaded.</summary>
    IReadOnlyList<string> GetLoadedCultures<T>() where T : LocalizationModelBase, new();
}
