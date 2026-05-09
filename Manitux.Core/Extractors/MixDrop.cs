using System.Text.RegularExpressions;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class MixDrop : ExtractorBase
{
    public override string Name => "MixDrop";
    public override string MainUrl => "https://mixdrop.co";
    public override List<string> SupportedDomains => new()
    {
        "mixdrop.co", "mixdrop.ps", "mdy48tn97.com", "mxdrop.to", "mixdrop.si",
        "mixdrop.bz", "mixdrop.ag", "mixdrop.ch", "mixdrop.to"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var url = videoSource.Url.Replace("/f/", "/e/", StringComparison.OrdinalIgnoreCase);
        var html = await HttpGet(url, referer: referer ?? MainUrl, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var unpacked = JsUnpacker.Unpack(html) ?? html;
        var streamUrl = Regex.Match(unpacked, @"wurl.*?=.*?[""'](?<url>[^""']+)[""'];?", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["url"].Value;
        if (string.IsNullOrWhiteSpace(streamUrl)) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = FixProtocol(streamUrl);
        videoSource.Referer = url;
        return videoSource;
    }

    private static string FixProtocol(string url)
    {
        return url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url : url;
    }
}
