using System.Text.Json;
using Manitux.Services.Storage;

namespace Manitux.Services.WatchedEpisodes;

public class WatchedEpisodesService : IWatchedEpisodesService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private Dictionary<string, Dictionary<string, DateTimeOffset>>? _watchedEpisodes;

    public async Task<bool> IsWatchedAsync(string? pluginId, string seriesUrl, string episodeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(seriesUrl) || string.IsNullOrWhiteSpace(episodeUrl))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var watchedEpisodes = await GetOrLoadAsync(cancellationToken);
            return watchedEpisodes.TryGetValue(CreateSeriesKey(pluginId, seriesUrl), out var episodes)
                   && episodes.ContainsKey(episodeUrl);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlySet<string>> GetWatchedEpisodeUrlsAsync(string? pluginId, string seriesUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(seriesUrl))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var watchedEpisodes = await GetOrLoadAsync(cancellationToken);
            return watchedEpisodes.TryGetValue(CreateSeriesKey(pluginId, seriesUrl), out var episodes)
                ? episodes.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MarkAsWatchedAsync(string? pluginId, string seriesUrl, string episodeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(seriesUrl) || string.IsNullOrWhiteSpace(episodeUrl))
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var watchedEpisodes = await GetOrLoadAsync(cancellationToken);
            var seriesKey = CreateSeriesKey(pluginId, seriesUrl);
            if (!watchedEpisodes.TryGetValue(seriesKey, out var episodes))
            {
                episodes = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
                watchedEpisodes[seriesKey] = episodes;
            }

            episodes[episodeUrl] = DateTimeOffset.UtcNow;
            await WriteAsync(watchedEpisodes, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UnmarkAsWatchedAsync(string? pluginId, string seriesUrl, string episodeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(seriesUrl) || string.IsNullOrWhiteSpace(episodeUrl))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var watchedEpisodes = await GetOrLoadAsync(cancellationToken);
            var seriesKey = CreateSeriesKey(pluginId, seriesUrl);
            if (!watchedEpisodes.TryGetValue(seriesKey, out var episodes))
            {
                return false;
            }

            var removed = episodes.Remove(episodeUrl);
            if (!removed)
            {
                return false;
            }

            if (episodes.Count == 0)
            {
                watchedEpisodes.Remove(seriesKey);
            }

            await WriteAsync(watchedEpisodes, cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, Dictionary<string, DateTimeOffset>>> GetOrLoadAsync(CancellationToken cancellationToken)
    {
        if (_watchedEpisodes is not null)
        {
            return _watchedEpisodes;
        }

        var path = GetWatchedEpisodesFilePath();
        if (!File.Exists(path))
        {
            _watchedEpisodes = new Dictionary<string, Dictionary<string, DateTimeOffset>>(StringComparer.OrdinalIgnoreCase);
            return _watchedEpisodes;
        }

        await using var stream = File.OpenRead(path);
        _watchedEpisodes = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, DateTimeOffset>>>(
                               stream,
                               _jsonOptions,
                               cancellationToken)
                           ?? new Dictionary<string, Dictionary<string, DateTimeOffset>>(StringComparer.OrdinalIgnoreCase);

        _watchedEpisodes = new Dictionary<string, Dictionary<string, DateTimeOffset>>(
            _watchedEpisodes.ToDictionary(
                x => x.Key,
                x => new Dictionary<string, DateTimeOffset>(x.Value, StringComparer.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

        return _watchedEpisodes;
    }

    private async Task WriteAsync(Dictionary<string, Dictionary<string, DateTimeOffset>> watchedEpisodes, CancellationToken cancellationToken)
    {
        var path = GetWatchedEpisodesFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            watchedEpisodes
                .OrderBy(x => x.Key)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.OrderByDescending(y => y.Value).ToDictionary(y => y.Key, y => y.Value)),
            _jsonOptions,
            cancellationToken);
    }

    private static string CreateSeriesKey(string? pluginId, string seriesUrl)
    {
        return $"{pluginId?.Trim() ?? string.Empty}::{seriesUrl.Trim()}";
    }

    private static string GetWatchedEpisodesFilePath()
    {
        return AppDataPath.GetDataPath("favorites", "episodes.json");
    }
}
