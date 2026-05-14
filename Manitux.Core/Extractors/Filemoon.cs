using AngleSharp.Dom;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Filemoon : ExtractorBase
{
    public override string Name => "Filemoon";
    public override string MainUrl => "https://filemoon.to";
    public override List<string> SupportedDomains => new() { "filemoon.to", "filemoon.in", "filemoon.sx" };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var headers = GetHeaders(videoSource.Url);
        var html = await HttpGet(videoSource.Url, referer: referer ?? videoSource.Url, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        using var document = await HtmlParse(html);
        var iframeUrl = GetIframeUrl(document);
        if (!string.IsNullOrWhiteSpace(iframeUrl))
        {
            iframeUrl = FixUrl(iframeUrl, GetBaseUrl(videoSource.Url));
            html = await HttpGet(iframeUrl, referer: videoSource.Url, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
            if (string.IsNullOrWhiteSpace(html)) return null;
            videoSource.Referer = iframeUrl;
        }
        else
        {
            videoSource.Referer = videoSource.Url;
        }

        var unpacked = JsUnpacker.Unpack(html) ?? html;
        var jw = JwPlayerHelper.ExtractStreamLinks(unpacked, Name, GetBaseUrl(videoSource.Referer), headers);
        var source = jw.VideoSources.FirstOrDefault();
        if (source is null) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? source.Name : videoSource.Name;
        videoSource.Url = await ResolveM3u8(source.Url, videoSource.Referer, headers) ?? source.Url;
        videoSource.Headers = ToHeaderModels(headers);
        videoSource.Subtitles = jw.Subtitles.Count > 0 ? jw.Subtitles : null;
        return videoSource;
    }

    private static Dictionary<string, string> GetHeaders(string referer)
    {
        return new Dictionary<string, string>
        {
            ["Referer"] = referer,
            ["Sec-Fetch-Dest"] = "iframe",
            ["Sec-Fetch-Mode"] = "navigate",
            ["Sec-Fetch-Site"] = "cross-site",
            ["User-Agent"] = "Mozilla/5.0 (X11; Linux x86_64; rv:137.0) Gecko/20100101 Firefox/137.0"
        };
    }

    private static string? GetIframeUrl(IDocument? document)
    {
        return document?.QuerySelector("iframe")?.GetAttribute("src");
    }

    private static async Task<string?> ResolveM3u8(string url, string referer, Dictionary<string, string> headers)
    {
        if (!url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase) && !url.Contains("master.txt", StringComparison.OrdinalIgnoreCase)) return null;
        var probeHeaders = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase) { ["Referer"] = referer };
        var streams = await M3u8Helper.M3u8Generation(new M3u8Helper.M3u8Stream(url, Headers: probeHeaders), returnThis: false);
        return streams.OrderByDescending(x => x.Quality ?? 0).FirstOrDefault()?.StreamUrl;
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }
}
