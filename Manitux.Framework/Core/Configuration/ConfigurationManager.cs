using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeLogic.Core.Configuration;

/// <summary>
/// File-based JSON configuration manager for a single component.
/// Thread-safe for concurrent reads; writes use file system atomicity.
/// </summary>
public sealed class ConfigurationManager : IConfigurationManager
{
    private readonly string _baseDirectory;
    private readonly Dictionary<Type, object> _loaded = new();
    private readonly Dictionary<Type, string> _registered = new(); // Type → subConfigName

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <inheritdoc />
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    /// <summary>Initializes the configuration manager with the specified base directory.</summary>
    public ConfigurationManager(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        Directory.CreateDirectory(baseDirectory);
    }

    /// <inheritdoc />
    public void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new()
    {
        _registered[typeof(T)] = subConfigName ?? string.Empty;
    }

    /// <inheritdoc />
    public T Get<T>() where T : ConfigModelBase, new()
    {
        if (_loaded.TryGetValue(typeof(T), out var config))
            return (T)config;

        throw new InvalidOperationException(
            $"Configuration '{typeof(T).Name}' is not loaded. " +
            $"Call LoadAsync<{typeof(T).Name}>() or LoadAllAsync() first.");
    }

    /// <inheritdoc />
    public async Task GenerateDefaultAsync<T>(bool force = false) where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();
        if (!force && File.Exists(path)) return;

