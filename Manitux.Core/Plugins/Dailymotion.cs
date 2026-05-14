using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;

namespace Manitux.Core.Plugins;

public class Dailymotion : PluginBase
{
    private const string ApiBaseUrl = "https://api.dailymotion.com";
    private const string WatchBaseUrl = "https://www.dailymotion.com";

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.dailymotion",
        Name = "Dailymotion",
        Version = "1.0.0",
        Description = "Dailymotion resmi API uzerinden video kesfi, arama ve metadata saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = WatchBaseUrl,
        Favicon = "https://www.google.com/s2/favicons?domain=dailymotion.com&sz=64",
        Language = "en"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Popular", Url = BuildCategoryUrl("popular") }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var page = Math.Max(1, pageNumber);
            var url = $"{ApiBaseUrl}/videos?fields=id,title,thumbnail_360_url&limit=26&page={page}";
            var response = await HttpGet(url, referer: WatchBaseUrl, headers: GetHeaders());
            if (string.IsNullOrWhiteSpace(response)) return null;

            var results = JsonSerializer.Deserialize<DailymotionVideoListResponse>(response);
            var items = results?.List?
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
            var id = GetVideoId(pageItem.Url);
            if (string.IsNullOrWhiteSpace(id)) return null;

            var url = $"{ApiBaseUrl}/video/{id}?fields=id,title,description,thumbnail_720_url";
            var response = await HttpGet(url, referer: pageItem.Url, headers: GetHeaders());
            if (string.IsNullOrWhiteSpace(response)) return null;

            var detail = JsonSerializer.Deserialize<DailymotionVideoDetailResponse>(response);
            if (detail is null) return null;

            return new MediaInfoModel
            {
                Title = detail.Title ?? pageItem.Title,
                Url = BuildWatchUrl(detail.Id ?? id),
                Description = detail.Description,
                Poster = detail.Thumbnail720Url ?? pageItem.Poster,
                Backdrop = detail.Thumbnail720Url ?? pageItem.Poster,
                VideoSources = new List<VideoSourceModel>
                {
                    new()
                    {
                        Name = "Dailymotion",
                        Url = BuildEmbedUrl(detail.Id ?? id),
                        Referer = WatchBaseUrl
                    }
                }
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

            var url = $"{ApiBaseUrl}/videos?fields=id,title,thumbnail_360_url&limit=26&page=1&search={Uri.EscapeDataString(query)}";
            var response = await HttpGet(url, referer: WatchBaseUrl, headers: GetHeaders());
            if (string.IsNullOrWhiteSpace(response)) return null;

            var results = JsonSerializer.Deserialize<DailymotionVideoListResponse>(response);
            var items = results?.List?
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
        return await ExtractAsync(videoSource, videoSource.Referer ?? WatchBaseUrl);
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "application/json, text/plain, */*",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private static PageItemModel? ToPageItem(DailymotionVideoItem item, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Title)) return null;

        return new PageItemModel
        {
            CategoryName = categoryName,
            Title = item.Title,
            Url = BuildWatchUrl(item.Id),
            Poster = item.Thumbnail360Url
        };
    }

    private static string BuildCategoryUrl(string category)
    {
        return $"dailymotion://{category}";
    }

    private static string BuildWatchUrl(string id)
    {
        return $"{WatchBaseUrl}/video/{id}";
    }

    private static string BuildEmbedUrl(string id)
    {
        return $"{WatchBaseUrl}/embed/video/{id}";
    }

    private static string? GetVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (url.StartsWith("dailymotion://", StringComparison.OrdinalIgnoreCase)) return null;

        var match = Regex.Match(url, @"/video/(?<id>[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private sealed class DailymotionVideoListResponse
    {
        [JsonPropertyName("list")]
        public List<DailymotionVideoItem>? List { get; set; }
    }

    private sealed class DailymotionVideoItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("thumbnail_360_url")]
        public string? Thumbnail360Url { get; set; }
    }

    private sealed class DailymotionVideoDetailResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnail_720_url")]
        public string? Thumbnail720Url { get; set; }
    }
}
