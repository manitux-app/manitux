using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using Manitux.Core.Models;

namespace Manitux.Core.Extractors;

public class Youtube : ExtractorBase
{
    private const string InvidiousBaseUrl = "https://inv.nadeko.net";

    public override string Name => "YouTube";
    public override string MainUrl => "https://www.youtube.com";
    public override List<string> SupportedDomains => new()
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "music.youtube.com",
        "youtube-nocookie.com",
        "www.youtube-nocookie.com",
        "youtu.be"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        try
        {
            var videoId = GetVideoId(videoSource.Url);
            if (string.IsNullOrWhiteSpace(videoId)) return null;

            videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
            videoSource.Url = $"{InvidiousBaseUrl}/api/manifest/dash/id/{videoId}";
            videoSource.Referer = referer;
            videoSource.Headers = new()
            {
                new HeaderModel { Name = "User-Agent", Value = GetUserAgent() }
            };

            return await Task.FromResult(videoSource);
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    }

    private static string? GetVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var id = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return IsValidVideoId(id) ? id : null;
        }

        var queryId = GetQueryValue(uri.Query, "v");
        if (IsValidVideoId(queryId)) return queryId;

        var pathMatch = Regex.Match(
            uri.AbsolutePath,
            @"/(?:embed|shorts|live|v)/(?<id>[A-Za-z0-9_-]{6,})",
            RegexOptions.IgnoreCase);

        return pathMatch.Success ? pathMatch.Groups["id"].Value : null;
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private static bool IsValidVideoId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[A-Za-z0-9_-]{6,}$");
    }
}
