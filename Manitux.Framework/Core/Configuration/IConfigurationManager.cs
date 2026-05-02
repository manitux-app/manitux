namespace CodeLogic.Core.Configuration;

/// <summary>
/// Manages configuration files for a single component (library, application, or plugin).
/// Supports multiple config files per component:
///   Register&lt;MainConfig&gt;()           → config.json
///   Register&lt;DatabaseConfig&gt;("db")   → config.db.json
/// </summary>
public interface IConfigurationManager
{
    // ─── Existing API (unchanged) ─────────────────────────────────────────────

    /// <summary>Registers a config type for this component.</summary>
    void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new();

    /// <summary>Gets a loaded config instance. Throws if not loaded.</summary>
    T Get<T>() where T : ConfigModelBase, new();

    /// <summary>Generates the config file with defaults if it doesn't exist, or overwrites it when <paramref name="force"/> is true.</summary>
    Task GenerateDefaultAsync<T>(bool force = false) where T : ConfigModelBase, new();

    /// <summary>Loads a config from disk. Generates defaults if missing when <paramref name="generateIfMissing"/> is true. Validates after load.</summary>
    Task LoadAsync<T>(bool generateIfMissing = true) where T : ConfigModelBase, new();

    /// <summary>
    /// Saves config to disk. Validates before writing — throws if invalid.
    /// Fires <see cref="ConfigChanged"/> with <see cref="ConfigChangeKind.Updated"/> on success.
    /// </summary>
    Task SaveAsync<T>(T config) where T : ConfigModelBase, new();

    /// <summary>
    /// Saves config to disk first time.
    /// Fires <see cref="ConfigChanged"/> with <see cref="ConfigChangeKind.Updated"/> on success.
    /// </summary>
    Task SaveFirstTimeAsync<T>(T config) where T : ConfigModelBase, new();

    /// <summary>Generates defaults for all registered types that don't have files yet, or overwrites them when <paramref name="force"/> is true.</summary>
    Task GenerateAllDefaultsAsync(bool force = false);

    /// <summary>Loads all registered configs. Generates missing files only when <paramref name="generateIfMissing"/> is true.</summary>
    Task LoadAllAsync(bool generateIfMissing = true);

    /// <summary>Validates all registered config files that already exist on disk. Missing files are allowed only when <paramref name="allowMissingFiles"/> is true.</summary>
    Task ValidateAllAsync(bool allowMissingFiles = false);

    /// <summary>
    /// Reloads a specific config from disk and updates the in-memory instance.
    /// Fires <see cref="ConfigChanged"/> with <see cref="ConfigChangeKind.Reloaded"/> on success.
    /// Safe for: log levels, intervals, pool sizes.
    /// NOT safe for: connection strings, paths, core settings (requires restart).
    /// </summary>
    Task ReloadAsync<T>() where T : ConfigModelBase, new();

    /// <summary>Reloads all registered configs from disk.</summary>
    Task ReloadAllAsync();

    /// <summary>Returns the on-disk paths for all registered config files.</summary>
    IReadOnlyList<string> GetRegisteredFilePaths();

    // ─── Discovery / schema / generic save (added in 3.3.0) ───────────────────

    /// <summary>Enumerate every registered config section. Useful for admin UIs.</summary>
    IReadOnlyList<ConfigSectionInfo> GetSections();

    /// <summary>
    /// Build a <see cref="ConfigSchema"/> for a registered section, merged with the
    /// currently-loaded values. Set <paramref name="includeSecrets"/> to true only
    /// when you need the raw values (e.g. exporting config); the default masks
    /// secret fields.
    /// </summary>
    /// <param name="sectionName">Empty string = main <c>config.json</c>.</param>
    /// <param name="includeSecrets">When true, return real values for fields marked <see cref="ConfigFieldAttribute.Secret"/>; default false returns the mask string.</param>
    /// <exception cref="InvalidOperationException">Section is not registered.</exception>
    ConfigSchema GetSchema(string sectionName, bool includeSecrets = false);

    /// <summary>
    /// Update a section from a raw JSON payload. The payload is deserialized into
    /// the registered type, validated, and (if valid) persisted and reloaded in
    /// memory. Secret fields that echo <see cref="ConfigField.SecretMask"/> are
    /// preserved from the previously-loaded values rather than overwritten.
    /// Fires <see cref="ConfigChanged"/> with <see cref="ConfigChangeKind.Updated"/> on success.
    /// </summary>
    /// <returns>The validation result. When <see cref="ConfigValidationResult.IsValid"/> is false, the file is untouched.</returns>
    Task<ConfigValidationResult> UpdateSectionAsync(
        string sectionName,
        string json,
        string? changedBy = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reset a section to its default values (replaces the file and the in-memory
    /// instance). Fires <see cref="ConfigChanged"/> with <see cref="ConfigChangeKind.Reset"/>.
    /// </summary>
    Task ResetSectionAsync(string sectionName, string? changedBy = null, CancellationToken ct = default);

    /// <summary>
    /// Fired after a successful save / reload / reset. Subscribers typically use
    /// this to refresh cached state derived from the section (connection pools,
    /// thresholds, etc.). Fires on the calling thread; keep handlers fast.
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
}
