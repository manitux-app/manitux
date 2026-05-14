using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Voe : ExtractorBase
{
    private static readonly Regex RedirectRegex = new(@"window.location.href\s*=\s*'(?<url>[^']+)';", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override string Name => "Voe";
    public override string MainUrl => "https://voe.sx";
    public override List<string> SupportedDomains => new()
    {
        "voe.sx", "tubelessceliolymph.com", "simpulumlamerop.com", "urochsunloath.com",
        "nathanfromsubject.com", "yip.su", "metagnathtuggers.com", "donaldlineelse.com",
        "charlestoughrace.com"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var html = await HttpGet(videoSource.Url, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var redirectUrl = RedirectRegex.Match(html).Groups["url"].Value;
        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            html = await HttpGet(redirectUrl, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
            if (string.IsNullOrWhiteSpace(html)) return null;
        }

        using var document = await HtmlParse(html);
        var encoded = GetEncodedString(document);
        if (string.IsNullOrWhiteSpace(encoded)) return null;

        using var json = DecryptF7(encoded);
        var root = json.RootElement;
        var m3u8 = root.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() : null;
        var mp4 = root.TryGetProperty("direct_access_url", out var mp4Element) ? mp4Element.GetString() : null;

        if (!string.IsNullOrWhiteSpace(m3u8))
        {
            var headers = new Dictionary<string, string> { ["Origin"] = GetBaseUrl(videoSource.Url) };
            var streams = await M3u8Helper.GenerateM3u8(Name, m3u8, GetBaseUrl(videoSource.Url) + "/", headers: headers, returnThis: false);
            var stream = streams.FirstOrDefault();
            if (stream is not null)
            {
                videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? stream.Name : videoSource.Name;
                videoSource.Url = stream.Url;
                videoSource.Referer = stream.Referer;
                videoSource.Headers = stream.Headers;
                return videoSource;
            }
        }

        if (string.IsNullOrWhiteSpace(mp4)) return null;
        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? $"{Name} MP4" : videoSource.Name;
        videoSource.Url = mp4;
        videoSource.Referer = videoSource.Url;
        return videoSource;
    }

    private static string? GetEncodedString(IDocument? document)
    {
        var data = document?.QuerySelector("script[type='application/json']")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(data)) return null;

        return data.Contains("[\"", StringComparison.Ordinal)
            ? data.Substring(data.IndexOf("[\"", StringComparison.Ordinal) + 2)
                .Replace("\"]", string.Empty, StringComparison.Ordinal)
                .Trim()
            : null;
    }

    private static JsonDocument DecryptF7(string input)
    {
        var value = Rot13(input);
        value = ReplacePatterns(value).Replace("_", string.Empty, StringComparison.Ordinal);
        value = Base64Decode(value);
        value = new string(value.Select(c => (char)(c - 3)).ToArray());
        value = new string(value.Reverse().ToArray());
        value = Base64Decode(value);
        return JsonDocument.Parse(value);
    }

    private static string Rot13(string input)
    {
        return new string(input.Select(c => c switch
        {
            >= 'A' and <= 'Z' => (char)((c - 'A' + 13) % 26 + 'A'),
            >= 'a' and <= 'z' => (char)((c - 'a' + 13) % 26 + 'a'),
            _ => c
        }).ToArray());
    }

    private static string ReplacePatterns(string input)
    {
        var patterns = new[] { "@$", "^^", "~@", "%?", "*~", "!!", "#&" };
        return patterns.Aggregate(input, (current, pattern) => current.Replace(pattern, "_", StringComparison.Ordinal));
    }

    private static string Base64Decode(string value)
    {
        var normalized = value.Trim();
        var padding = normalized.Length % 4;
        if (padding > 0) normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }
}
