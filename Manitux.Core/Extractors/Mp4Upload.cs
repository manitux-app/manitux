using System.Text.RegularExpressions;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Mp4Upload : ExtractorBase
{
    public override string Name => "Mp4Upload";
    public override string MainUrl => "https://www.mp4upload.com";
    public override List<string> SupportedDomains => new() { "mp4upload.com", "www.mp4upload.com" };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var realUrl = NormalizeUrl(videoSource.Url);
        var html = await HttpGet(realUrl, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var unpacked = JsUnpacker.Unpack(html) ?? html;
        var streamUrl = Regex.Match(unpacked, @"player\.src\([""'](?<url>[^""']+)", RegexOptions.IgnoreCase).Groups["url"].Value;
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            streamUrl = Regex.Match(unpacked, @"player\.src\([\w\W]*?src:\s*[""'](?<url>[^""']+)", RegexOptions.IgnoreCase).Groups["url"].Value;
        }

        if (string.IsNullOrWhiteSpace(streamUrl)) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = streamUrl;
        videoSource.Referer = realUrl;
        return videoSource;
    }

    private string NormalizeUrl(string url)
    {
        var match = Regex.Match(url, @"mp4upload\.com/(?:embed-)?(?<id>[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
        return match.Success ? $"{MainUrl}/embed-{match.Groups["id"].Value}.html" : url;
    }
}
