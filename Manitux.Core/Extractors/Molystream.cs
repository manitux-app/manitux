using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Molystream : ExtractorBase
{
    private const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:101.0) Gecko/20100101 Firefox/101.0";

    public override string Name => "Molystream";
    public override string MainUrl => "https://dbx.molystream.org";
    public override List<string> SupportedDomains => new()
    {
        "dbx.molystream.org",
        "ydx.molystream.org",
        "molystream.org",
        "yd.sheila.stream",
        "ydf.popcornvakti.net",
        "rufiiguta.com",
        "www.molystream.net"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            var embedUrl = NormalizeEmbedUrl(videoSource.Url);
            var requestReferer = string.IsNullOrWhiteSpace(referer) ? MainUrl : referer;
            var html = await HttpGet(embedUrl, referer: requestReferer, headers: GetHeaders(requestReferer), identifier: TlsClientIdentifier.Cloudscraper);
            if (string.IsNullOrWhiteSpace(html)) return null;

            if (IsBlocked(html))
            {
                Log(CodeLogic.Core.Logging.LogLevel.Warning, $"Cloudflare block detected: {embedUrl}");
                return null;
            }

            var decryptedHtml = DecryptHtml(html);
            if (!string.IsNullOrWhiteSpace(decryptedHtml))
                html = decryptedHtml;

            var streamUrl = await ExtractStreamUrl(html, embedUrl);
            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                if (RequiresProxyPlaylist(html))
                    Log(CodeLogic.Core.Logging.LogLevel.Warning, $"MolyStream proxy playlist fallback is not available: {embedUrl}");

                return null;
            }

            var baseUrl = GetBaseUrl(embedUrl);
            var playbackHeaders = GetPlaybackHeaders(baseUrl, embedUrl);
            var resolvedUrl = await ResolveM3u8VariantUrl(streamUrl, playbackHeaders);

            videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
            videoSource.Url = resolvedUrl ?? streamUrl;
            videoSource.Referer = embedUrl;
            videoSource.Headers = playbackHeaders
                .Select(header => new HeaderModel { Name = header.Key, Value = header.Value })
                .ToList();
            var subtitles = ExtractSubtitles(html, embedUrl);
            videoSource.Subtitles = subtitles.Count > 0 ? subtitles : null;

            return videoSource;
        }
        catch (Exception ex)
        {
            Log(CodeLogic.Core.Logging.LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private async Task<string?> ExtractStreamUrl(string html, string embedUrl)
    {
        using var document = await HtmlParse(html);
        var sourceUrl = document?.QuerySelector("video source[src]")?.GetAttribute("src")
            ?? document?.QuerySelector("source[src]")?.GetAttribute("src");

        if (!string.IsNullOrWhiteSpace(sourceUrl))
            return FixUrl(sourceUrl, embedUrl);

        var namedSource = ExtractNamedMediaUrl(html);
        if (!string.IsNullOrWhiteSpace(namedSource))
            return FixUrl(namedSource, embedUrl);

        var jwResult = JwPlayerHelper.ExtractStreamLinks(html, Name, GetBaseUrl(embedUrl), GetPlaybackHeaders(GetBaseUrl(embedUrl), embedUrl));
        var jwSource = jwResult.VideoSources.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(jwSource?.Url))
            return jwSource.Url;

        var direct = Regex.Match(
            html,
            @"https?://[^\s""'<>]+?(?:\.m3u8|\.mp4|master\.txt|/embed/sheila/)[^\s""'<>]*",
            RegexOptions.IgnoreCase).Value;

        return string.IsNullOrWhiteSpace(direct)
            ? null
            : direct.Replace("\\/", "/", StringComparison.Ordinal);
    }

    private static string? ExtractNamedMediaUrl(string html)
    {
        var patterns = new[]
        {
            @"(?:file|source)\s*[:=]\s*[""'](?<url>[^""']+\.(?:m3u8|mp4|master\.txt)[^""']*)[""']",
            @"""(?:file|src)""\s*:\s*""(?<url>[^""]+\.(?:m3u8|mp4|master\.txt)[^""]*)"""
        };

        foreach (var pattern in patterns)
        {
            var url = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["url"].Value;
            if (!string.IsNullOrWhiteSpace(url))
                return url.Replace("\\/", "/", StringComparison.Ordinal);
        }

        return null;
    }

    private static List<SubtitleModel> ExtractSubtitles(string html, string embedUrl)
    {
        var subtitles = new List<SubtitleModel>();
        var index = 1;

        foreach (Match match in Regex.Matches(
                     html,
                     @"addSrtFile\([""'](?<url>[^""']+\.srt)[""']\s*,\s*[""'][a-z]{2}[""']\s*,\s*[""'](?<name>[^""']+)[""']",
                     RegexOptions.IgnoreCase))
        {
            subtitles.Add(new SubtitleModel
            {
                Id = index.ToString(),
                Name = match.Groups["name"].Value,
                Url = FixUrlStatic(match.Groups["url"].Value, embedUrl)
            });

            index++;
        }

        return subtitles;
    }

    private static string? DecryptHtml(string html)
    {
        var match = Regex.Match(
            html,
            @"CryptoJS\.AES\.decrypt\([""'](?<data>[^""']+)[""']\s*,\s*[""'](?<pass>[^""']+)[""']\)",
            RegexOptions.Singleline);

        if (!match.Success) return null;

        return CryptoJSHelper.Decrypt(match.Groups["pass"].Value, match.Groups["data"].Value);
    }

    private static string NormalizeEmbedUrl(string url)
    {
        if (url.Contains("/embed/sheila/", StringComparison.OrdinalIgnoreCase))
            return url.Replace("/embed/sheila/", "/embed/", StringComparison.OrdinalIgnoreCase);

        return url;
    }

    private static bool IsBlocked(string html)
    {
        return html.Contains("Attention Required! | Cloudflare", StringComparison.OrdinalIgnoreCase)
               || html.Contains("Sorry, you have been blocked", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresProxyPlaylist(string html)
    {
        return html.Contains("const datas", StringComparison.OrdinalIgnoreCase)
               || html.Contains("atob(datas)", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> GetHeaders(string referer)
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["User-Agent"] = GetUserAgent(),
            ["Referer"] = referer,
            ["Sec-Fetch-Dest"] = "iframe"
        };
    }

    private static Dictionary<string, string> GetPlaybackHeaders(string origin, string referer)
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = GetUserAgent(),
            ["Origin"] = origin,
            ["Referer"] = referer
        };
    }

    private static string GetUserAgent()
    {
        return UserAgent;
    }

    private static async Task<string?> ResolveM3u8VariantUrl(string url, Dictionary<string, string> playbackHeaders)
    {
        if (!url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/embed/sheila/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var streams = await M3u8Helper.M3u8Generation(
                new M3u8Helper.M3u8Stream(url, Headers: playbackHeaders),
                returnThis: false);

            return streams
                .OrderByDescending(stream => stream.Quality ?? 0)
                .FirstOrDefault()
                ?.StreamUrl;
        }
        catch
        {
            return null;
        }
    }

    private static string FixUrlStatic(string url, string mainUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        if (url.StartsWith("//", StringComparison.OrdinalIgnoreCase)) return "https:" + url;

        return new Uri(new Uri(mainUrl), url).ToString();
    }
}
