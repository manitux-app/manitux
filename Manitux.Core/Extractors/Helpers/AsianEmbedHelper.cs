using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors.Helpers;

public static class AsianEmbedHelper
{
    public static async Task<List<VideoSourceModel>> GetUrls(
        string url,
        string? referer = null,
        bool resolveExtractors = true)
    {
        var results = new List<VideoSourceModel>();
        if (string.IsNullOrWhiteSpace(url)) return results;

        using var http = new HttpHelper();
        var html = await http.HttpGet(
            url,
            referer: referer,
            headers: GetHeaders(),
            identifier: TlsClientIdentifier.Cloudscraper);

        if (string.IsNullOrWhiteSpace(html)) return results;

        using var document = await http.HtmlParse(html);
        if (document is null) return results;

        var links = document.QuerySelectorAll("div#list-server-more > ul > li.linkserver[data-video]");
        foreach (var link in links)
        {
            var dataVideo = link.GetAttribute("data-video");
            if (string.IsNullOrWhiteSpace(dataVideo)) continue;

            var source = new VideoSourceModel
            {
                Name = GetSourceName(link),
                Url = http.FixUrl(dataVideo, url),
                Referer = url
            };

            if (!resolveExtractors)
            {
                results.Add(source);
                continue;
            }

            var extractor = ExtractorManager.GetExtractorByUrl(source.Url);
            if (extractor is null)
            {
                results.Add(source);
                continue;
            }

            try
            {
                var extracted = await extractor.ExtractAsync(source, url);
                if (extracted is not null)
                {
                    results.Add(extracted);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Extractor.Log(LogLevel.Error, "AsianEmbedHelper", ex.ToString());
            }
        }

        return results;
    }

    private static string GetSourceName(IElement link)
    {
        var name = link.GetAttribute("data-name")
                   ?? link.GetAttribute("data-server")
                   ?? link.TextContent?.Trim();

        return string.IsNullOrWhiteSpace(name) ? "AsianEmbed" : name.Trim();
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }
}
