using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Builders;
using TlsClient.Core.Models.Entities;
using TlsClient.Core.Models.Requests;
using TlsClient.Native.Extensions;

namespace Manitux.Core.Extractors;

public class MailRu : ExtractorBase
{
    public override string Name => "MailRu";
    public override string MainUrl => "https://my.mail.ru";
    public override List<string> SupportedDomains => new()
    {
        "my.mail.ru"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var embedUrl = videoSource.Url;
        var videoId = GetVideoId(embedUrl);
        if (string.IsNullOrWhiteSpace(videoId)) return null;

        var metaUrl = $"{MainUrl}/+/video/meta/{videoId}";
        var response = await GetMetaResponse(metaUrl, embedUrl);
        if (response is null || string.IsNullOrWhiteSpace(response.Body)) return null;

        var videoData = JsonSerializer.Deserialize<MailRuData>(response.Body);
        var selectedVideo = videoData?.Videos?
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .OrderByDescending(x => GetQuality(x.Key))
            .FirstOrDefault();

        if (selectedVideo is null) return null;

        var streamUrl = selectedVideo.Url.StartsWith("//", StringComparison.Ordinal)
            ? $"https:{selectedVideo.Url}"
            : selectedVideo.Url;

        var headers = GetPlaybackHeaders(response.VideoKey);

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = streamUrl;
        videoSource.Referer = embedUrl;
        videoSource.Headers = ToHeaderModels(headers);
        return videoSource;
    }

    private static string? GetVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var match = Regex.Match(url, @"video/embed/(?<id>[^?#]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["id"].Value.Trim() : null;
    }

    private async Task<MailRuMetaResponse?> GetMetaResponse(string url, string referer)
    {
        string fileName = true switch
        {
            _ when OperatingSystem.IsWindows() => "tls-client.dll",
            _ when OperatingSystem.IsLinux() => "tls-client.so",
            _ when OperatingSystem.IsMacOS() => "tls-client.dylib",
            _ => "tlsclient.so"
        };

        var filePath = RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))
            ? fileName
            : Path.Combine(Environment.CurrentDirectory, fileName);

        using var client = new TlsClientBuilder()
            .WithIdentifier(TlsClientIdentifier.Cloudscraper)
            .WithUserAgent(GetUserAgent())
            .WithFollowRedirects()
            .WithNative(filePath)
            .Build();

        var response = await client.RequestAsync(new Request
        {
            RequestUrl = url,
            RequestMethod = HttpMethod.Get,
            Headers = new Dictionary<string, string>
            {
                ["Accept"] = "application/json, text/plain, */*",
                ["Referer"] = referer,
                ["User-Agent"] = GetUserAgent()
            }
        });

        if (!response.IsSuccessStatus || string.IsNullOrWhiteSpace(response.Body)) return null;

        string? videoKey = null;
        response.Cookies?.TryGetValue("video_key", out videoKey);
        if (string.IsNullOrWhiteSpace(videoKey))
        {
            videoKey = GetVideoKeyFromHeaders(response.Headers);
        }

        return new MailRuMetaResponse(response.Body, videoKey);
    }

    private static string? GetVideoKeyFromHeaders(Dictionary<string, List<string>>? headers)
    {
        if (headers is null) return null;

        var setCookie = headers
            .FirstOrDefault(x => string.Equals(x.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (setCookie is null) return null;

        foreach (var cookie in setCookie)
        {
            var match = Regex.Match(cookie, @"(?:^|;\s*)video_key=(?<value>[^;]+)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups["value"].Value;
        }

        return null;
    }

    private static Dictionary<string, string> GetPlaybackHeaders(string? videoKey)
    {
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = GetUserAgent()
        };

        if (!string.IsNullOrWhiteSpace(videoKey))
        {
            headers["Cookie"] = $"video_key={videoKey}";
        }

        return headers;
    }

    private static int GetQuality(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;

        var match = Regex.Match(key, @"(?<quality>\d{3,4})");
        return match.Success && int.TryParse(match.Groups["quality"].Value, out var quality) ? quality : 0;
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    }

    private static List<HeaderModel> ToHeaderModels(Dictionary<string, string> headers)
    {
        return headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    private sealed record MailRuMetaResponse(string Body, string? VideoKey);

    private sealed class MailRuData
    {
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("videos")]
        public List<MailRuVideoData>? Videos { get; set; }
    }

    private sealed class MailRuVideoData
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
    }
}
