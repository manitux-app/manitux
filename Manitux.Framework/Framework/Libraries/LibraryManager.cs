using System.Reflection;
using CodeLogic.Core.Configuration;
using CodeLogic.Core.Events;
using CodeLogic.Core.Localization;
using CodeLogic.Core.Logging;
using CodeLogic.Core.Utilities;

namespace CodeLogic.Framework.Libraries;

/// <summary>
/// Manages the full lifecycle of all registered CL.* libraries.
/// Thread-safe: lifecycle methods are serialized via SemaphoreSlim.
/// </summary>
public sealed class LibraryManager : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<LoadedLibrary> _libraries = [];
    private readonly Dictionary<string, ILibrary> _librariesById = new();
    private readonly IEventBus _eventBus;

    /// <summary>Logging options applied to library loggers.</summary>
    public LoggingOptions LoggingOptions { get; set; } = new();
    /// <summary>Root path of the CodeLogic framework directory.</summary>
    public string FrameworkRootPath { get; set; } = "CodeLogic";
    /// <summary>Default culture used for localization.</summary>
    public string DefaultCulture { get; set; } = "en-US";
    /// <summary>List of cultures supported for localization.</summary>
    public IReadOnlyList<string> SupportedCultures { get; set; } = ["en-US"];
    /// <summary>Whether to resolve and validate library dependencies.</summary>
    public bool EnableDependencyResolution { get; set; } = true;
    /// <summary>Glob pattern used to discover library directories.</summary>
    public string LibraryDiscoveryPattern { get; set; } = "CL.*";

    /// <summary>Initializes a new library manager with the specified event bus.</summary>
    public LibraryManager(IEventBus eventBus)
    {
        _eventBus = eventBus;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    // ── Discovery ────────────────────────────────────────────────────────────

    /// <summary>Discovers library DLLs matching the discovery pattern in the Libraries directory.</summary>
    public List<string> Discover()
    {
        var librariesRoot = Path.Combine(FrameworkRootPath, "Libraries");
        var paths = new List<string>();

        if (!Directory.Exists(librariesRoot)) return paths;

        var dirs = Directory.GetDirectories(librariesRoot, LibraryDiscoveryPattern, SearchOption.TopDirectoryOnly);
        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            var dll  = Path.Combine(dir, $"{name}.dll");
            if (File.Exists(dll)) paths.Add(dll);
        }
        return paths;
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Manually registers a library by type.
    /// Dependency validation is deferred to ConfigureAllAsync which runs
    /// ValidateDependencies after all libraries are registered.
    /// </summary>
    public async Task<bool> LoadLibraryAsync<T>() where T : class, ILibrary, new()
    {
        await _lock.WaitAsync();
        try
        {
            var library  = new T();
            var manifest = library.Manifest;

            if (_librariesById.ContainsKey(manifest.Id))
            {
                Console.WriteLine($"  ⚠ Library '{manifest.Name}' already loaded");
                return false;
            }

            _libraries.Add(new LoadedLibrary
            {
                Instance     = library,
                Manifest     = manifest,
                AssemblyPath = typeof(T).Assembly.Location
            });
            _librariesById[manifest.Id] = library;

            Console.WriteLine($"  ✓ Manually loaded: {manifest.Name} v{manifest.Version}");
            return true;
        }
        finally { _lock.Release(); }
    }

    /// <summary>Loads libraries from the specified assembly paths.</summary>
    public async Task LoadLibrariesAsync(IReadOnlyList<string> libraryPaths)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var path in libraryPaths)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(path);
                    var type     = FindLibraryType(assembly);
                    if (type == null)
                    {
                        Console.WriteLine($"  ⚠ No ILibrary found in {Path.GetFileName(path)}");
                        continue;
                    }

                    var library  = (ILibrary)Activator.CreateInstance(type)!;
                    var manifest = library.Manifest;

                    if (_librariesById.ContainsKey(manifest.Id))
                        throw new InvalidOperationException($"Duplicate library ID '{manifest.Id}'");

                    _libraries.Add(new LoadedLibrary
                    {
                        Instance = library, Manifest = manifest, AssemblyPath = path
                    });
                    _librariesById[manifest.Id] = library;
                    Console.WriteLine($"  ✓ Discovered: {manifest.Name} v{manifest.Version}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to load {Path.GetFileName(path)}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
        finally { _lock.Release(); }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures all loaded libraries in dependency order, optionally generating missing config files.
    /// </summary>
    /// <param name="generateMissingConfigs">When true, generates default config files for libraries that don't have one.</param>
    /// <param name="forceGenerateConfigs">When true, overwrites existing config files with defaults.</param>
    /// <param name="configScopeToIds">When set, limits config generation to the specified library IDs.</param>
    /// <param name="dryRun">When true, validates configuration without applying changes.</param>
    public async Task ConfigureAllAsync(
        bool generateMissingConfigs = true,
        bool forceGenerateConfigs = false,
        IReadOnlyCollection<string>? configScopeToIds = null,
        bool dryRun = false)
    {
        await _lock.WaitAsync();
        try
        {
            ValidateDependencies(GetOrderedLibraries());

            foreach (var loaded in GetOrderedLibraries())
            {
                if (loaded.State != LibraryState.Loaded) continue;

                try
                {
                    var ctx = CreateContext(loaded.Manifest.Id);
                    await loaded.Instance.OnConfigureAsync(ctx);

                    var shouldApplyConfigGeneration = configScopeToIds == null
                        || configScopeToIds.Count == 0
                        || configScopeToIds.Contains(loaded.Manifest.Id, StringComparer.OrdinalIgnoreCase);
                    var allowMissingConfigs = shouldApplyConfigGeneration && (generateMissingConfigs || forceGenerateConfigs);
                    var configuration = GetConfigurationManager(ctx);

                    if (dryRun)
                    {
                        if (shouldApplyConfigGeneration)
                            ReportDryRunConfigActions(configuration, forceGenerateConfigs);

                        if (!(forceGenerateConfigs && shouldApplyConfigGeneration))
                            await configuration.ValidateAllAsync(allowMissingFiles: allowMissingConfigs);

                        await ctx.Localization.LoadAllAsync(SupportedCultures, generateIfMissing: false);
                    }
                    else
                    {
                        if (forceGenerateConfigs && shouldApplyConfigGeneration)
                            await ctx.Configuration.GenerateAllDefaultsAsync(force: true);

                        await ctx.Configuration.LoadAllAsync(generateIfMissing: allowMissingConfigs);
                        await ctx.Localization.GenerateAllTemplatesAsync(SupportedCultures);
                        await ctx.Localization.LoadAllAsync(SupportedCultures);
                    }

                    loaded.Context = ctx;
                    loaded.State   = LibraryState.Configured;
                    Console.WriteLine($"  ✓ Configured: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    loaded.State            = LibraryState.Failed;
                    loaded.FailureException = ex;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to configure {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Initializes all configured libraries in dependency order.
    /// </summary>
    public async Task InitializeAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var loaded in GetOrderedLibraries())
            {
                if (loaded.State == LibraryState.Failed) continue;
                if (loaded.State != LibraryState.Configured)
                    throw new InvalidOperationException(
                        $"Library '{loaded.Manifest.Name}' must be Configured before Initialize. State: {loaded.State}");

                try
                {
                    await loaded.Instance.OnInitializeAsync(loaded.Context!);
                    loaded.State = LibraryState.Initialized;
                    Console.WriteLine($"  ✓ Initialized: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    loaded.State = LibraryState.Failed;
                    loaded.FailureException = ex;
                    _eventBus.Publish(new LibraryFailedEvent(loaded.Manifest.Id, loaded.Manifest.Name, ex));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to initialize {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Starts all initialized libraries in dependency order.
    /// </summary>
    public async Task StartAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var loaded in GetOrderedLibraries())
            {
                if (loaded.State == LibraryState.Failed) continue;
                if (loaded.State != LibraryState.Initialized)
                    throw new InvalidOperationException(
                        $"Library '{loaded.Manifest.Name}' must be Initialized before Start. State: {loaded.State}");

                try
                {
                    await loaded.Instance.OnStartAsync(loaded.Context!);
                    loaded.State = LibraryState.Started;
                    _eventBus.Publish(new LibraryStartedEvent(loaded.Manifest.Id, loaded.Manifest.Name));
                    Console.WriteLine($"  ✓ Started: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    loaded.State = LibraryState.Failed;
                    loaded.FailureException = ex;
                    _eventBus.Publish(new LibraryFailedEvent(loaded.Manifest.Id, loaded.Manifest.Name, ex));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to start {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    throw;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// Stops all started libraries in reverse dependency order.
    /// </summary>
    public async Task StopAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var loaded in GetOrderedLibraries(reverse: true))
            {
                if (loaded.State != LibraryState.Started) continue;
                try
                {
                    await loaded.Instance.OnStopAsync();
                    loaded.State = LibraryState.Stopped;
                    _eventBus.Publish(new LibraryStoppedEvent(loaded.Manifest.Id, loaded.Manifest.Name));
                    Console.WriteLine($"  ✓ Stopped: {loaded.Manifest.Name}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Error stopping {loaded.Manifest.Name}: {ex.Message}");
                    Console.ResetColor();
                    // Don't rethrow on stop — try to stop everything
                }
            }
        }
        finally { _lock.Release(); }
    }

    // ── Health checks ────────────────────────────────────────────────────────

    /// <summary>
    /// Runs health checks on all started libraries and returns their status.
    /// </summary>
    public async Task<Dictionary<string, HealthStatus>> GetHealthAsync()
    {
        var results = new Dictionary<string, HealthStatus>();
        foreach (var loaded in _libraries.Where(l => l.State == LibraryState.Started))
        {
            try
            {
                results[loaded.Manifest.Id] = await loaded.Instance.HealthCheckAsync();
            }
            catch (Exception ex)
            {
                results[loaded.Manifest.Id] = HealthStatus.FromException(ex);
            }
        }
        return results;
    }

    /// <summary>
    /// Force-regenerates config defaults for all configured libraries, overwriting existing files.
    /// Only runs for libraries that have already completed their Configure phase (Context is set).
    /// Pass an empty array to include all libraries; pass specific IDs to scope generation.
    /// </summary>
    public async Task ForceRegenerateAllConfigsAsync(string[] scopeToIds)
    {
        foreach (var loaded in _libraries.Where(l => l.Context != null))
        {
            if (scopeToIds.Length > 0 &&
                !scopeToIds.Any(id => string.Equals(id, loaded.Manifest.Id, StringComparison.OrdinalIgnoreCase)))
                continue;

            await loaded.Context!.Configuration.GenerateAllDefaultsAsync(force: true);
        }
    }

    // ── Accessors ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets a loaded library instance by type.
    /// </summary>
    public T? GetLibrary<T>() where T : class, ILibrary =>
        _libraries.Select(l => l.Instance as T).FirstOrDefault(l => l != null);

    /// <summary>
    /// Gets a loaded library instance by its manifest ID.
    /// </summary>
    public ILibrary? GetLibrary(string id) =>
        _librariesById.GetValueOrDefault(id);

    /// <summary>
    /// Returns all registered library instances.
    /// </summary>
    public IEnumerable<ILibrary> GetAllLibraries() =>
        _libraries.Select(l => l.Instance);

    /// <summary>
    /// Returns all loaded library records including state and context.
    /// </summary>
    public IEnumerable<LoadedLibrary> GetLoadedLibraries() => _libraries.AsReadOnly();

    // ── Config discovery (3.4.1+) ────────────────────────────────────────────

    /// <summary>
    /// Enumerate every registered configuration section across every loaded
    /// library. Used by admin UIs and CLI tools to list editable settings.
    /// </summary>
    public IReadOnlyList<ConfigSectionOverview> GetAllConfigSections()
    {
        var result = new List<ConfigSectionOverview>();
        foreach (var lib in _libraries)
        {
            if (lib.Context is null) continue; // Pre-configure phase
            IReadOnlyList<ConfigSectionInfo> sections;
            try { sections = lib.Context.Configuration.GetSections(); }
            catch { continue; }

            foreach (var s in sections)
            {
                result.Add(new ConfigSectionOverview
                {
                    LibraryId   = lib.Manifest.Id,
                    LibraryName = lib.Manifest.Name,
                    SectionName = s.SectionName,
                    Title       = s.Title,
                    Description = s.Description,
                    FilePath    = s.FilePath,
                    FileExists  = s.FileExists,
                    IsLoaded    = s.IsLoaded
                });
            }
        }
        return result;
    }

    /// <summary>
    /// Build the <see cref="ConfigSchema"/> for a specific library's section.
    /// Returns null if the library or section isn't registered.
    /// </summary>
    /// <param name="libraryId">The <see cref="LibraryManifest.Id"/>.</param>
    /// <param name="sectionName">The section sub-name (empty string for the root <c>config.json</c>).</param>
    /// <param name="includeSecrets">When true, return raw values; default masks fields marked <see cref="ConfigFieldAttribute.Secret"/>.</param>
    public ConfigSchema? GetConfigSchema(string libraryId, string sectionName, bool includeSecrets = false)
    {
        var lib = _libraries.FirstOrDefault(l =>
            string.Equals(l.Manifest.Id, libraryId, StringComparison.OrdinalIgnoreCase));
        if (lib?.Context is null) return null;
        try { return lib.Context.Configuration.GetSchema(sectionName, includeSecrets); }
        catch (InvalidOperationException) { return null; }
    }

    /// <summary>
    /// Update a library's config section from a raw JSON payload.
    /// Returns the validation result. File is untouched when validation fails.
    /// </summary>
    public async Task<ConfigValidationResult> UpdateConfigAsync(
        string libraryId,
        string sectionName,
        string json,
        string? changedBy = null,
        CancellationToken ct = default)
    {
        var lib = _libraries.FirstOrDefault(l =>
            string.Equals(l.Manifest.Id, libraryId, StringComparison.OrdinalIgnoreCase));
        if (lib?.Context is null)
            return ConfigValidationResult.Invalid($"Library '{libraryId}' is not loaded.");

        return await lib.Context.Configuration
            .UpdateSectionAsync(sectionName, json, changedBy, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Reset a library's config section to defaults.</summary>
    public async Task ResetConfigAsync(
        string libraryId,
        string sectionName,
        string? changedBy = null,
        CancellationToken ct = default)
    {
        var lib = _libraries.FirstOrDefault(l =>
            string.Equals(l.Manifest.Id, libraryId, StringComparison.OrdinalIgnoreCase));
        if (lib?.Context is null)
            throw new InvalidOperationException($"Library '{libraryId}' is not loaded.");

        await lib.Context.Configuration
            .ResetSectionAsync(sectionName, changedBy, ct)
            .ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private LibraryContext CreateContext(string libraryId)
    {
        var root    = Path.Combine(FrameworkRootPath, "Libraries");
        var libDir  = Path.Combine(root, NormalizeId(libraryId));
        var logsDir = Path.Combine(libDir, "logs");
        var dataDir = Path.Combine(libDir, "data");
        var locDir  = Path.Combine(libDir, "localization");

        Directory.CreateDirectory(libDir);
        Directory.CreateDirectory(logsDir);
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(locDir);

        var logger = new Logger(
            libraryId.ToUpper(), logsDir, LoggingOptions.GlobalLevel, LoggingOptions);

        return new LibraryContext
        {
            LibraryId             = libraryId,
            LibraryDirectory      = libDir,
            ConfigDirectory       = libDir,
            LocalizationDirectory = locDir,
            LogsDirectory         = logsDir,
            DataDirectory         = dataDir,
            Logger                = logger,
            Configuration         = new ConfigurationManager(libDir),
            Localization          = new LocalizationManager(locDir, DefaultCulture),
            Events                = _eventBus
        };
    }

    private static ConfigurationManager GetConfigurationManager(LibraryContext context) =>
        context.Configuration as ConfigurationManager
        ?? throw new InvalidOperationException(
            "Library configuration manager must be CodeLogic.Core.Configuration.ConfigurationManager.");

    private static void ReportDryRunConfigActions(ConfigurationManager configuration, bool force)
    {
        foreach (var path in configuration.GetRegisteredFilePaths())
        {
            if (force)
            {
                var action = File.Exists(path) ? "overwrite" : "generate";
                Console.WriteLine($"[dry-run] Would {action}: {GetDisplayPath(path)}");
            }
            else if (!File.Exists(path))
            {
                Console.WriteLine($"[dry-run] Would generate: {GetDisplayPath(path)}");
            }
        }
    }

    private static string GetDisplayPath(string path) =>
        Path.GetRelativePath(AppContext.BaseDirectory, path);

    private List<LoadedLibrary> GetOrderedLibraries(bool reverse = false)
    {
        var ordered = EnableDependencyResolution
            ? TopologicalSort()
            : _libraries.ToList();
        if (reverse) ordered.Reverse();
        return ordered;
    }

    private List<LoadedLibrary> TopologicalSort()
    {
        var sorted   = new List<LoadedLibrary>();
        var visited  = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var lib in _libraries)
            Visit(lib, sorted, visited, visiting);

        return sorted;
    }

    private void Visit(LoadedLibrary lib, List<LoadedLibrary> sorted,
        HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(lib.Manifest.Id)) return;
        if (visiting.Contains(lib.Manifest.Id))
            throw new InvalidOperationException($"Circular dependency: {lib.Manifest.Id}");

        visiting.Add(lib.Manifest.Id);

        foreach (var dep in lib.Manifest.Dependencies)
        {
            var depLib = _libraries.FirstOrDefault(l => l.Manifest.Id == dep.Id);
            if (depLib == null && dep.IsOptional) continue;
            if (depLib != null) Visit(depLib, sorted, visited, visiting);
        }

        visiting.Remove(lib.Manifest.Id);
        visited.Add(lib.Manifest.Id);
        sorted.Add(lib);
    }

    private void ValidateDependencies(IEnumerable<LoadedLibrary> ordered)
    {
        var errors = new List<string>();
        foreach (var lib in ordered)
        {
            foreach (var dep in lib.Manifest.Dependencies)
            {
                var found = _libraries.FirstOrDefault(l => l.Manifest.Id == dep.Id);
                if (found == null)
                {
                    if (!dep.IsOptional)
                        errors.Add($"'{lib.Manifest.Name}' requires missing dependency '{dep.Id}'" +
                            (dep.MinVersion != null ? $" >= {dep.MinVersion}" : ""));
                    continue;
                }

                if (dep.MinVersion != null &&
                    SemanticVersion.TryParse(found.Manifest.Version, out var installed) &&
                    SemanticVersion.TryParse(dep.MinVersion, out var required) &&
                    installed != null && required != null &&
                    installed < required)
                {
                    errors.Add($"'{lib.Manifest.Name}' requires '{dep.Id}' >= {dep.MinVersion}, " +
                        $"but {installed} is loaded");
                }
            }
        }
        if (errors.Count > 0)
            throw new InvalidOperationException("Dependency validation failed:\n  " + string.Join("\n  ", errors));
    }

    private static string NormalizeId(string id) =>
        id.StartsWith("CL.", StringComparison.OrdinalIgnoreCase) ? $"CL.{id[3..]}" : $"CL.{id}";

    private static Type? FindLibraryType(Assembly assembly) =>
        assembly.GetTypes().FirstOrDefault(t =>
            typeof(ILibrary).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        foreach (var lib in _libraries)
        {
            var dir  = Path.GetDirectoryName(lib.AssemblyPath);
            if (dir == null) continue;
            var path = Path.Combine(dir, name.Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
    }
}
