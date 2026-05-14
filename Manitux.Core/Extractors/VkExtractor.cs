using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class VkExtractor : ExtractorBase
{
    public override string Name => "Vk";
    public override string MainUrl => "https://vkvideo.ru";
    public override List<string> SupportedDomains => new()
    {
        "vkvideo.ru",
        "www.vkvideo.ru",
        "vk.com",
        "www.vk.com",
        "m.vk.com"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var html = await GetPageHtml(videoSource.Url);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var stream = GetBestDirectVideo(html) ?? GetHlsOrDashVideo(html);
        if (stream is null) return null;

        var playbackHeaders = GetPlaybackHeaders();

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? stream.Name : videoSource.Name;
        videoSource.Url = stream.Url;
        videoSource.Referer = $"{MainUrl}/";
        videoSource.Headers = ToHeaderModels(playbackHeaders);
        return videoSource;
    }

    private async Task<string?> GetPageHtml(string url)
    {
        var headers = GetPageHeaders();
        var cookies = new Dictionary<string, string>();

        _ = await HttpGet(
            url,
            headers: headers,
            identifier: TlsClientIdentifier.Chrome144,
            useCookie: true,
            followRedirects: false,
            cookieOutput: cookies);

        var pageHeaders = new Dictionary<string, string>(headers);
        if (cookies.Count > 0)
        {
            pageHeaders["Cookie"] = string.Join("; ", cookies.Select(x => $"{x.Key}={x.Value}"));
        }

        return await HttpGet(
            url,
            headers: pageHeaders,
            identifier: TlsClientIdentifier.Chrome144,
            useCookie: true,
            followRedirects: false);
    }

    private static VkStream? GetBestDirectVideo(string html)
    {
        return Regex.Matches(html, @"""url(?<quality>[0-9]+)""\s*:\s*""(?<url>[^""]*)""", RegexOptions.IgnoreCase)
            .Select(match => new VkStream(
                "Vk",
                CleanUrl(match.Groups["url"].Value),
                int.TryParse(match.Groups["quality"].Value, out var quality) ? quality : 0))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .OrderByDescending(x => x.Quality)
            .FirstOrDefault();
    }

    private static VkStream? GetHlsOrDashVideo(string html)
    {
        foreach (var linkType in new[] { "hls", "dash_sep" })
        {
            var match = Regex.Match(html, $@"""{linkType}""\s*:\s*""(?<url>[^""]*)""", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var type = linkType.Contains("hls", StringComparison.OrdinalIgnoreCase) ? "HLS" : "Dash";
            var url = CleanUrl(match.Groups["url"].Value);
            if (!string.IsNullOrWhiteSpace(url)) return new VkStream($"Vk {type}", url, 0);
        }

        return null;
    }

    private static string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        var decoded = Regex.Replace(
            url,
            @"\\u(?<code>[0-9A-Fa-f]{4})",
            match => ((char)Convert.ToInt32(match.Groups["code"].Value, 16)).ToString());

        return decoded.Replace(@"\/", "/").Replace("\\", "");
    }

    private static Dictionary<string, string> GetPageHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = GetUserAgent(),
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Accept-Language"] = "en-US,en;q=0.5",
            ["Sec-GPC"] = "1",
            ["Connection"] = "keep-alive",
            ["Upgrade-Insecure-Requests"] = "1",
            ["Sec-Fetch-Dest"] = "document",
            ["Sec-Fetch-Mode"] = "navigate",
            ["Sec-Fetch-Site"] = "none",
            ["Sec-Fetch-User"] = "?1",
            ["Priority"] = "u=0, i",
            ["Pragma"] = "no-cache",
            ["Cache-Control"] = "no-cache"
        };
    }

    private static Dictionary<string, string> GetPlaybackHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = GetUserAgent(),
            ["Accept"] = "*/*",
            ["Accept-Language"] = "en-US,en;q=0.5",
            ["Referer"] = "https://vkvideo.ru/"
        };
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0";
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    private sealed record VkStream(string Name, string Url, int Quality);
}
