using System.Text.RegularExpressions;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class Supervideo : ExtractorBase
{
    public override string Name => "Supervideo";
    public override string MainUrl => "https://supervideo.cc";
    public override List<string> SupportedDomains => new()
    {
        "supervideo.cc"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var html = await HttpGet(videoSource.Url, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var packedScript = Regex.Match(
            html,
            @"eval(?<script>(?:.|\n)*?)</script>",
            RegexOptions.IgnoreCase).Groups["script"].Value;

        var unpacked = string.IsNullOrWhiteSpace(packedScript)
            ? JsUnpacker.Unpack(html)
            : JsUnpacker.Unpack($"eval{packedScript}");

        if (string.IsNullOrWhiteSpace(unpacked)) return null;

        var result = JwPlayerHelper.ExtractStreamLinks(unpacked, Name, MainUrl);
        var source = result.VideoSources.FirstOrDefault();
        if (source is null) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? source.Name : videoSource.Name;
        videoSource.Url = source.Url;
        videoSource.Referer = $"{MainUrl}/";
        videoSource.Headers = source.Headers;
        videoSource.Subtitles = result.Subtitles.Count > 0 ? result.Subtitles : null;
        return videoSource;
    }
}
