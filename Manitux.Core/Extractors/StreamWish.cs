using AngleSharp.Dom;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class StreamWish : ExtractorBase
{
    public override string Name => "Streamwish";
    public override string MainUrl => "https://streamwish.to";
    public override List<string> SupportedDomains => new()
    {
        "streamwish.to", "mwish.pro", "dwish.pro", "embedwish.com", "wishembed.pro", "kswplayer.info",
        "wishfast.top", "streamwish.site", "sfastwish.com", "strwish.xyz", "strwish.com", "flaswish.com",
        "awish.pro", "obeywish.com", "jodwish.com", "swhoi.com", "multimovies.cloud", "uqloads.xyz",
        "cdnwish.com", "asnwish.com", "nekowish.my.id", "neko-stream.click", "swdyu.com",
        "wishonly.site", "playerwish.com", "streamhls.to", "hlswish.com"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var pageUrl = ResolveEmbedUrl(videoSource.Url);
        var pageBase = GetBaseUrl(pageUrl);
        var headers = GetHeaders(pageBase);
        var html = await HttpGet(pageUrl, referer: referer ?? pageBase + "/", headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        using var document = await HtmlParse(html);
        var script = GetPlayerScript(document, html);
        var jw = JwPlayerHelper.ExtractStreamLinks(script, Name, pageBase, headers);
        var source = jw.VideoSources.FirstOrDefault();
        if (source is null) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? source.Name : videoSource.Name;
        videoSource.Url = await ResolveM3u8(source.Url, pageUrl, headers) ?? source.Url;
        videoSource.Referer = pageUrl;
        videoSource.Headers = ToHeaderModels(headers);
        videoSource.Subtitles = jw.Subtitles.Count > 0 ? jw.Subtitles : null;
        return videoSource;
    }

    private string ResolveEmbedUrl(string inputUrl)
    {
        var baseUrl = GetBaseUrl(inputUrl);
        if (inputUrl.Contains("/f/", StringComparison.OrdinalIgnoreCase)) return baseUrl + "/" + inputUrl.Split("/f/").Last();
        if (inputUrl.Contains("/e/", StringComparison.OrdinalIgnoreCase)) return baseUrl + "/" + inputUrl.Split("/e/").Last();
        return inputUrl;
    }

    private static string GetPlayerScript(IDocument? document, string html)
    {
        var packed = JsUnpacker.Unpack(html);
        if (!string.IsNullOrWhiteSpace(packed)) return packed;

        return document?
                   .QuerySelectorAll("script")
                   .Select(x => x.TextContent)
                   .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)
                                        && (x.Contains("jwplayer(\"vplayer\").setup", StringComparison.OrdinalIgnoreCase)
                                            || x.Contains("sources:", StringComparison.OrdinalIgnoreCase)))
               ?? html;
    }

    private static Dictionary<string, string> GetHeaders(string origin)
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = "cross-site",
            ["Referer"] = origin + "/",
            ["Origin"] = origin,
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
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
