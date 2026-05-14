using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Manitux.Core.Helpers;
using Manitux.Core.Models;

namespace Manitux.Core.Extractors.Helpers;

public static class JwPlayerHelper
{
    private static readonly Regex SourceRegex = new(@"""?sources""?\s*:\s*(\[.*?\])", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex TracksRegex = new(@"""?tracks""?\s*:\s*(\[.*?\])", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex M3u8Regex = new(@"[:=]\s*[""']([^""'\s]+(?:\.m3u8|master\.txt)[^""'\s]*)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public static JwPlayerResult ExtractStreamLinks(
        string script,
        string sourceName,
        string mainUrl,
        Dictionary<string, string>? headers = null)
    {
        var result = new JwPlayerResult();
        if (string.IsNullOrWhiteSpace(script)) return result;

        var sourceMatches = SourceRegex
            .Matches(script)
            .SelectMany(x => ParseSources(x.Groups[1].Value))
            .Where(x => !string.IsNullOrWhiteSpace(x.File))
            .ToList();

        foreach (var source in sourceMatches)
        {
            result.VideoSources.Add(new VideoSourceModel
            {
                Name = string.IsNullOrWhiteSpace(source.Label) ? sourceName : $"{sourceName} - {source.Label}",
                Url = FixUrl(source.File, mainUrl),
                Referer = mainUrl,
                Headers = ToHeaderModels(headers)
            });
        }

        if (result.VideoSources.Count == 0)
        {
            foreach (Match match in M3u8Regex.Matches(script))
            {
                var url = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(url)) continue;

                result.VideoSources.Add(new VideoSourceModel
                {
                    Name = sourceName,
                    Url = FixUrl(url, mainUrl),
                    Referer = mainUrl,
                    Headers = ToHeaderModels(headers)
                });
            }
        }

        var trackMatches = TracksRegex
            .Matches(script)
            .SelectMany(x => ParseTracks(x.Groups[1].Value))
            .Where(IsSubtitleTrack)
            .ToList();

        var subtitleId = 1;
        foreach (var track in trackMatches)
        {
            result.Subtitles.Add(new SubtitleModel
            {
                Id = subtitleId.ToString(),
                Name = track.Label!,
                Url = FixUrl(track.File!, mainUrl)
            });

            subtitleId++;
        }

        return result;
    }

    public static bool CanParseJwScript(string script)
    {
        return !string.IsNullOrWhiteSpace(script) && (SourceRegex.IsMatch(script) || M3u8Regex.IsMatch(script));
    }

    private static List<JwSource> ParseSources(string value)
    {
        return ParseJsonList<JwSource>(
            value
                .AddMarks("file")
                .AddMarks("label")
                .AddMarks("type"));
    }

    private static List<JwTrack> ParseTracks(string value)
    {
        return ParseJsonList<JwTrack>(
            value
                .AddMarks("file")
                .AddMarks("label")
                .AddMarks("kind"));
    }

    private static List<T> ParseJsonList<T>(string value)
    {
        try
        {
            var normalized = value
                .Replace("\\/", "/")
                .Replace("'", "\"");

            normalized = Regex.Replace(normalized, @",\s*([\]}])", "$1");
            return JsonSerializer.Deserialize<List<T>>(normalized) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static bool IsSubtitleTrack(JwTrack track)
    {
        return !string.IsNullOrWhiteSpace(track.File)
               && !string.IsNullOrWhiteSpace(track.Label)
               && (track.Kind?.Contains("caption", StringComparison.OrdinalIgnoreCase) == true
                   || track.Kind?.Contains("subtitle", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string FixUrl(string url, string mainUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        if (url.StartsWith("//", StringComparison.OrdinalIgnoreCase)) return "https:" + url;

        return url.StartsWith("/", StringComparison.Ordinal)
            ? mainUrl.TrimEnd('/') + url
            : $"{mainUrl.TrimEnd('/')}/{url}";
    }

    private static List<HeaderModel>? ToHeaderModels(Dictionary<string, string>? headers)
    {
        return headers is null || headers.Count == 0
            ? null
            : headers.Select(x => new HeaderModel { Name = x.Key, Value = x.Value }).ToList();
    }

    private static string AddMarks(this string value, string field)
    {
        return Regex.Replace(value, $@"""?{Regex.Escape(field)}""?", $"\"{field}\"", RegexOptions.IgnoreCase);
    }

    public sealed class JwPlayerResult
    {
        public List<VideoSourceModel> VideoSources { get; } = new();
        public List<SubtitleModel> Subtitles { get; } = new();

        public bool HasAny => VideoSources.Count > 0 || Subtitles.Count > 0;
    }

    private sealed class JwSource
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public sealed class JwTrack
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }
    }
}
