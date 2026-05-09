using System.Text;
using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class CloudMailRu : ExtractorBase
{
    public override string Name => "CloudMailRu";
    public override string MainUrl => "https://cloud.mail.ru";
    public override List<string> SupportedDomains => new()
    {
        "cloud.mail.ru"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var headers = GetHeaders();
        var html = await HttpGet(videoSource.Url, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var videoBaseUrl = Regex.Match(
            html,
            @"videowl_view""\s*:\s*\{\s*""count""\s*:\s*""1""\s*,\s*""url""\s*:\s*""(?<url>[^""]+)""",
            RegexOptions.IgnoreCase).Groups["url"].Value;

        if (string.IsNullOrWhiteSpace(videoBaseUrl)) return null;

        var publicPath = GetPublicPath(videoSource.Url);
        if (string.IsNullOrWhiteSpace(publicPath)) return null;

        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicPath));

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = $"{videoBaseUrl.TrimEnd('/')}/0p/{encodedId}.m3u8?double_encode=1";
        videoSource.Referer = $"{MainUrl}/";
        videoSource.Headers = ToHeaderModels(headers);
        return videoSource;
    }

    private static string? GetPublicPath(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var index = url.IndexOf("public/", StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        return url[(index + "public/".Length)..];
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

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }
}
