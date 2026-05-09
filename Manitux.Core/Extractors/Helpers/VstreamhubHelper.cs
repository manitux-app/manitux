using System.Text.RegularExpressions;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors.Helpers;

public static class VstreamhubHelper
{
    private const string BaseUrl = "https://vstreamhub.com";
    private const string BaseName = "Vstreamhub";

    public static async Task<List<VideoSourceModel>> GetUrls(
        string url,
        bool resolveExtractors = true)
    {
        var results = new List<VideoSourceModel>();
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return results;
        }

        using var http = new HttpHelper();
        var html = await http.HttpGet(
            url,
            referer: BaseUrl,
            headers: GetHeaders(),
            identifier: TlsClientIdentifier.Cloudscraper);

        if (string.IsNullOrWhiteSpace(html)) return results;

        using var document = await http.HtmlParse(html);
        if (document is null) return results;

        foreach (var script in document.QuerySelectorAll("script"))
        {
            var innerText = script.TextContent;
            if (string.IsNullOrWhiteSpace(innerText)) continue;

            var streamUrl = GetFileUrl(innerText);
            if (!string.IsNullOrWhiteSpace(streamUrl))
            {
                results.Add(new VideoSourceModel
                {
                    Name = $"{BaseName} m3u8",
                    Url = http.FixUrl(streamUrl, BaseUrl),
                    Referer = url
                });
            }

            var dataVideo = GetPlayerButtonUrl(innerText);
            if (string.IsNullOrWhiteSpace(dataVideo)) continue;

            var source = new VideoSourceModel
            {
                Name = BaseName,
                Url = http.FixUrl(dataVideo, BaseUrl),
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

            var extracted = await extractor.ExtractAsync(source, url);
            if (extracted is not null)
            {
                results.Add(extracted);
            }
        }

        return results
            .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static string? GetFileUrl(string script)
    {
        var match = Regex.Match(
            script,
            @"file\s*:\s*[""'](?<url>[^""']+)[""']",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["url"].Value : null;
    }

    private static string? GetPlayerButtonUrl(string script)
    {
        if (!script.Contains("playerInstance", StringComparison.OrdinalIgnoreCase)) return null;

        var match = Regex.Match(
            script,
            @"window\.open\(\s*\[\s*[""'](?<url>[^""']+)[""']\s*\]",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["url"].Value : null;
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
