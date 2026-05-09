using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class LuluStream : ExtractorBase
{
    public override string Name => "LuluStream";
    public override string MainUrl => "https://luluvdo.com";
    public override List<string> SupportedDomains => new() { "luluvdo.com", "luluvdoo.com", "lulustream.com", "kinoger.pw" };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var baseUrl = GetBaseUrl(videoSource.Url);
        var fileCode = videoSource.Url.TrimEnd('/').Split('/').Last();
        var html = await HttpPost(
            $"{baseUrl}/dl",
            new Dictionary<string, string>
            {
                ["op"] = "embed",
                ["file_code"] = fileCode,
                ["auto"] = "1",
                ["referer"] = referer ?? string.Empty
            },
            referer: referer ?? baseUrl + "/",
            headers: new Dictionary<string, string>
            {
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
            });

        if (string.IsNullOrWhiteSpace(html)) return null;

        var jw = JwPlayerHelper.ExtractStreamLinks(html, Name, baseUrl, new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        });
        var source = jw.VideoSources.FirstOrDefault();
        if (source is null) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? source.Name : videoSource.Name;
        videoSource.Url = source.Url;
        videoSource.Referer = baseUrl + "/";
        videoSource.Headers = source.Headers;
        videoSource.Subtitles = jw.Subtitles.Count > 0 ? jw.Subtitles : null;
        return videoSource;
    }
}
