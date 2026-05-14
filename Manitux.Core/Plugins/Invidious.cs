using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;

namespace Manitux.Core.Plugins;

public class Invidious : PluginBase
{
    private const string DefaultMainUrl = "https://inv.nadeko.net";

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.invidious",
        Name = "Invidious",
        Version = "1.0.0",
        Description = "Invidious API uzerinden video kesfi, arama ve metadata saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = DefaultMainUrl,
        Favicon = "https://www.google.com/s2/favicons?domain=invidious.io&sz=64",
        Language = "en"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Popular", Url = BuildCategoryUrl("popular") },
            new() { Title = "Trending", Url = BuildCategoryUrl("trending") }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var type = ParseCategoryType(category.Url);
            if (string.IsNullOrWhiteSpace(type)) return null;

            var response = await HttpGet(
                $"{MainUrl}/api/v1/{type}?fields=videoId,title",
                referer: MainUrl,
                headers: GetHeaders());

            if (!LooksLikeJson(response)) return null;

            var items = JsonSerializer.Deserialize<List<InvidiousSearchEntry>>(response!)?
                .Select(x => ToPageItem(x, category.Title))
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            return items is { Count: > 0 } ? items : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem)
    {
        try
        {
            var videoId = GetVideoId(pageItem.Url);
            if (string.IsNullOrWhiteSpace(videoId)) return null;

            var url = $"{MainUrl}/api/v1/videos/{videoId}?fields=videoId,title,description,recommendedVideos,author,authorThumbnails,formatStreams";
            var response = await HttpGet(url, referer: pageItem.Url, headers: GetHeaders());
            if (!LooksLikeJson(response))
            {
                Log(LogLevel.Warning, $"Invidious detail endpoint returned non-JSON for {videoId}: {Preview(response)}");
                return CreateFallbackMediaInfo(pageItem, videoId);
            }

            var detail = JsonSerializer.Deserialize<InvidiousVideoEntry>(response!);
            if (detail is null) return null;

            var id = string.IsNullOrWhiteSpace(detail.VideoId) ? videoId : detail.VideoId;
            var relatedVideos = detail.RecommendedVideos?
                .Select(x => ToRelatedVideo(x))
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            return new MediaInfoModel
            {
                Title = detail.Title ?? pageItem.Title,
                Url = BuildWatchUrl(id),
                Description = detail.Description,
                Poster = BuildPosterUrl(id, "hqdefault"),
                Backdrop = BuildPosterUrl(id, "maxresdefault"),
                Actors = detail.Author,
                VideoSources = new List<VideoSourceModel>
                {
                    new()
                    {
                        Name = "YouTube",
                        Url = BuildYoutubeWatchUrl(id),
                        Referer = MainUrl
                    }
                },
                RelatedVideos = relatedVideos is { Count: > 0 } ? relatedVideos : null
            };
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            var url = $"{MainUrl}/api/v1/search?q={Uri.EscapeDataString(query)}&page=1&type=video&fields=videoId,title";
            var response = await HttpGet(url, referer: MainUrl, headers: GetHeaders());
            if (!LooksLikeJson(response)) return null;

            var items = JsonSerializer.Deserialize<List<InvidiousSearchEntry>>(response!)?
                .Select(x => ToPageItem(x, "Search"))
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            return items is { Count: > 0 } ? items : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return await ExtractAsync(videoSource, videoSource.Referer ?? MainUrl);
    }

    private string MainUrl => string.IsNullOrWhiteSpace(Config.MainUrl)
        ? DefaultMainUrl
        : Config.MainUrl.TrimEnd('/');

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "application/json, text/plain, */*",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private PageItemModel? ToPageItem(InvidiousSearchEntry item, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(item.VideoId) || string.IsNullOrWhiteSpace(item.Title)) return null;

        return new PageItemModel
        {
            CategoryName = categoryName,
            Title = item.Title,
            Url = BuildWatchUrl(item.VideoId),
            Poster = BuildPosterUrl(item.VideoId, "mqdefault")
        };
    }

    private RelatedVideoModel? ToRelatedVideo(InvidiousSearchEntry item)
    {
        if (string.IsNullOrWhiteSpace(item.VideoId) || string.IsNullOrWhiteSpace(item.Title)) return null;

        return new RelatedVideoModel
        {
            Title = item.Title,
            Url = BuildWatchUrl(item.VideoId),
            Poster = BuildPosterUrl(item.VideoId, "mqdefault")
        };
    }

    private static string BuildCategoryUrl(string type)
    {
        return $"invidious://{type}";
    }

    private string BuildWatchUrl(string videoId)
    {
        return $"{MainUrl}/watch?v={videoId}";
    }

    private string BuildPosterUrl(string videoId, string quality)
    {
        return $"{MainUrl}/vi/{videoId}/{quality}.jpg";
    }

    private static string BuildYoutubeWatchUrl(string videoId)
    {
        return $"https://www.youtube.com/watch?v={videoId}";
    }

    private static string ParseCategoryType(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        const string prefix = "invidious://";
        return url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? url[prefix.Length..]
            : url;
    }

    private static string? GetVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var match = Regex.Match(url, @"[?&]v=(?<id>[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private MediaInfoModel CreateFallbackMediaInfo(PageItemModel pageItem, string videoId)
    {
        return new MediaInfoModel
        {
            Title = pageItem.Title,
            Url = BuildWatchUrl(videoId),
            Poster = pageItem.Poster ?? BuildPosterUrl(videoId, "hqdefault"),
            Backdrop = BuildPosterUrl(videoId, "maxresdefault"),
            VideoSources = new List<VideoSourceModel>
            {
                new()
                {
                    Name = "YouTube",
                    Url = BuildYoutubeWatchUrl(videoId),
                    Referer = MainUrl
                }
            }
        };
    }

    private static bool LooksLikeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static string Preview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var trimmed = value.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..120];
    }

    private sealed class InvidiousSearchEntry
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }
    }

    private sealed class InvidiousVideoEntry
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }

        [JsonPropertyName("recommendedVideos")]
        public List<InvidiousSearchEntry>? RecommendedVideos { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }
    }
}
