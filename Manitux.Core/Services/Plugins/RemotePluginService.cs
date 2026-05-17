using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Manitux.Core.Services.Plugins;

public sealed class RemotePluginService : IRemotePluginService, IDisposable
{
    private const string SettingsFileName = "remote-plugins.json";
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
        DownloadsPath = Path.Combine(PluginsRootPath, "remote");
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
        existing.ManifestVersion = repository.ManifestVersion;
        existing.PluginLists = repository.PluginLists.Select(x => FixUrl(x, repositoryUrl)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        existing.Url = repositoryUrl;
        existing.UpdatedAt = now;

        await SaveSettingsAsync(settings, cancellationToken);
        return existing;
    }

    public async Task<IReadOnlyList<RemotePluginManifest>> GetRepositoryPluginsAsync(string repositoryUrlOrShortCode, CancellationToken cancellationToken = default)
    {
        var repository = await AddRepositoryAsync(repositoryUrlOrShortCode, cancellationToken);
        var plugins = new List<RemotePluginManifest>();

        foreach (var pluginListUrl in repository.PluginLists)
        {
            var list = await FetchPluginListAsync(pluginListUrl, cancellationToken);
            foreach (var plugin in list)
            {
                plugin.Url = FixUrl(plugin.Url, pluginListUrl);
                plugin.RepositoryUrl ??= repository.Url;
                plugins.Add(plugin);
            }
        }

        return plugins
            .GroupBy(x => GetPluginKey(x))
            .Select(x => x.OrderByDescending(p => p.Version).First())
            .OrderBy(x => x.Name)
            .ToList();
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
        foreach (var pluginListUrl in repository.PluginLists)
        {
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
        var settings = await LoadSettingsAsync(cancellationToken);
        var plugin = settings.InstalledPlugins.FirstOrDefault(x => KeyEquals(x.InternalName, internalName));
        if (plugin is null)
        {
            return false;
        }

        settings.InstalledPlugins.Remove(plugin);
        await SaveSettingsAsync(settings, cancellationToken);

        if (!string.IsNullOrWhiteSpace(plugin.FilePath) && File.Exists(plugin.FilePath))
        {
            File.Delete(plugin.FilePath);
        }

        var pluginDirectory = Path.GetDirectoryName(plugin.FilePath);
        if (!string.IsNullOrWhiteSpace(pluginDirectory)
            && Directory.Exists(pluginDirectory)
            && !Directory.EnumerateFileSystemEntries(pluginDirectory).Any())
        {
            Directory.Delete(pluginDirectory);
        }

        return true;
    }

    public async Task<RemotePluginInstallResult> UpdateAsync(string internalName, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var installed = settings.InstalledPlugins.FirstOrDefault(x => KeyEquals(x.InternalName, internalName));
        if (installed is null)
        {
            return new RemotePluginInstallResult { Success = false, Message = "Plugin is not installed." };
        }

        var manifest = await FindLatestManifestAsync(installed, cancellationToken);
        if (manifest is null)
        {
            return new RemotePluginInstallResult { Success = false, Message = "Plugin manifest could not be refreshed." };
        }

        if (manifest.Version <= installed.Version)
        {
            return new RemotePluginInstallResult { Success = true, Message = "Plugin is already up to date.", Plugin = installed };
        }

        var updated = await DownloadPluginAsync(manifest, installed.RepositoryUrl, installed.PluginListUrl, cancellationToken);
        return new RemotePluginInstallResult { Success = true, Message = "Plugin updated.", Plugin = updated };
    }

    public async Task<IReadOnlyList<RemotePluginInstallResult>> UpdateAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var names = settings.InstalledPlugins.Select(x => x.InternalName).ToList();
        var results = new List<RemotePluginInstallResult>();

        foreach (var name in names)
        {
            results.Add(await UpdateAsync(name, cancellationToken));
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
        var fileName = GetSafeFileName(Path.GetFileName(new Uri(manifest.Url).LocalPath));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"{key}.dll";
        }

        var pluginDirectory = Path.Combine(DownloadsPath, GetSafeFileName(key));
        Directory.CreateDirectory(pluginDirectory);

        var filePath = Path.Combine(pluginDirectory, fileName);
        await using (var remote = await _httpClient.GetStreamAsync(manifest.Url, cancellationToken))
        await using (var local = File.Create(filePath))
        {
            await remote.CopyToAsync(local, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var existing = settings.InstalledPlugins.FirstOrDefault(x => KeyEquals(x.InternalName, key));
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
            File.Delete(existing.FilePath);
        }

        existing.Name = manifest.Name;
        existing.InternalName = key;
        existing.Version = manifest.Version;
        existing.ApiVersion = manifest.ApiVersion;
        existing.SourceUrl = manifest.Url;
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
            var manifest = list.FirstOrDefault(x => KeyEquals(GetPluginKey(x), installed.InternalName));
            if (manifest is not null)
            {
                manifest.Url = FixUrl(manifest.Url, installed.PluginListUrl);
                manifest.RepositoryUrl ??= installed.RepositoryUrl;
                return manifest;
            }
        }

        if (!string.IsNullOrWhiteSpace(installed.RepositoryUrl))
        {
            var plugins = await GetRepositoryPluginsAsync(installed.RepositoryUrl, cancellationToken);
            return plugins.FirstOrDefault(x => KeyEquals(GetPluginKey(x), installed.InternalName));
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
        var plugins = await GetJsonAsync<List<RemotePluginManifest>>(pluginListUrl, cancellationToken);
        return plugins ?? [];
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

        var shortCodeUrl = $"https://cutt.ly/{Uri.EscapeDataString(input)}";
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
            InternalName = name ?? "remote-plugin",
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

        return Path.Combine(baseDir, "data/plugins");
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

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
