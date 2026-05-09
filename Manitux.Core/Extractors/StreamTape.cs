using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class StreamTape : ExtractorBase
{
    public override string Name => "StreamTape";
    public override string MainUrl => "https://streamtape.com";
    public override List<string> SupportedDomains => new()
    {
        "streamtape.com", "streamtape.net", "streamtape.xyz", "watchadsontape.com", "shavetape.cash"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var html = await HttpGet(videoSource.Url, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var streamPath = ExtractBotLink(html);
        if (string.IsNullOrWhiteSpace(streamPath)) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = FixProtocol(streamPath) + "&stream=1";
        videoSource.Referer = videoSource.Url;
        return videoSource;
    }

    private static string? ExtractBotLink(string html)
    {
        var line = html
            .Split('\n')
            .FirstOrDefault(x => x.Contains("botlink').innerHTML", StringComparison.OrdinalIgnoreCase)
                                 || x.Contains("botlink\").innerHTML", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(line)) return null;

        var expression = line.Contains(").innerHTML", StringComparison.Ordinal)
            ? line.Substring(line.IndexOf(").innerHTML", StringComparison.Ordinal) + ").innerHTML".Length)
            : line;

        expression = expression.Trim().TrimStart('=').Trim().TrimEnd(';');

        var literals = Regex.Matches(expression, @"['""](?<value>[^'""]*)['""]")
            .Select(x => x.Groups["value"].Value)
            .Where(x => x.Contains("/", StringComparison.Ordinal) || x.Contains("&", StringComparison.Ordinal) || x.Contains("=", StringComparison.Ordinal))
            .ToList();

        if (literals.Count == 0) return null;
        return string.Concat(literals);
    }

    private static string FixProtocol(string url)
    {
        return url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url : url;
    }
}
