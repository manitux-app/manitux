using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using Manitux.Core.Models;

namespace Manitux.Core.Extractors;

public abstract class VideoPlayerExtractor : ExtractorBase
{
    protected virtual bool StripQuery => false;
    protected virtual bool LowerKey => false;

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            var requestUrl = StripQuery ? StripQueryString(videoSource.Url) : videoSource.Url;
            var headers = GetHeaders(referer ?? videoSource.Url);
            var html = await HttpGet(requestUrl, referer: referer ?? videoSource.Url, headers: headers);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var videoUrl = Regex.Match(html, @"videoUrl""\s*:\s*""(?<value>[^"",]+)""", RegexOptions.IgnoreCase).Groups["value"].Value;
            var videoServer = Regex.Match(html, @"videoServer""\s*:\s*""(?<value>[^"",]+)""", RegexOptions.IgnoreCase).Groups["value"].Value;
            if (string.IsNullOrWhiteSpace(videoUrl) || string.IsNullOrWhiteSpace(videoServer))
            {
                Log(LogLevel.Warning, $"Video url/server not found: {videoSource.Url}");
                return null;
            }

            using var document = await HtmlParse(html);
            var suffix = ResolveSuffix(document?.DocumentElement, html, GetPartKey(videoSource.Url));
            var baseUrl = StripQuery ? MainUrl.TrimEnd('/') : GetBaseUrl(videoSource.Url);
            var resolvedUrl = $"{baseUrl}{CleanJsUrl(videoUrl)}?s={videoServer}";

            videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? $"{Name} - {suffix}" : videoSource.Name;
            videoSource.Url = resolvedUrl;
            videoSource.Referer = requestUrl;
            videoSource.Headers = ToHeaderModels(headers);
            return videoSource;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    protected virtual string ResolveSuffix(IElement? root, string html, string partKey)
    {
        var key = LowerKey ? partKey.ToLowerInvariant() : partKey;

        if (key.Contains("turkcedublaj", StringComparison.OrdinalIgnoreCase)) return "Dublaj";
        if (key.Contains("turkcealtyazi", StringComparison.OrdinalIgnoreCase)) return "Altyazi";
        if (!string.IsNullOrWhiteSpace(partKey)) return partKey;

        var title = Regex.Match(html, @"title""\s*:\s*""(?<value>[^"",]+)""", RegexOptions.IgnoreCase).Groups["value"].Value;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var parts = title.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 0) return parts[^1];
        }

        return "Bilinmiyor";
    }

    private static Dictionary<string, string> GetHeaders(string referer)
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Referer"] = referer,
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private static string StripQueryString(string url)
    {
        var index = url.IndexOf('?', StringComparison.Ordinal);
        return index < 0 ? url : url[..index];
    }

    private static string GetPartKey(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return string.Empty;

        return uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Where(parts => parts[0].Equals("partKey", StringComparison.OrdinalIgnoreCase))
            .Select(parts => Uri.UnescapeDataString(parts[1].Replace('+', ' ')))
            .FirstOrDefault() ?? string.Empty;
    }

    private static string CleanJsUrl(string value)
    {
        return value
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal);
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }
}
