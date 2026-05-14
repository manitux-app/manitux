using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Odnoklassniki : ExtractorBase
{
    public override string Name => "Odnoklassniki";
    public override string MainUrl => "https://odnoklassniki.ru";
    public override List<string> SupportedDomains => new()
    {
        "odnoklassniki.ru",
        //"ok.ru",
        //"m.ok.ru"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var headers = GetHeaders();
        var embedUrl = GetEmbedUrl(videoSource.Url);
        var html = await HttpGet(embedUrl, headers: headers, identifier: TlsClientIdentifier.Chrome144, useCookie: true);
        if (string.IsNullOrWhiteSpace(html)) return null;

        html = UnescapeHtmlPayload(html);
        var videosJson = Regex.Match(
            html,
            @"""videos""\s*:\s*(?<videos>\[[^\]]*\])",
            RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["videos"].Value;

        if (string.IsNullOrWhiteSpace(videosJson)) return null;
        //Debug.WriteLine(videosJson);

        var videos = JsonSerializer.Deserialize<List<OkRuVideo>>(videosJson);
        var selectedVideo = videos?
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .OrderByDescending(x => GetQuality(x.Name))
            .FirstOrDefault();

        if (selectedVideo is null) return null;

        var streamUrl = selectedVideo.Url.StartsWith("//", StringComparison.Ordinal)
            ? $"https:{selectedVideo.Url}"
            : selectedVideo.Url;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = streamUrl;
        videoSource.Referer = $"{MainUrl}/";
        videoSource.Headers = ToHeaderModels(headers);
        return videoSource;
    }

    private static string GetEmbedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        return url.Replace("/video/", "/videoembed/", StringComparison.OrdinalIgnoreCase);
    }

    private static string UnescapeHtmlPayload(string html)
    {
        return Regex.Replace(
            html.Replace("\\&quot;", "\"").Replace(@"\\", @"\"),
            @"\\u(?<code>[0-9A-Fa-f]{4})",
            match => ((char)Convert.ToInt32(match.Groups["code"].Value, 16)).ToString());
    }

    private Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Connection"] = "keep-alive",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "cross-site",
            ["Origin"] = MainUrl,
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private static int GetQuality(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;

        return name.ToUpperInvariant() switch
        {
            "MOBILE" => 144,
            "LOWEST" => 240,
            "LOW" => 360,
            "SD" => 480,
            "HD" => 720,
            "FULL" => 1080,
            "QUAD" => 1440,
            "ULTRA" => 2160,
            _ => GetQualityFromName(name)
        };
    }

    private static int GetQualityFromName(string name)
    {
        var match = Regex.Match(name, @"(?<quality>\d{3,4})");
        return match.Success && int.TryParse(match.Groups["quality"].Value, out var quality) ? quality : 0;
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    private sealed class OkRuVideo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
