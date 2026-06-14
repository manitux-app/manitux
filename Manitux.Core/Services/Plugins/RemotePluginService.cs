using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Manitux.Core.Services.Plugins;

public sealed class RemotePluginService : IRemotePluginService, IDisposable
{
    private const string SettingsFileName = "plugins.json";
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public RemotePluginService(string? pluginsRootPath = null, HttpClient? httpClient = null)
    {
        PluginsRootPath = pluginsRootPath ?? GetDefaultPluginsRootPath();
        SettingsPath = Path.Combine(PluginsRootPath, SettingsFileName);
        DownloadsPath = PluginsRootPath; //Path.Combine(PluginsRootPath, "remote");
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true
        });
        _disposeHttpClient = httpClient is null;
    }

    public string PluginsRootPath { get; }
    public string SettingsPath { get; }
    public string DownloadsPath { get; }

    public async Task<ManagedRemoteRepository> AddRepositoryAsync(string urlOrShortCode, CancellationToken cancellationToken = default)
    {
        var repositoryUrl = await ResolveInputUrlAsync(urlOrShortCode, cancellationToken);
        var (resolvedRepositoryUrl, repository) = await FetchRepositoryWithFallbackAsync(repositoryUrl, cancellationToken);
        repositoryUrl = resolvedRepositoryUrl;
        var settings = await LoadSettingsAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var existing = settings.Repositories.FirstOrDefault(x => UrlEquals(x.Url, repositoryUrl));
        if (existing is null)
        {
            existing = new ManagedRemoteRepository
            {
                AddedAt = now
            };
            settings.Repositories.Add(existing);
        }

        existing.Name = repository.Name;
        existing.Description = repository.Description;
        existing.IconUrl = string.IsNullOrWhiteSpace(repository.IconUrl)
            ? null
            : FixUrl(repository.IconUrl, repositoryUrl);
        existing.ManifestVersion = repository.ManifestVersion;
        existing.PluginLists = await FetchManagedPluginListsAsync(repository.PluginLists, repositoryUrl, now, cancellationToken);
        existing.Url = repositoryUrl;
        existing.UpdatedAt = now;

        await SaveSettingsAsync(settings, cancellationToken);
        return existing;
    }

    public async Task<IReadOnlyList<RemotePluginManifest>> GetRepositoryPluginsAsync(string repositoryUrlOrShortCode, CancellationToken cancellationToken = default)
    {
        var repository = await AddRepositoryAsync(repositoryUrlOrShortCode, cancellationToken);
        var plugins = new List<RemotePluginManifest>();

        foreach (var pluginList in repository.PluginLists)
        {
            var pluginListUrl = pluginList.Url;
            var list = FlattenPluginList(pluginList.Packages);
            foreach (var plugin in list)
            {
                plugin.Url = FixUrl(plugin.Url, pluginListUrl);
                plugin.RepositoryUrl ??= repository.Url;
                plugin.PluginListUrl = pluginListUrl;
                plugins.Add(plugin);
            }
        }

        return plugins
            .OrderBy(x => GetPackageKey(x))
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task<bool> RemoveRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var repository = settings.Repositories.FirstOrDefault(x => UrlEquals(x.Url, repositoryUrl));
        if (repository is null)
        {
            return false;
        }

        string? repoUrl = GetGitHubRepoUrl(repositoryUrl);
        if(repoUrl is null) return false;
        //Debug.WriteLine("repo url: " + repoUrl);

        var plugins = settings.InstalledPlugins.Where(x => UrlEquals(x.RepositoryUrl ?? "", repoUrl)).ToList();

        foreach (var plugin in plugins)
        {
            //Debug.WriteLine("deleted plugin: " + plugin.InternalName);

            if (plugin is null)
            {
                continue;
            }

            var canDeleteFile = !IsPluginFileUsed(settings, plugin.FilePath, plugin.InternalName);
            if (canDeleteFile)
            {
                await DeleteFileWithRetryAsync(plugin.FilePath, cancellationToken);
            }

            settings.InstalledPlugins.Remove(plugin);
            DeletePluginDirectoriesIfUnused(settings, plugin.FilePath);
        }

        settings.Repositories.Remove(repository);

        await SaveSettingsAsync(settings, cancellationToken);
        return true;
    }

    public async Task<RemotePluginInstallResult> InstallAsync(
        RemotePluginManifest manifest,
        CancellationToken cancellationToken = default)
    {
        manifest.Url = FixUrl(manifest.Url, manifest.PluginListUrl ?? manifest.RepositoryUrl ?? manifest.Url);
        var plugin = await DownloadPluginAsync(manifest, manifest.RepositoryUrl, manifest.PluginListUrl, cancellationToken);
        return new RemotePluginInstallResult { Success = true, Message = "Plugin installed.", Plugin = plugin };
    }

    public async Task<RemotePluginInstallResult> InstallAsync(string urlOrShortCode, string? internalName = null, CancellationToken cancellationToken = default)
    {
        var resolvedUrl = await ResolveInputUrlAsync(urlOrShortCode, cancellationToken);

        if (IsPluginFileUrl(resolvedUrl))
        {
            var manifest = CreateDirectManifest(resolvedUrl, internalName);
            var plugin = await DownloadPluginAsync(manifest, null, null, cancellationToken);
            return new RemotePluginInstallResult { Success = true, Message = "Plugin installed.", Plugin = plugin };
        }

        if (resolvedUrl.EndsWith("plugins.json", StringComparison.OrdinalIgnoreCase))
        {
            var plugins = await FetchPluginListAsync(resolvedUrl, cancellationToken);
            var manifest = PickPlugin(plugins, internalName);
            if (manifest is null)
            {
                return new RemotePluginInstallResult { Success = false, Message = "Plugin not found in plugin list." };
            }

            manifest.Url = FixUrl(manifest.Url, resolvedUrl);
            var plugin = await DownloadPluginAsync(manifest, null, resolvedUrl, cancellationToken);
            return new RemotePluginInstallResult { Success = true, Message = "Plugin installed.", Plugin = plugin };
        }

        var repository = await AddRepositoryAsync(resolvedUrl, cancellationToken);
        foreach (var pluginList in repository.PluginLists)
        {
            var pluginListUrl = pluginList.Url;
            var plugins = await FetchPluginListAsync(pluginListUrl, cancellationToken);
            var manifest = PickPlugin(plugins, internalName);
            if (manifest is null)
            {
                continue;
            }

            manifest.Url = FixUrl(manifest.Url, pluginListUrl);
            manifest.RepositoryUrl ??= repository.Url;
            var plugin = await DownloadPluginAsync(manifest, repository.Url, pluginListUrl, cancellationToken);
            return new RemotePluginInstallResult { Success = true, Message = "Plugin installed.", Plugin = plugin };
        }

        return new RemotePluginInstallResult { Success = false, Message = "Plugin not found in repository." };
    }

    public async Task<bool> RemoveAsync(string internalName, CancellationToken cancellationToken = default)
    {
        return await RemoveAsync(internalName, null, cancellationToken);
    }

    public async Task<int> SetEnabledStatesAsync(
        IReadOnlyCollection<RemotePluginEnabledState> states,
        CancellationToken cancellationToken = default)
    {
        if (states.Count == 0)
        {
            return 0;
        }

        var settings = await LoadSettingsAsync(cancellationToken);
        var changed = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var state in states)
        {
            var plugin = settings.InstalledPlugins.FirstOrDefault(x =>
                KeyEquals(x.InternalName, state.InternalName)
                && (string.IsNullOrWhiteSpace(state.PackageInternalName)
                    || KeyEquals(x.PackageInternalName, state.PackageInternalName)));

            if (plugin is null || plugin.IsEnabled == state.IsEnabled)
            {
                continue;
            }

            plugin.IsEnabled = state.IsEnabled;
            plugin.UpdatedAt = now;
            changed++;
        }

        if (changed > 0)
        {
            await SaveSettingsAsync(settings, cancellationToken);
        }

        return changed;
    }

    public async Task<bool> RemoveAsync(string internalName, string? packageInternalName, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var plugin = settings.InstalledPlugins.FirstOrDefault(x =>
            KeyEquals(x.InternalName, internalName)
            && (string.IsNullOrWhiteSpace(packageInternalName) || KeyEquals(x.PackageInternalName, packageInternalName)));
        if (plugin is null)
        {
            return false;
        }

        var canDeleteFile = !IsPluginFileUsed(settings, plugin.FilePath, plugin.InternalName);
        if (canDeleteFile)
        {
            await DeleteFileWithRetryAsync(plugin.FilePath, cancellationToken);
        }

        settings.InstalledPlugins.Remove(plugin);
        await SaveSettingsAsync(settings, cancellationToken);

        DeletePluginDirectoriesIfUnused(settings, plugin.FilePath);

        return true;
    }

    public async Task<RemotePluginInstallResult> UpdateAsync(string internalName, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(internalName, null, cancellationToken);
    }

    public async Task<RemotePluginUpdateCheckResult> CheckUpdateAsync(string internalName, CancellationToken cancellationToken = default)
    {
        return await CheckUpdateAsync(internalName, null, cancellationToken);
    }

    public async Task<RemotePluginUpdateCheckResult> CheckUpdateAsync(
        string internalName,
        string? packageInternalName,
        CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var installed = settings.InstalledPlugins.FirstOrDefault(x =>
            KeyEquals(x.InternalName, internalName)
            && (string.IsNullOrWhiteSpace(packageInternalName) || KeyEquals(x.PackageInternalName, packageInternalName)));
        if (installed is null)
        {
            return new RemotePluginUpdateCheckResult
            {
                Message = "Plugin is not installed."
            };
        }

        var manifest = await FindLatestManifestAsync(installed, cancellationToken);
        if (manifest is null)
        {
            return new RemotePluginUpdateCheckResult
            {
                InstalledPlugin = installed,
                Message = "Plugin manifest could not be refreshed."
            };
        }

        var hasUpdate = manifest.Version > installed.Version;
        return new RemotePluginUpdateCheckResult
        {
            InstalledPlugin = installed,
            LatestManifest = manifest,
            HasUpdate = hasUpdate,
            Message = hasUpdate ? "Plugin update is available." : "Plugin is already up to date."
        };
    }

    public async Task<IReadOnlyList<RemotePluginUpdateCheckResult>> CheckUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var plugins = settings.InstalledPlugins
            .Select(x => new { x.InternalName, x.PackageInternalName })
            .ToList();
        var results = new List<RemotePluginUpdateCheckResult>();

        foreach (var plugin in plugins)
        {
            results.Add(await CheckUpdateAsync(plugin.InternalName, plugin.PackageInternalName, cancellationToken));
        }

        return results;
    }

    public async Task<RemotePluginInstallResult> UpdateAsync(string internalName, string? packageInternalName, CancellationToken cancellationToken = default)
    {
        var check = await CheckUpdateAsync(internalName, packageInternalName, cancellationToken);
        if (check.InstalledPlugin is null || string.IsNullOrWhiteSpace(check.InstalledPlugin.InternalName))
        {
            return new RemotePluginInstallResult { Success = false, Message = check.Message };
        }

        if (check.LatestManifest is null)
        {
            return new RemotePluginInstallResult { Success = false, Message = check.Message, Plugin = check.InstalledPlugin };
        }

        if (!check.HasUpdate)
        {
            return new RemotePluginInstallResult { Success = true, Message = check.Message, Plugin = check.InstalledPlugin };
        }

        var updated = await DownloadPluginAsync(
            check.LatestManifest,
            check.InstalledPlugin.RepositoryUrl,
            check.InstalledPlugin.PluginListUrl,
            cancellationToken);
        return new RemotePluginInstallResult { Success = true, Message = "Plugin updated.", Plugin = updated };
    }

    public async Task<IReadOnlyList<RemotePluginInstallResult>> UpdateAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var plugins = settings.InstalledPlugins
            .Select(x => new { x.InternalName, x.PackageInternalName })
            .ToList();
        var results = new List<RemotePluginInstallResult>();

        foreach (var plugin in plugins)
        {
            results.Add(await UpdateAsync(plugin.InternalName, plugin.PackageInternalName, cancellationToken));
        }

        return results;
    }

    public Task<RemotePluginSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return LoadSettingsAsync(cancellationToken);
    }

    private async Task<ManagedRemotePlugin> DownloadPluginAsync(
        RemotePluginManifest manifest,
        string? repositoryUrl,
        string? pluginListUrl,
        CancellationToken cancellationToken)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var key = GetPluginKey(manifest);
        var packageKey = GetPackageKey(manifest);
        var fileName = GetSafeFileName(Path.GetFileName(new Uri(manifest.Url).LocalPath));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"{packageKey}.dll";
        }

        var pluginDirectory = Path.Combine(
            DownloadsPath,
            GetSafeFileName(packageKey),
            GetVersionDirectoryName(manifest.Version));
        Directory.CreateDirectory(pluginDirectory);

        var filePath = Path.Combine(pluginDirectory, fileName);

        var alreadyDownloaded = File.Exists(filePath)
                                && settings.InstalledPlugins.Any(x =>
                                    string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(x.SourceUrl, manifest.Url, StringComparison.OrdinalIgnoreCase)
                                    && x.Version >= manifest.Version);

        if (!alreadyDownloaded)
        {
            var tempFilePath = $"{filePath}.{Guid.NewGuid():N}.download";
            try
            {
                await using (var remote = await _httpClient.GetStreamAsync(manifest.Url, cancellationToken))
                await using (var local = File.Create(tempFilePath))
                {
                    await remote.CopyToAsync(local, cancellationToken);
                }

                await ReplaceFileWithRetryAsync(tempFilePath, filePath, cancellationToken);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        var now = DateTimeOffset.UtcNow;
        var existing = settings.InstalledPlugins.FirstOrDefault(x =>
            KeyEquals(x.InternalName, key)
            && KeyEquals(x.PackageInternalName, packageKey));
        if (existing is null)
        {
            existing = new ManagedRemotePlugin
            {
                InstalledAt = now
            };
            settings.InstalledPlugins.Add(existing);
        }
        else if (!string.Equals(existing.FilePath, filePath, StringComparison.OrdinalIgnoreCase)
                 && File.Exists(existing.FilePath))
        {
            DeletePluginFileIfUnused(settings, existing.FilePath, existing.InternalName);
        }

        existing.Name = manifest.Name;
        existing.InternalName = key;
        existing.Version = manifest.Version;
        existing.ApiVersion = manifest.ApiVersion;
        existing.SourceUrl = manifest.Url;
        existing.PackageInternalName = packageKey;
        existing.PackageName = manifest.PackageName;
        existing.RepositoryUrl = repositoryUrl ?? manifest.RepositoryUrl;
        existing.PluginListUrl = pluginListUrl;
        existing.FilePath = filePath;
        existing.UpdatedAt = now;
        existing.Manifest = manifest;

        await SaveSettingsAsync(settings, cancellationToken);
        return existing;
    }

    private async Task<RemotePluginManifest?> FindLatestManifestAsync(ManagedRemotePlugin installed, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(installed.PluginListUrl))
        {
            var list = await FetchPluginListAsync(installed.PluginListUrl, cancellationToken);
            var manifest = list.FirstOrDefault(x =>
                               KeyEquals(GetPluginKey(x), installed.InternalName)
                               && KeyEquals(GetPackageKey(x), installed.PackageInternalName))
                           ?? list.FirstOrDefault(x => KeyEquals(GetPluginKey(x), installed.InternalName));
            if (manifest is not null)
            {
                manifest.Url = FixUrl(manifest.Url, installed.PluginListUrl);
                manifest.PluginListUrl = installed.PluginListUrl;
                manifest.RepositoryUrl ??= installed.RepositoryUrl;
                return manifest;
            }
        }

        if (!string.IsNullOrWhiteSpace(installed.RepositoryUrl))
        {
            var plugins = await GetRepositoryPluginsAsync(installed.RepositoryUrl, cancellationToken);
            return plugins.FirstOrDefault(x =>
                       KeyEquals(GetPluginKey(x), installed.InternalName)
                       && KeyEquals(GetPackageKey(x), installed.PackageInternalName))
                   ?? plugins.FirstOrDefault(x => KeyEquals(GetPluginKey(x), installed.InternalName));
        }

        return installed.Manifest;
    }

    private async Task<RemotePluginRepository> FetchRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken)
    {
        var repository = await GetJsonAsync<RemotePluginRepository>(repositoryUrl, cancellationToken);
        if (repository is null || repository.PluginLists.Count == 0)
        {
            throw new InvalidOperationException("Remote repository manifest is invalid or has no plugin lists.");
        }

        return repository;
    }

    private async Task<(string Url, RemotePluginRepository Repository)> FetchRepositoryWithFallbackAsync(
        string repositoryUrl,
        CancellationToken cancellationToken)
    {
        var candidates = new[] { repositoryUrl }
            .Concat(GetRepositoryFallbackUrls(repositoryUrl))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Exception? lastError = null;
        foreach (var candidate in candidates)
        {
            try
            {
                return (candidate, await FetchRepositoryAsync(candidate, cancellationToken));
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException("Remote repository manifest could not be loaded.", lastError);
    }

    private async Task<List<RemotePluginManifest>> FetchPluginListAsync(string pluginListUrl, CancellationToken cancellationToken)
    {
        return FlattenPluginList(await FetchPluginPackagesAsync(pluginListUrl, cancellationToken));
    }

    private async Task<List<RemotePluginManifest>> FetchPluginPackagesAsync(string pluginListUrl, CancellationToken cancellationToken)
    {
        return await GetJsonAsync<List<RemotePluginManifest>>(pluginListUrl, cancellationToken) ?? [];
    }

    private async Task<List<ManagedRemotePluginList>> FetchManagedPluginListsAsync(
        IEnumerable<string> pluginListUrls,
        string repositoryUrl,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var lists = new List<ManagedRemotePluginList>();

        foreach (var pluginListUrl in pluginListUrls.Select(x => FixUrl(x, repositoryUrl)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            lists.Add(new ManagedRemotePluginList
            {
                Url = pluginListUrl,
                Packages = await FetchPluginPackagesAsync(pluginListUrl, cancellationToken),
                UpdatedAt = updatedAt
            });
        }

        return lists;
    }

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
    }

    private async Task<string> ResolveInputUrlAsync(string urlOrShortCode, CancellationToken cancellationToken)
    {
        var input = urlOrShortCode.Trim();
        if (input.StartsWith("manituxrepo://", StringComparison.OrdinalIgnoreCase))
        {
            input = "https://" + input["manituxrepo://".Length..].TrimStart('/');
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return NormalizeGitHubUrl(uri.ToString());
        }

        var shortCodeUrl = $"https://tinyurl.com/{Uri.EscapeDataString(input)}";
        using var response = await _httpClient.GetAsync(shortCodeUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resolved = response.RequestMessage?.RequestUri?.ToString();
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException("Short code could not be resolved.");
        }

        return NormalizeGitHubUrl(resolved);
    }

    private static RemotePluginManifest? PickPlugin(IEnumerable<RemotePluginManifest> plugins, string? internalName)
    {
        if (string.IsNullOrWhiteSpace(internalName))
        {
            return plugins.OrderBy(x => x.Name).FirstOrDefault();
        }

        return plugins.FirstOrDefault(x => KeyEquals(x.InternalName, internalName)
                                           || string.Equals(x.Name, internalName, StringComparison.OrdinalIgnoreCase));
    }

    private static RemotePluginManifest CreateDirectManifest(string url, string? internalName)
    {
        var fileName = Path.GetFileName(new Uri(url).LocalPath);
        var name = string.IsNullOrWhiteSpace(internalName)
            ? Path.GetFileNameWithoutExtension(fileName)
            : internalName;

        return new RemotePluginManifest
        {
            Name = name ?? "Remote Plugin",
            InternalName = name ?? "plugin.remote",
            Url = url,
            Version = 1,
            ApiVersion = 1,
            Status = 1
        };
    }

    private async Task<RemotePluginSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(PluginsRootPath);
        Directory.CreateDirectory(DownloadsPath);

        if (!File.Exists(SettingsPath))
        {
            return new RemotePluginSettings();
        }

        await using var stream = File.OpenRead(SettingsPath);
        return await JsonSerializer.DeserializeAsync<RemotePluginSettings>(stream, _jsonOptions, cancellationToken)
               ?? new RemotePluginSettings();
    }

    private async Task SaveSettingsAsync(RemotePluginSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(PluginsRootPath);
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, _jsonOptions, cancellationToken);
    }

    private static string GetDefaultPluginsRootPath()
    {
        var baseDir = OperatingSystem.IsAndroid()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : AppContext.BaseDirectory;

        return Path.Combine(baseDir, "data", "plugins");
    }

    private static string NormalizeGitHubUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 5 && parts[2].Equals("blob", StringComparison.OrdinalIgnoreCase))
        {
            var owner = parts[0];
            var repo = parts[1];
            var branch = parts[3];
            var path = string.Join('/', parts.Skip(4));
            return $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}";
        }

        if (parts.Length == 2)
        {
            return $"https://raw.githubusercontent.com/{parts[0]}/{parts[1]}/builds/repo.json";
        }

        return url;
    }

    private string? GetGitHubRepoUrl(string url)
    {
        try
        {
            Uri uri = new Uri(url);

            // if (uri.Host != "://githubusercontent.com")
            // {
            //     return null;
            // }

            string[] segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
            {
                return null;
            }

            string repoAddress = $"https://github.com/{segments[0]}/{segments[1]}";
            return repoAddress;
        }
        catch { return null; }
    }

    private static IEnumerable<string> GetRepositoryFallbackUrls(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4 || !parts[^1].Equals("repo.json", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        foreach (var branch in new[] { "builds", "master", "main" })
        {
            if (parts[2].Equals(branch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parts[2] = branch;
            yield return $"{uri.Scheme}://{uri.Host}/{string.Join('/', parts)}";
        }
    }

    private static string FixUrl(string value, string baseUrl)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return NormalizeGitHubUrl(value);
        }

        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            ? NormalizeGitHubUrl(new Uri(baseUri, value).ToString())
            : value;
    }

    private static bool IsPluginFileUrl(string url)
    {
        return url.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
               || url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPluginKey(RemotePluginManifest manifest)
    {
        return string.IsNullOrWhiteSpace(manifest.InternalName)
            ? GetSafeFileName(manifest.Name)
            : manifest.InternalName;
    }

    private static string GetPackageKey(RemotePluginManifest manifest)
    {
        if (!string.IsNullOrWhiteSpace(manifest.PackageInternalName))
        {
            return manifest.PackageInternalName;
        }

        var fileName = Uri.TryCreate(manifest.Url, UriKind.Absolute, out var uri)
            ? Path.GetFileNameWithoutExtension(uri.LocalPath)
            : Path.GetFileNameWithoutExtension(manifest.Url);

        return string.IsNullOrWhiteSpace(fileName)
            ? GetPluginKey(manifest)
            : fileName;
    }

    private static List<RemotePluginManifest> FlattenPluginList(IEnumerable<RemotePluginManifest> packages)
    {
        var plugins = new List<RemotePluginManifest>();

        foreach (var package in packages)
        {
            if (package.Plugins is null || package.Plugins.Count == 0)
            {
                package.PackageInternalName = string.IsNullOrWhiteSpace(package.InternalName)
                    ? GetPackageKey(package)
                    : package.InternalName;
                package.PackageName = package.Name;
                plugins.Add(package);
                continue;
            }

            var packageInternalName = string.IsNullOrWhiteSpace(package.InternalName)
                ? GetPackageKey(package)
                : package.InternalName;

            foreach (var plugin in package.Plugins)
            {
                plugin.Url = package.Url;
                plugin.Status = plugin.Status == 0 ? package.Status : plugin.Status;
                plugin.Version = plugin.Version == 0 ? package.Version : plugin.Version;
                plugin.ApiVersion = plugin.ApiVersion == 0 ? package.ApiVersion : plugin.ApiVersion;
                plugin.RepositoryUrl ??= package.RepositoryUrl;
                plugin.IconUrl ??= package.IconUrl;
                plugin.Authors = plugin.Authors.Count == 0 ? package.Authors : plugin.Authors;
                plugin.TvTypes ??= package.TvTypes;
                plugin.Language ??= package.Language;
                plugin.IsAdult ??= package.IsAdult;
                plugin.PackageInternalName = packageInternalName;
                plugin.PackageName = string.IsNullOrWhiteSpace(package.Name) ? packageInternalName : package.Name;
                plugin.Plugins = null;
                plugins.Add(plugin);
            }
        }

        return plugins;
    }

    private static void DeletePluginFileIfUnused(RemotePluginSettings settings, string? filePath, string? excludingInternalName = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        if (!IsPluginFileUsed(settings, filePath, excludingInternalName))
        {
            File.Delete(filePath);
        }
    }

    private static bool IsPluginFileUsed(
        RemotePluginSettings settings,
        string? filePath,
        string? excludingInternalName = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        return settings.InstalledPlugins.Any(x =>
            !KeyEquals(x.InternalName, excludingInternalName)
            && string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task DeleteFileWithRetryAsync(string? filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        const int attempts = 20;
        Exception? lastError = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(filePath);
                return;
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(150, cancellationToken);
        }

        throw new IOException($"Plugin file could not be deleted because it is still locked: {filePath}", lastError);
    }

    private static void DeletePluginDirectoriesIfUnused(RemotePluginSettings settings, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var pluginDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            return;
        }

        if (settings.InstalledPlugins.Any(x =>
                !string.IsNullOrWhiteSpace(x.FilePath)
                && IsSameOrChildPath(pluginDirectory, x.FilePath)))
        {
            return;
        }

        TryDeleteDirectoryIfEmpty(pluginDirectory);

        var parentDirectory = Directory.GetParent(pluginDirectory)?.FullName;
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return;
        }

        if (settings.InstalledPlugins.Any(x =>
                !string.IsNullOrWhiteSpace(x.FilePath)
                && IsSameOrChildPath(parentDirectory, x.FilePath)))
        {
            return;
        }

        TryDeleteDirectoryIfEmpty(parentDirectory);
    }

    private static void TryDeleteDirectoryIfEmpty(string directory)
    {
        if (!Directory.Exists(directory) || Directory.EnumerateFileSystemEntries(directory).Any())
        {
            return;
        }

        Directory.Delete(directory);
    }

    private static bool IsSameOrChildPath(string directory, string path)
    {
        var normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);

        return normalizedPath.StartsWith(
            normalizedDirectory,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static async Task ReplaceFileWithRetryAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken)
    {
        const int attempts = 20;
        Exception? lastError = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }

                File.Move(sourceFilePath, destinationFilePath);
                return;
            }
            catch (IOException ex)
            {
                lastError = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(150, cancellationToken);
        }

        throw new IOException($"Plugin file could not be replaced because it is still locked: {destinationFilePath}", lastError);
    }

    private static bool UrlEquals(string left, string right)
    {
        return string.Equals(left.TrimEnd('/'), right.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    private static bool KeyEquals(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSafeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "remote-plugin" : safe;
    }

    private static string GetVersionDirectoryName(int version)
    {
        return $"v{Math.Max(version, 1)}";
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
