using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class VidMoly : ExtractorBase
{
    public override string Name => "VidMoly";
    public override string MainUrl => "https://vidmoly.biz";
    public override List<string> SupportedDomains => new()
    {
        "vidmoly.to",
        "www.vidmoly.to",
        "vidmoly.me",
        "www.vidmoly.me",
        "vidmoly.net",
        "www.vidmoly.net",
        "vidmoly.biz",
        "www.vidmoly.biz",
        "videobin.co",
        "www.videobin.co"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            var candidates = GetCandidateUrls(videoSource.Url);
            if (candidates.Count == 0) return null;

            string? html = null;
            string? pageUrl = null;
            IDocument? document = null;

            foreach (var candidate in candidates)
            {
                html = await HttpGet(candidate, referer: referer, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
                if (string.IsNullOrWhiteSpace(html)) continue;

                if (IsDeleted(html))
                {
                    Log(LogLevel.Warning, $"Video deleted or unavailable: {candidate}");
                    return null;
                }

                document?.Dispose();
                document = await HtmlParse(html);
                if (document is null) continue;

                pageUrl = candidate;
                if (HasPlayableContent(html)) break;
            }

            if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(pageUrl) || document is null) return null;

            if (NeedsNumberChallenge(html))
            {
                var challengeHtml = await SubmitNumberChallenge(pageUrl, document, referer);
                if (!string.IsNullOrWhiteSpace(challengeHtml))
                {
                    html = challengeHtml;
                    document.Dispose();
                    document = await HtmlParse(html);
                    if (document is null) return null;
                }
            }

            var jwPlayerResult = TryExtractJwPlayer(document, pageUrl);
            if (jwPlayerResult?.VideoSources.Count > 0)
            {
                var source = jwPlayerResult.VideoSources[0];
                var pageBaseUrl = GetBaseUri(pageUrl);
                var playbackHeaders = GetPlaybackHeaders(pageBaseUrl);
                var resolvedUrl = await ResolveM3u8VariantUrl(source.Url, pageUrl, playbackHeaders);

                videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? source.Name : videoSource.Name;
                videoSource.Url = resolvedUrl ?? source.Url;
                videoSource.Referer = pageUrl.EndsWith("/")? pageUrl: pageUrl+"/";
                videoSource.Headers = ToHeaderModels(playbackHeaders);
                videoSource.Subtitles = jwPlayerResult.Subtitles.Count > 0 ? jwPlayerResult.Subtitles : null;

                document.Dispose();
                return videoSource;
            }

            var streamUrl = FindVideoUrl(html);
            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                Log(LogLevel.Warning, $"Video URL not found: {pageUrl}");
                return null;
            }

            var userAgent = GetUserAgent();
            var fallbackHeaders = GetPlaybackHeaders(GetBaseUri(pageUrl));
            var fallbackStreamUrl = FixUrl(streamUrl, MainUrl);
            videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
            videoSource.Url = await ResolveM3u8VariantUrl(fallbackStreamUrl, pageUrl, fallbackHeaders) ?? fallbackStreamUrl;
            videoSource.Referer = pageUrl;
            videoSource.Headers = ToHeaderModels(fallbackHeaders);

            var subtitles = GetSubtitles(html);
            videoSource.Subtitles = subtitles.Count > 0 ? subtitles : null;

            document.Dispose();
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
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["User-Agent"] = GetUserAgent(),
            ["Sec-Fetch-Dest"] = "iframe"
        };
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    }

    private static Dictionary<string, string> GetPlaybackHeaders(string origin)
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = GetUserAgent(),
            ["Origin"] = origin
        };
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    private static async Task<string?> ResolveM3u8VariantUrl(string url, string referer, Dictionary<string, string> playbackHeaders)
    {
        if (!url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var probeHeaders = new Dictionary<string, string>(playbackHeaders, StringComparer.OrdinalIgnoreCase)
            {
                ["Referer"] = referer
            };

            var streams = await M3u8Helper.M3u8Generation(
                new M3u8Helper.M3u8Stream(url, Headers: probeHeaders),
                returnThis: false);

            return streams
                .OrderByDescending(x => x.Quality ?? 0)
                .FirstOrDefault()
                ?.StreamUrl;
        }
        catch
        {
            return null;
        }
    }

    private List<string> GetCandidateUrls(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return new List<string>();

        var candidates = new List<string>();
        AddCandidate(candidates, NormalizeWatchUrl(url));
        AddCandidate(candidates, url);

        var normalized = Regex.Replace(url, @"https?://vidmoly\.[a-z]+", MainUrl, RegexOptions.IgnoreCase);
        AddCandidate(candidates, NormalizeWatchUrl(normalized));
        AddCandidate(candidates, normalized);

        var watchUrl = Regex.Replace(normalized, @"/embed-([a-z0-9]+)\.html", "/w/$1", RegexOptions.IgnoreCase);
        AddCandidate(candidates, watchUrl);

        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!candidates.Contains(url, StringComparer.OrdinalIgnoreCase)) candidates.Add(url);
    }

    private static string NormalizeWatchUrl(string url)
    {
        var match = Regex.Match(url, @"/w/(?<id>[a-z0-9]+)/*$", RegexOptions.IgnoreCase);
        if (!match.Success) return url;

        return Regex.Replace(url, @"/w/(?<id>[a-z0-9]+)/*$", $"/embed-{match.Groups["id"].Value}.html", RegexOptions.IgnoreCase);
    }

    private static bool IsDeleted(string html)
    {
        return html.Contains("this video not found", StringComparison.OrdinalIgnoreCase)
               || html.Contains("file was deleted", StringComparison.OrdinalIgnoreCase)
               || html.Contains("video not found", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasPlayableContent(string html)
    {
        return html.Contains("sources:", StringComparison.OrdinalIgnoreCase)
               || html.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
               || html.Contains(".mp4", StringComparison.OrdinalIgnoreCase)
               || html.Contains("jwplayer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool NeedsNumberChallenge(string html)
    {
        return html.Contains("Select number", StringComparison.OrdinalIgnoreCase)
               || html.Contains("select the number", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> SubmitNumberChallenge(string pageUrl, IDocument document, string? referer)
    {
        var answer = GetFirstText(document, "div.vhint b", "span.vhint b")
                     ?? Regex.Match(document.DocumentElement.OuterHtml, @"Please select\s+(\d+)", RegexOptions.IgnoreCase).Groups[1].Value;

        var op = GetInputValue(document, "op");
        var fileCode = GetInputValue(document, "file_code");
        if (string.IsNullOrWhiteSpace(op) || string.IsNullOrWhiteSpace(fileCode) || string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        var body = new Dictionary<string, string>
        {
            ["op"] = op,
            ["file_code"] = fileCode,
            ["answer"] = answer
        };

        AddIfNotEmpty(body, "ts", GetInputValue(document, "ts"));
        AddIfNotEmpty(body, "nonce", GetInputValue(document, "nonce"));
        AddIfNotEmpty(body, "ctok", GetInputValue(document, "ctok"));

        return await HttpPost(pageUrl, body, referer: referer ?? pageUrl, headers: GetHeaders());
    }

    private static void AddIfNotEmpty(Dictionary<string, string> body, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) body[key] = value;
    }

    private static string? GetInputValue(IDocument document, string name)
    {
        return document.QuerySelector($"input[name='{name}']")?.GetAttribute("value");
    }

    private static string? GetFirstText(IDocument document, params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var value = document.QuerySelector(selector)?.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    private string? FindVideoUrl(string html)
    {
        var unpackedUrl = UnpackAndFind(html);
        if (!string.IsNullOrWhiteSpace(unpackedUrl)) return unpackedUrl;

        if (html.Contains("#EXTM3U", StringComparison.OrdinalIgnoreCase))
        {
            var playlistUrl = html
                .Split('\n')
                .Select(x => x.Trim().Trim('"', '\''))
                .FirstOrDefault(x => x.StartsWith("http", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(playlistUrl)) return playlistUrl;
        }

        var sources = Regex.Match(html, @"sources:\s*\[(?<items>.*?)\]\s*,", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (sources.Success)
        {
            var sourceUrl = ExtractNamedValue(sources.Groups["items"].Value, "file");
            if (!string.IsNullOrWhiteSpace(sourceUrl)) return sourceUrl;
        }

        return Regex.Match(html, @"file\s*:\s*[""'](?<url>[^""']+\.(?:m3u8|mp4)[^""']*)[""']", RegexOptions.IgnoreCase).Groups["url"].Value;
    }

    private static JwPlayerHelper.JwPlayerResult? TryExtractJwPlayer(IDocument document, string pageUrl)
    {
        var script = document
            .QuerySelectorAll("script")
            .Select(x => x.TextContent)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x.Contains("sources:", StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(script)
            ? null
            : JwPlayerHelper.ExtractStreamLinks(script, "VidMoly", GetBaseUri(pageUrl), new Dictionary<string, string>
            {
                ["User-Agent"] = GetUserAgent()
            });
    }

    private static string GetBaseUri(string url)
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}";
    }

    private string? UnpackAndFind(string html)
    {
        foreach (Match match in Regex.Matches(
                     html,
                     @"eval\(function\(p,a,c,k,e,(?:r|d)\).*?\}\(\s*'(?<payload>(?:\\'|[^'])*)'\s*,\s*(?<radix>\d+)\s*,\s*(?<count>\d+)\s*,\s*'(?<symbols>(?:\\'|[^'])*)'\.split\('\|'\)",
                     RegexOptions.Singleline))
        {
            var unpacked = UnpackDeanEdwards(match);
            var url = GetMediaUrl(unpacked);
            if (!string.IsNullOrWhiteSpace(url)) return url;
        }

        return null;
    }

    private static string UnpackDeanEdwards(Match match)
    {
        var payload = UnescapePackedString(match.Groups["payload"].Value);
        var radix = int.Parse(match.Groups["radix"].Value);
        var symbols = UnescapePackedString(match.Groups["symbols"].Value).Split('|');

        return Regex.Replace(payload, @"\b\w+\b", word =>
        {
            var index = FromBase(word.Value, radix);
            return index >= 0 && index < symbols.Length && !string.IsNullOrEmpty(symbols[index])
                ? symbols[index]
                : word.Value;
        });
    }

    private static string UnescapePackedString(string value)
    {
        return value.Replace("\\'", "'").Replace("\\\\", "\\");
    }

    private static int FromBase(string value, int radix)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var result = 0;

        foreach (var c in value)
        {
            var digit = chars.IndexOf(c);
            if (digit < 0 || digit >= radix) return -1;
            result = result * radix + digit;
        }

        return result;
    }

    private static string? GetMediaUrl(string text)
    {
        var match = Regex.Match(text, @"https?://[^\s""'<>]+?\.(?:m3u8|mp4)(?:\?[^\s""'<>]*)?", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Replace("\\/", "/") : null;
    }

    private static string? ExtractNamedValue(string text, string field)
    {
        var match = Regex.Match(
            text,
            $@"[""']?{Regex.Escape(field)}[""']?\s*:\s*[""'](?<value>[^""']+)[""']",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["value"].Value.Replace("\\/", "/") : null;
    }

    private List<SubtitleModel> GetSubtitles(string html)
    {
        var subtitles = new List<SubtitleModel>();
        var tracks = Regex.Match(html, @"tracks:\s*\[(?<items>.*?)\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!tracks.Success) return subtitles;

        var itemMatches = Regex.Matches(tracks.Groups["items"].Value, @"\{(?<item>.*?)\}", RegexOptions.Singleline);
        var index = 1;

        foreach (Match itemMatch in itemMatches)
        {
            var item = itemMatch.Groups["item"].Value;
            var kind = ExtractNamedValue(item, "kind");
            if (!string.Equals(kind, "captions", StringComparison.OrdinalIgnoreCase)) continue;

            var file = ExtractNamedValue(item, "file");
            if (string.IsNullOrWhiteSpace(file)) continue;

            subtitles.Add(new SubtitleModel
            {
                Id = index.ToString(),
                Name = ExtractNamedValue(item, "label") ?? $"Subtitle {index}",
                Url = FixUrl(file, MainUrl)
            });

            index++;
        }

        return subtitles;
    }
}
