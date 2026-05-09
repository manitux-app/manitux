using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Uqload : ExtractorBase
{
    public override string Name => "Uqload";
    public override string MainUrl => "https://www.uqload.com";
    public override List<string> SupportedDomains => new() { "uqload.com", "uqload.co", "uqload.cx", "uqload.bz", "www.uqload.com" };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var html = await HttpGet(videoSource.Url, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html) || html.Contains("error_nofile", StringComparison.OrdinalIgnoreCase)) return null;

        var streamUrl = Regex.Match(html, @"sources:.*?[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["url"].Value;
        if (string.IsNullOrWhiteSpace(streamUrl)) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = streamUrl;
        videoSource.Referer = GetBaseUrl(videoSource.Url) + "/";
        return videoSource;
    }
}
