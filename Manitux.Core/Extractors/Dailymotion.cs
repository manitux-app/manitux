using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using Manitux.Core.Models;

namespace Manitux.Core.Extractors;

public class Dailymotion : ExtractorBase
{
    private const string MetadataBaseUrl = "https://www.dailymotion.com/player/metadata/video/";

    public override string Name => "Dailymotion";
    public override string MainUrl => "https://www.dailymotion.com";
    public override List<string> SupportedDomains => new()
    {
        "dailymotion.com",
        "www.dailymotion.com",
        "geo.dailymotion.com",
        "dai.ly"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            var embedUrl = GetEmbedUrl(videoSource.Url);
            if (embedUrl is null) return null;

            var videoId = GetVideoId(embedUrl);
            if (videoId is null) return null;

            var headers = GetHeaders();
            var metadataJson = await HttpGet(
                $"{MetadataBaseUrl}{videoId}",
                referer: embedUrl,
                headers: headers);

            if (string.IsNullOrWhiteSpace(metadataJson)) return null;

            var metadata = JsonSerializer.Deserialize<DailymotionMetadata>(metadataJson);
            if (metadata is null) return null;

            var streamUrl = GetBestStreamUrl(metadata);
            if (string.IsNullOrWhiteSpace(streamUrl)) return null;

            videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
            videoSource.Url = streamUrl;
            videoSource.Referer = referer;
            videoSource.Headers = new()
            {
                new HeaderModel { Name = "User-Agent", Value = headers["User-Agent"] }
            };

            var subtitles = GetSubtitles(metadata);
            videoSource.Subtitles = subtitles.Count > 0 ? subtitles : null;

            return videoSource;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "application/json, text/plain, */*",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private string? GetEmbedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (url.Contains("/embed/video/", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/video/", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.Contains("geo.dailymotion.com", StringComparison.OrdinalIgnoreCase))
        {
            var videoId = GetQueryValue(url, "video");
            return string.IsNullOrWhiteSpace(videoId) ? null : $"{MainUrl}/embed/video/{videoId}";
        }

        if (url.Contains("dai.ly/", StringComparison.OrdinalIgnoreCase))
        {
            var videoId = new Uri(url).AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(videoId) ? null : $"{MainUrl}/embed/video/{videoId}";
        }

        return null;
    }

    private static string? GetVideoId(string url)
    {
        var path = new Uri(url).AbsolutePath;
        var match = Regex.Match(path, @"/(?:embed/)?video/(?<id>[kx][A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private static string? GetQueryValue(string url, string key)
    {
        var query = new Uri(url).Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query)) return null;

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private static string? GetBestStreamUrl(DailymotionMetadata metadata)
    {
        if (metadata.Qualities is null) return null;

        if (metadata.Qualities.TryGetValue("auto", out var autoQualities))
        {
            var hlsUrl = autoQualities?
                .Select(x => x.Url)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x.Contains(".m3u8", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(hlsUrl)) return hlsUrl;
        }

        return metadata.Qualities
            .SelectMany(x => x.Value ?? [])
            .Select(x => x.Url)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x.Contains(".m3u8", StringComparison.OrdinalIgnoreCase));
    }

    private static List<SubtitleModel> GetSubtitles(DailymotionMetadata metadata)
    {
        var subtitles = new List<SubtitleModel>();
        if (metadata.Subtitles?.Data is null) return subtitles;

        foreach (var item in metadata.Subtitles.Data)
        {
            var label = string.IsNullOrWhiteSpace(item.Value.Label) ? item.Key : item.Value.Label;

            int i = 1;

            foreach (var url in item.Value.Urls ?? [])
            {
                if (string.IsNullOrWhiteSpace(url)) continue;

                subtitles.Add(new SubtitleModel
                {
                    Id = i.ToString(),
                    Name = label,
                    Url = url
                });

                i++;
            }
        }

        return subtitles;
    }

    private sealed class DailymotionMetadata
    {
        [JsonPropertyName("qualities")]
        public Dictionary<string, List<DailymotionQuality>?>? Qualities { get; set; }

        [JsonPropertyName("subtitles")]
        public DailymotionSubtitles? Subtitles { get; set; }
    }

    private sealed class DailymotionQuality
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private sealed class DailymotionSubtitles
    {
        [JsonPropertyName("enable")]
        public bool Enable { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, DailymotionSubtitleData>? Data { get; set; }
    }

    private sealed class DailymotionSubtitleData
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("urls")]
        public List<string>? Urls { get; set; }
    }
}
