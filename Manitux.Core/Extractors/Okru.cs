using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using CodeLogic.Core.Logging;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Okru : ExtractorBase
{
    public override string Name => "Okru";
    public override string MainUrl => "https://ok.ru";
    public override List<string> SupportedDomains => new()
    {
        //"odnoklassniki.ru",
        "ok.ru",
        "m.ok.ru",
        "vkvideo.ru",
        "vk.com"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            var url = UnwrapHrefLi(videoSource.Url);

            if (IsVkUrl(url))
            {
                return await ExtractVk(videoSource, url, referer);
            }

            var embedUrl = GetEmbedUrl(url);
            var headers = GetHeaders();
            headers["Origin"] = MainUrl;

            var html = await HttpGet(
                embedUrl,
                referer: referer,
                headers: headers,
                identifier: TlsClientIdentifier.Chrome144,
                useCookie: true,
                followRedirects: true);

            if (string.IsNullOrWhiteSpace(html)) return null;

            using var document = await HtmlParse(html);
            var metadata = document is null ? null : ExtractMetadataFromDataOptions(document);
            var bestUrl = GetBestMetadataUrl(metadata);

            if (string.IsNullOrWhiteSpace(bestUrl))
            {
                var videos = ExtractVideosFromHtml(html);
                bestUrl = GetBestVideoUrl(videos);
            }

            if (string.IsNullOrWhiteSpace(bestUrl))
            {
                if (IsBlockedOrUnavailable(html))
                {
                    Log(LogLevel.Warning, $"Video blocked or unavailable: {embedUrl}");
                }
                else
                {
                    Log(LogLevel.Warning, $"Video data not found: {embedUrl}");
                }

                return null;
            }

            videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
            videoSource.Url = FixUrl(CleanEscapedUrl(bestUrl), MainUrl);
            videoSource.Referer = embedUrl;
            videoSource.Headers = ToHeaderModels(GetPlaybackHeaders(headers["User-Agent"], embedUrl));
            return videoSource;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private async Task<VideoSourceModel?> ExtractVk(VideoSourceModel videoSource, string url, string? referer)
    {
        var embedUrl = GetVkEmbedUrl(url);
        if (string.IsNullOrWhiteSpace(embedUrl))
        {
            Log(LogLevel.Warning, $"VK video id could not be parsed: {url}");
            return null;
        }

        var userAgent = GetUserAgent();
        var html = await HttpGet(
            embedUrl,
            referer: referer,
            headers: new Dictionary<string, string> { ["User-Agent"] = userAgent },
            identifier: TlsClientIdentifier.Chrome144,
            followRedirects: true);

        if (string.IsNullOrWhiteSpace(html)) return null;

        var bestUrl = GetBestVkUrl(html);
        if (string.IsNullOrWhiteSpace(bestUrl))
        {
            Log(LogLevel.Warning, $"VK video URL not found: {url}");
            return null;
        }

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = FixUrl(CleanEscapedUrl(bestUrl), "https://vkvideo.ru");
        videoSource.Referer = embedUrl;
        videoSource.Headers = ToHeaderModels(GetPlaybackHeaders(userAgent, embedUrl));
        return videoSource;
    }

    private static OkruMetadata? ExtractMetadataFromDataOptions(IHtmlDocument document)
    {
        var rawOptions = document
            .QuerySelector("[data-module='OKVideo']")
            ?.GetAttribute("data-options");

        if (string.IsNullOrWhiteSpace(rawOptions)) return null;

        try
        {
            using var optionsDocument = JsonDocument.Parse(WebUtility.HtmlDecode(rawOptions));
            if (!optionsDocument.RootElement.TryGetProperty("flashvars", out var flashvars)) return null;
            if (!flashvars.TryGetProperty("metadata", out var metadataElement)) return null;

            var metadata = metadataElement.ValueKind == JsonValueKind.String
                ? metadataElement.GetString()
                : metadataElement.GetRawText();

            return string.IsNullOrWhiteSpace(metadata)
                ? null
                : JsonSerializer.Deserialize<OkruMetadata>(metadata);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetBestMetadataUrl(OkruMetadata? metadata)
    {
        if (metadata is null) return null;

        if (!string.IsNullOrWhiteSpace(metadata.OndemandHls)) return metadata.OndemandHls;
        if (!string.IsNullOrWhiteSpace(metadata.OndemandDash)) return metadata.OndemandDash;

        return GetBestVideoUrl(metadata.Videos);
    }

    private static List<OkruVideo>? ExtractVideosFromHtml(string html)
    {
        var text = WebUtility.HtmlDecode(html)
            .Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\/", "/", StringComparison.Ordinal);

        var videosJson = Regex.Match(
            text,
            @"""videos""\s*[:=]\s*(?<videos>\[.*?\])",
            RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["videos"].Value;

        if (string.IsNullOrWhiteSpace(videosJson))
        {
            var metadataJson = Regex.Match(
                text,
                @"""metadata""\s*[:=]\s*""?(?<metadata>\{.*?\})""?",
                RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["metadata"].Value;

            if (!string.IsNullOrWhiteSpace(metadataJson))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<OkruMetadata>(CleanJsonPayload(metadataJson));
                    return metadata?.Videos;
                }
                catch
                {
                    videosJson = Regex.Match(
                        metadataJson,
                        @"""videos""\s*:\s*(?<videos>\[.*?\])",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["videos"].Value;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(videosJson)) return null;

        try
        {
            return JsonSerializer.Deserialize<List<OkruVideo>>(CleanJsonPayload(videosJson));
        }
        catch
        {
            return null;
        }
    }

    private static string? GetBestVideoUrl(List<OkruVideo>? videos)
    {
        if (videos is not { Count: > 0 }) return null;

        foreach (var quality in new[] { "ULTRA", "QUAD", "FULL", "HD", "SD", "LOW", "MOBILE" })
        {
            var url = videos.FirstOrDefault(x =>
                string.Equals(x.Name, quality, StringComparison.OrdinalIgnoreCase))?.Url;

            if (!string.IsNullOrWhiteSpace(url)) return url;
        }

        return videos.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Url))?.Url;
    }

    private static string? GetBestVkUrl(string html)
    {
        foreach (var quality in new[] { "1080", "720", "480", "360", "240", "144" })
        {
            var url = Regex.Match(
                html,
                $@"""mp4_{quality}""\s*:\s*""(?<url>[^""]+)""",
                RegexOptions.IgnoreCase).Groups["url"].Value;

            if (!string.IsNullOrWhiteSpace(url)) return url.Replace("\\/", "/", StringComparison.Ordinal);
        }

        return null;
    }

    private static string? GetVkEmbedUrl(string url)
    {
        if (url.Contains("video_ext.php", StringComparison.OrdinalIgnoreCase)) return url;

        var match = Regex.Match(url, @"/video(?<oid>-?\d+)_(?<id>\d+)", RegexOptions.IgnoreCase);
        return match.Success
            ? $"https://vkvideo.ru/video_ext.php?oid={match.Groups["oid"].Value}&id={match.Groups["id"].Value}"
            : null;
    }

    private static string GetEmbedUrl(string url)
    {
        return url.Contains("/video/", StringComparison.OrdinalIgnoreCase)
            ? url.Replace("/video/", "/videoembed/", StringComparison.OrdinalIgnoreCase)
            : url;
    }

    private static string UnwrapHrefLi(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        var index = url.IndexOf("href.li/?", StringComparison.OrdinalIgnoreCase);
        return index < 0 ? url : url[(index + "href.li/?".Length)..];
    }

    private static bool IsVkUrl(string url)
    {
        return url.Contains("vkvideo.ru", StringComparison.OrdinalIgnoreCase)
               || url.Contains("vk.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockedOrUnavailable(string html)
    {
        return html.Contains("Видео заблокировано", StringComparison.OrdinalIgnoreCase)
               || html.Contains("copyrightsRestricted", StringComparison.OrdinalIgnoreCase)
               || html.Contains("not_found", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanJsonPayload(string value)
    {
        return WebUtility.HtmlDecode(value)
            .Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\u0026", "&", StringComparison.Ordinal)
            .Replace("u0026", "&", StringComparison.Ordinal);
    }

    private static string CleanEscapedUrl(string url)
    {
        var value = WebUtility.HtmlDecode(url)
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\u0026", "&", StringComparison.Ordinal)
            .Replace("u0026", "&", StringComparison.Ordinal);

        if (value.Contains("\\u", StringComparison.Ordinal))
        {
            value = Regex.Replace(
                value,
                @"\\u(?<code>[0-9A-Fa-f]{4})",
                match => ((char)Convert.ToInt32(match.Groups["code"].Value, 16)).ToString());
        }

        return value;
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["User-Agent"] = GetUserAgent()
        };
    }

    private Dictionary<string, string> GetPlaybackHeaders(string userAgent, string referer)
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = userAgent,
            ["Referer"] = referer,
            ["Origin"] = MainUrl,
            ["Accept"] = "*/*",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "cross-site"
        };
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    private sealed class OkruMetadata
    {
        [JsonPropertyName("ondemandHls")]
        public string? OndemandHls { get; set; }

        [JsonPropertyName("ondemandDash")]
        public string? OndemandDash { get; set; }

        [JsonPropertyName("videos")]
        public List<OkruVideo>? Videos { get; set; }
    }

    private sealed class OkruVideo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