        var defaultConfig = new T();
        await WriteToFileAsync(path, defaultConfig);
    }

    /// <inheritdoc />
    public async Task LoadAsync<T>(bool generateIfMissing = true) where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();

        if (!File.Exists(path))
        {
            if (!generateIfMissing)
                throw new FileNotFoundException(
                    $"Config file for '{typeof(T).Name}' not found at: {path}");

            await GenerateDefaultAsync<T>();
        }

        await LoadExistingAsync<T>();
    }

    /// <inheritdoc />
    public async Task SaveAsync<T>(T config) where T : ConfigModelBase, new()
    {
        // Validate BEFORE writing — never persist invalid config
        var validation = config.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Cannot save invalid configuration '{typeof(T).Name}': {validation}");

        var path = GetFilePath<T>();
        await WriteToFileAsync(path, config);

        // Update in-memory cache
        _loaded[typeof(T)] = config;

        FireChanged(typeof(T), ConfigChangeKind.Updated);
    }

    /// <inheritdoc />
    public async Task SaveFirstTimeAsync<T>(T config) where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();
        //Console.WriteLine(path);

        if (!File.Exists(path))
        {
            await WriteToFileAsync(path, config);

            // Update in-memory cache
            _loaded[typeof(T)] = config;

            FireChanged(typeof(T), ConfigChangeKind.Updated);
        }
    }

    /// <inheritdoc />
    public async Task GenerateAllDefaultsAsync(bool force = false)
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(GenerateDefaultAsync), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Reflection failed: method '{nameof(GenerateDefaultAsync)}' not found.");

            var generic = method.MakeGenericMethod(type);
            var task = generic.Invoke(this, [force]) as Task
                ?? throw new InvalidOperationException(
                    $"Reflection failed: '{nameof(GenerateDefaultAsync)}<{type.Name}>' returned null.");

            await task;
        }
    }

    /// <inheritdoc />
    public async Task LoadAllAsync(bool generateIfMissing = true)
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(LoadAsync), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Reflection failed: method '{nameof(LoadAsync)}' not found.");

            var generic = method.MakeGenericMethod(type);
            var task = generic.Invoke(this, [generateIfMissing]) as Task
                ?? throw new InvalidOperationException(
                    $"Reflection failed: '{nameof(LoadAsync)}<{type.Name}>' returned null.");

            await task;
        }
    }

    /// <inheritdoc />
    public async Task ValidateAllAsync(bool allowMissingFiles = false)
    {
        foreach (var type in _registered.Keys)
        {
            var path = GetFilePath(type);
            if (!File.Exists(path))
            {
                if (allowMissingFiles)
                    continue;

                throw new FileNotFoundException(
                    $"Config file for '{type.Name}' not found at: {path}");
            }

            await LoadExistingAsync(type);
        }
    }

    /// <inheritdoc />
    public async Task ReloadAsync<T>() where T : ConfigModelBase, new()
    {
        // Same as LoadAsync but always re-reads from disk even if already loaded
        var path = GetFilePath<T>();

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Config file for '{typeof(T).Name}' not found at: {path}. " +
                $"Call GenerateDefaultAsync<{typeof(T).Name}>() first.");

        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize '{typeof(T).Name}' from {path}");

        var validation = config.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Reloaded configuration '{typeof(T).Name}' is invalid: {validation}");

        _loaded[typeof(T)] = config;

        FireChanged(typeof(T), ConfigChangeKind.Reloaded);
    }

    /// <inheritdoc />
    public async Task ReloadAllAsync()
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(ReloadAsync), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"Reflection failed: method '{nameof(ReloadAsync)}' not found.");

            var generic = method.MakeGenericMethod(type);
            var task = generic.Invoke(this, null) as Task
                ?? throw new InvalidOperationException(
                    $"Reflection failed: '{nameof(ReloadAsync)}<{type.Name}>' returned null.");

            await task;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRegisteredFilePaths() =>
        _registered.Keys.Select(GetFilePath).ToList();

    // ─── 3.3.0 additions ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<ConfigSectionInfo> GetSections()
    {
        var result = new List<ConfigSectionInfo>(_registered.Count);
        foreach (var (type, subName) in _registered)
        {
            var path = GetFilePath(type);
            result.Add(new ConfigSectionInfo
            {
                SectionName = subName,
                Title = type.Name,
                FilePath = path,
                FileExists = File.Exists(path),
                IsLoaded = _loaded.ContainsKey(type)
            });
        }
        return result;
    }

    /// <inheritdoc />
    public ConfigSchema GetSchema(string sectionName, bool includeSecrets = false)
    {
        var type = ResolveTypeForSection(sectionName);
        _loaded.TryGetValue(type, out var instance);
        return ConfigSchemaBuilder.Build(type, sectionName, instance, includeSecrets);
    }

    /// <inheritdoc />
    public async Task<ConfigValidationResult> UpdateSectionAsync(
        string sectionName,
        string json,
        string? changedBy = null,
        CancellationToken ct = default)
    {
        var type = ResolveTypeForSection(sectionName);

        object deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize(json, type, _jsonOptions)
                ?? throw new InvalidOperationException("Deserialized config was null.");
        }
        catch (JsonException ex)
        {
            return ConfigValidationResult.Invalid($"Invalid JSON: {ex.Message}");
        }

        // Preserve any masked secret fields by copying from the existing loaded instance.
        if (_loaded.TryGetValue(type, out var previous))
            PreserveMaskedSecrets(type, previous, deserialized);

        var validation = ((ConfigModelBase)deserialized).Validate();
        if (!validation.IsValid)
            return validation;

        var path = GetFilePath(type);
        await WriteObjectToFileAsync(path, deserialized, type, ct).ConfigureAwait(false);
        _loaded[type] = deserialized;

        FireChanged(type, ConfigChangeKind.Updated, changedBy);
        return ConfigValidationResult.Valid();
    }

    /// <inheritdoc />
    public async Task ResetSectionAsync(string sectionName, string? changedBy = null, CancellationToken ct = default)
    {
        var type = ResolveTypeForSection(sectionName);
        var defaults = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not instantiate '{type.Name}' for reset.");
        var path = GetFilePath(type);
        await WriteObjectToFileAsync(path, defaults, type, ct).ConfigureAwait(false);
        _loaded[type] = defaults;
        FireChanged(type, ConfigChangeKind.Reset, changedBy);
    }

    // ─── Internals ────────────────────────────────────────────────────────────

    private Type ResolveTypeForSection(string sectionName)
    {
        var key = sectionName ?? string.Empty;
        foreach (var (type, subName) in _registered)
        {
            if (string.Equals(subName, key, StringComparison.Ordinal))
                return type;
        }
        throw new InvalidOperationException(
            $"No config registered for section '{key}'. Known sections: {string.Join(", ", _registered.Values.Select(s => $"'{s}'"))}");
    }

    /// <summary>
    /// Walk <paramref name="incoming"/> and replace any string property that
    /// equals <see cref="ConfigField.SecretMask"/> with the corresponding value
    /// from <paramref name="previous"/>. Recurses into nested objects and
    /// <c>Dictionary&lt;string,X&gt;</c> entries.
    /// </summary>
    private static void PreserveMaskedSecrets(Type type, object previous, object incoming)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var prop in props)
        {
            var pt = prop.PropertyType;

            if (pt == typeof(string))
            {
                var incomingValue = prop.GetValue(incoming) as string;
                if (incomingValue == ConfigField.SecretMask)
                {
                    var previousValue = prop.GetValue(previous);
                    prop.SetValue(incoming, previousValue);
                }
                continue;
            }

            if (pt.IsClass && pt != typeof(string) && !pt.IsArray)
            {
                var prevNested = prop.GetValue(previous);
                var incomingNested = prop.GetValue(incoming);
                if (prevNested is null || incomingNested is null) continue;

                // Dictionary<string, X>
                if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                    pt.GetGenericArguments()[0] == typeof(string))
                {
                    var valueType = pt.GetGenericArguments()[1];
                    var prevDict = (System.Collections.IDictionary)prevNested;
                    var incomingDict = (System.Collections.IDictionary)incomingNested;
                    foreach (var key in incomingDict.Keys)
                    {
                        if (prevDict.Contains(key))
                        {
                            var incomingEntry = incomingDict[key];
                            var previousEntry = prevDict[key];
                            if (incomingEntry is not null && previousEntry is not null)
                                PreserveMaskedSecrets(valueType, previousEntry, incomingEntry);
                        }
                    }
                    continue;
                }

                PreserveMaskedSecrets(pt, prevNested, incomingNested);
            }
        }
    }

    private void FireChanged(Type type, ConfigChangeKind kind, string? changedBy = null)
    {
        var handler = ConfigChanged;
        if (handler is null) return;
        var section = _registered.TryGetValue(type, out var subName) ? subName : string.Empty;
        handler(this, new ConfigChangedEventArgs
        {
            SectionName = section,
            Kind = kind,
            ChangedBy = changedBy
        });
    }

    private string GetFilePath<T>() where T : ConfigModelBase, new()
    {
        return GetFilePath(typeof(T));
    }

    private async Task WriteToFileAsync<T>(string path, T config) where T : ConfigModelBase, new()
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private async Task WriteObjectToFileAsync(string path, object config, Type type, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, type, _jsonOptions);
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

    private string GetFilePath(Type type)
    {
        if (!_registered.TryGetValue(type, out var subName))
            throw new InvalidOperationException(
                $"Configuration type '{type.Name}' is not registered. " +
                $"Call Register<{type.Name}>() in OnConfigureAsync first.");

        var fileName = string.IsNullOrEmpty(subName) ? "config.json" : $"config.{subName}.json";
        return Path.Combine(_baseDirectory, fileName);
    }

    private async Task LoadExistingAsync(Type type)
    {
        var method = typeof(ConfigurationManager)
            .GetMethod(nameof(LoadExistingAsync), BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException(
                $"Reflection failed: method '{nameof(LoadExistingAsync)}' not found.");

        var generic = method.MakeGenericMethod(type);
        var task = generic.Invoke(this, null) as Task
            ?? throw new InvalidOperationException(
                $"Reflection failed: '{nameof(LoadExistingAsync)}<{type.Name}>' returned null.");

        await task;
    }

    private async Task LoadExistingAsync<T>() where T : ConfigModelBase, new()
    {
        var path = GetFilePath<T>();
        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize '{typeof(T).Name}' from {path}");

        var validation = config.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Configuration '{typeof(T).Name}' is invalid: {validation}");

        _loaded[typeof(T)] = config;
    }
}
