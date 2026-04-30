using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CodeLogic.Core.Localization;

/// <summary>
/// File-based localization manager for a single component.
/// Files live in the component's localization/ directory.
/// </summary>
public sealed class LocalizationManager : ILocalizationManager
{
    private readonly string _localizationDirectory;
    private readonly string _defaultCulture;

    // Type → Culture → Localization instance
    private readonly Dictionary<Type, Dictionary<string, object>> _localizations = new();
    private readonly Dictionary<Type, bool> _registered = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>Initializes the localization manager with the specified directory and default culture.</summary>
    public LocalizationManager(string localizationDirectory, string defaultCulture = "en-US")
    {
        _localizationDirectory = localizationDirectory;
        _defaultCulture = defaultCulture;
        Directory.CreateDirectory(localizationDirectory);
    }

    /// <inheritdoc />
    public void Register<T>() where T : LocalizationModelBase, new()
    {
        _registered[typeof(T)] = true;
    }

    /// <inheritdoc />
    public T Get<T>(string? culture = null) where T : LocalizationModelBase, new()
    {
        culture ??= _defaultCulture;
        var type = typeof(T);

        if (_localizations.TryGetValue(type, out var cultures))
        {
            if (cultures.TryGetValue(culture, out var localization))
                return (T)localization;

            // Fallback to default culture
            if (culture != _defaultCulture && cultures.TryGetValue(_defaultCulture, out var defaultLoc))
                return (T)defaultLoc;
        }

        throw new InvalidOperationException(
            $"Localization '{type.Name}' for culture '{culture}' is not loaded. " +
            $"Call LoadAllAsync() first.");
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetLoadedCultures<T>() where T : LocalizationModelBase, new()
    {
        var type = typeof(T);
        if (_localizations.TryGetValue(type, out var cultures))
            return cultures.Keys.ToList();
        return [];
    }

    /// <inheritdoc />
    public async Task GenerateTemplatesAsync<T>(IReadOnlyList<string> cultures)
        where T : LocalizationModelBase, new()
    {
        foreach (var culture in cultures)
        {
            var filePath = GetFilePath<T>(culture);
            if (File.Exists(filePath)) continue; // never overwrite existing translations

            var template = new T { Culture = culture };
            var json = JsonSerializer.Serialize(template, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync<T>(IReadOnlyList<string> cultures, bool generateIfMissing = true)
        where T : LocalizationModelBase, new()
    {
        var type = typeof(T);
        _localizations.TryAdd(type, new Dictionary<string, object>());

        T? defaultLocalization = null;

        foreach (var culture in cultures)
        {
            var filePath = GetFilePath<T>(culture);
            if (!File.Exists(filePath))
            {
                if (!generateIfMissing)
                    throw new FileNotFoundException(
                        $"Localization file for '{type.Name}' and culture '{culture}' not found at: {filePath}");

                await GenerateTemplatesAsync<T>([culture]);
            }

            var json = await File.ReadAllTextAsync(filePath);
            T localization;

            if (culture == _defaultCulture)
            {
                localization = JsonSerializer.Deserialize<T>(json, _jsonOptions)
                    ?? throw new InvalidOperationException(
                        $"Failed to deserialize '{type.Name}' localization from {filePath}");
                defaultLocalization = localization;
            }
            else
            {
                defaultLocalization ??= await EnsureDefaultLoadedAsync<T>(generateIfMissing);
                localization = MergeWithDefault(defaultLocalization, json);
            }

            localization.Culture = culture;
            _localizations[type][culture] = localization;

            if (culture == _defaultCulture)
                defaultLocalization = localization;
        }
    }

    /// <inheritdoc />
    public async Task GenerateAllTemplatesAsync(IReadOnlyList<string> cultures)
    {
        foreach (var type in _registered.Keys)
            await InvokeGenericAsync(nameof(GenerateTemplatesAsync), type, cultures);
    }

    /// <inheritdoc />
    public async Task LoadAllAsync(IReadOnlyList<string> cultures, bool generateIfMissing = true)
    {
        foreach (var type in _registered.Keys)
            await InvokeGenericAsync(nameof(LoadAsync), type, cultures, generateIfMissing);
    }

    /// <inheritdoc />
    public async Task ReloadAllAsync(IReadOnlyList<string> cultures)
    {
        // Clear in-memory cache and reload all from disk
        _localizations.Clear();
        await LoadAllAsync(cultures);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private string GetFilePath<T>(string culture) where T : LocalizationModelBase, new()
    {
        var section = GetSectionName<T>();
        return Path.Combine(_localizationDirectory, $"{section}.{culture}.json");
    }

    private static string GetSectionName<T>() where T : LocalizationModelBase, new()
    {
        var attr = typeof(T)
            .GetCustomAttribute<LocalizationSectionAttribute>();
        return !string.IsNullOrWhiteSpace(attr?.SectionName) ? attr.SectionName : typeof(T).Name;
    }

    private async Task<T> EnsureDefaultLoadedAsync<T>(bool generateIfMissing) where T : LocalizationModelBase, new()
    {
        var type = typeof(T);

        if (_localizations.TryGetValue(type, out var cultures) &&
            cultures.TryGetValue(_defaultCulture, out var existing))
            return (T)existing;

        var filePath = GetFilePath<T>(_defaultCulture);
        if (!File.Exists(filePath))
        {
            if (!generateIfMissing)
                throw new FileNotFoundException(
                    $"Localization file for '{type.Name}' and culture '{_defaultCulture}' not found at: {filePath}");

            await GenerateTemplatesAsync<T>([_defaultCulture]);
        }

        var json = await File.ReadAllTextAsync(filePath);
        var localization = JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize default '{type.Name}' localization.");

        localization.Culture = _defaultCulture;
        _localizations.TryAdd(type, new Dictionary<string, object>());
        _localizations[type][_defaultCulture] = localization;
        return localization;
    }

    private T MergeWithDefault<T>(T defaultLocalization, string overrideJson)
        where T : LocalizationModelBase, new()
    {
        var defaultNode = JsonNode.Parse(
            JsonSerializer.Serialize(defaultLocalization, _jsonOptions))?.AsObject()
            ?? throw new InvalidOperationException("Failed to parse default localization for merge.");

        var overrideNode = JsonNode.Parse(overrideJson)?.AsObject();
        if (overrideNode != null)
        {
            foreach (var prop in overrideNode)
            {
                // Only override if the value is non-null and non-empty string
                if (prop.Value != null)
                    defaultNode[prop.Key] = prop.Value.DeepClone();
            }
        }

        return defaultNode.Deserialize<T>(_jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize merged localization.");
    }

    private async Task InvokeGenericAsync(string methodName, Type typeArg, IReadOnlyList<string> cultures, params object[] extraArgs)
    {
        var method = typeof(LocalizationManager)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Reflection failed: method '{methodName}' not found on LocalizationManager.");

        var generic = method.MakeGenericMethod(typeArg);
        var args = new object[1 + extraArgs.Length];
        args[0] = cultures;
        Array.Copy(extraArgs, 0, args, 1, extraArgs.Length);

        var task = generic.Invoke(this, args) as Task
            ?? throw new InvalidOperationException(
                $"Reflection failed: '{methodName}<{typeArg.Name}>' returned null.");

        await task;
    }
}
