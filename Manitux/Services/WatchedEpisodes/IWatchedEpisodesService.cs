namespace Manitux.Services.WatchedEpisodes;

public interface IWatchedEpisodesService
{
    Task<bool> IsWatchedAsync(string? pluginId, string seriesUrl, string episodeUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetWatchedEpisodeUrlsAsync(string? pluginId, string seriesUrl, CancellationToken cancellationToken = default);
    Task MarkAsWatchedAsync(string? pluginId, string seriesUrl, string episodeUrl, CancellationToken cancellationToken = default);
    Task<bool> UnmarkAsWatchedAsync(string? pluginId, string seriesUrl, string episodeUrl, CancellationToken cancellationToken = default);
}
