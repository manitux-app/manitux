using System.Text.Json;
using System.Text.Json.Serialization;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class VideoSeyred : ExtractorBase
{
    public override string Name => "VideoSeyred";
    public override string MainUrl => "https://videoseyred.in";
    public override List<string> SupportedDomains => new()
    {
        "videoseyred.in"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var videoId = GetVideoId(videoSource.Url);
        if (string.IsNullOrWhiteSpace(videoId)) return null;

        var json = await HttpGet($"{MainUrl}/playlist/{videoId}.json", identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(json)) return null;

        var playlist = JsonSerializer.Deserialize<List<VideoSeyredSource>>(json);
        var item = playlist?.FirstOrDefault();
        if (item is null) return null;

        var source = item.Sources?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.File));
        if (source is null) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = source.File;
        videoSource.Referer = $"{MainUrl}/";
        videoSource.Headers = new()
        {
            new HeaderModel { Name = "User-Agent", Value = GetUserAgent() }
        };

        var subtitles = GetSubtitles(item);
        videoSource.Subtitles = subtitles.Count > 0 ? subtitles : null;
        return videoSource;
    }

    private static string? GetVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var marker = "embed/";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        return url[(index + marker.Length)..].Split('?', '#')[0];
    }

    private List<SubtitleModel> GetSubtitles(VideoSeyredSource item)
    {
        var subtitles = new List<SubtitleModel>();
        var index = 1;

        foreach (var track in item.Tracks ?? [])
        {
            if (!string.Equals(track.Kind, "captions", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(track.Label) ||
                string.IsNullOrWhiteSpace(track.File))
            {
                continue;
            }

            subtitles.Add(new SubtitleModel
            {
                Id = index.ToString(),
                Name = track.Label,
                Url = FixUrl(track.File, MainUrl)
            });

            index++;
        }

        return subtitles;
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    }

    private sealed class VideoSeyredSource
    {
        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("sources")]
        public List<VsSource>? Sources { get; set; }

        [JsonPropertyName("tracks")]
        public List<VsTrack>? Tracks { get; set; }
    }

    private sealed class VsSource
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("default")]
        public string? Default { get; set; }
    }

    private sealed class VsTrack
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("default")]
        public string? Default { get; set; }
    }
}
