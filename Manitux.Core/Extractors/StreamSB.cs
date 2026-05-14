using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Manitux.Core.Extractors.Utils;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class StreamSB : ExtractorBase
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public override string Name => "StreamSB";
    public override string MainUrl => "https://watchsb.com";
    public override List<string> SupportedDomains => new()
    {
        "watchsb.com", "sblona.com", "lvturbo.com", "sbrapid.com", "sbface.com", "sbsonic.com",
        "vidgomunimesb.xyz", "sbasian.pro", "sbnet.one", "keephealth.info", "sbspeed.com",
        "streamsss.net", "sbflix.xyz", "vidgomunime.xyz", "sbthe.com", "ssbstream.net",
        "sbfull.com", "sbplay1.com", "sbplay2.com", "sbplay3.com", "cloudemb.com",
        "sbplay.org", "embedsb.com", "pelistop.co", "streamsb.net", "sbplay.one",
        "sbplay2.xyz", "sbbrisk.com", "sblongvu.com"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var id = GetId(videoSource.Url);
        if (string.IsNullOrWhiteSpace(id)) return null;

        var baseUrl = GetBaseUrl(videoSource.Url);
        var apiUrl = $"{baseUrl}/375664356a494546326c4b797c7c6e756577776778623171737/{EncodeId(id)}".ToLowerInvariant();
        var headers = new Dictionary<string, string> { ["watchsb"] = "sbstream" };
        var json = await HttpGet(apiUrl, referer: videoSource.Url, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(json)) return null;

        var response = JsonSerializer.Deserialize<StreamSbResponse>(json);
        var file = response?.StreamData?.File;
        if (string.IsNullOrWhiteSpace(file)) return null;

        var streams = await M3u8Helper.GenerateM3u8(Name, file, videoSource.Url, headers: headers, returnThis: false);
        var source = streams.OrderByDescending(x => ExtractQuality(x)).FirstOrDefault();
        if (source is null) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? source.Name : videoSource.Name;
        videoSource.Url = source.Url;
        videoSource.Referer = videoSource.Url;
        videoSource.Headers = source.Headers;
        videoSource.Subtitles = response?.StreamData?.Subs?
            .Where(x => !string.IsNullOrWhiteSpace(x.File))
            .Select((x, index) => new SubtitleModel
            {
                Id = (index + 1).ToString(),
                Name = x.Label ?? $"Subtitle {index + 1}",
                Url = x.File!
            })
            .ToList();
        return videoSource;
    }

    private static string? GetId(string url)
    {
        return Regex.Match(url, @"(?:embed-|/e/)(?<id>[a-zA-Z\d_-]+)", RegexOptions.IgnoreCase).Groups["id"].Value;
    }

    private static string EncodeId(string id)
    {
        var code = $"{CreateHashTable(12)}||{id}||{CreateHashTable(12)}||streamsb";
        var builder = new StringBuilder();
        foreach (var c in code)
        {
            builder.Append(((int)c).ToString("x"));
        }

        return builder.ToString();
    }

    private static string CreateHashTable(int length)
    {
        var random = Random.Shared;
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            builder.Append(Alphabet[random.Next(Alphabet.Length)]);
        }

        return builder.ToString();
    }

    private static int ExtractQuality(VideoSourceModel source)
    {
        var match = Regex.Match(source.Name + " " + source.Url, @"(?<q>\d{3,4})p", RegexOptions.IgnoreCase);
        return match.Success ? int.Parse(match.Groups["q"].Value) : 0;
    }

    private sealed class StreamSbResponse
    {
        [JsonPropertyName("stream_data")]
        public StreamData? StreamData { get; set; }
    }

    private sealed class StreamData
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("subs")]
        public List<StreamSub>? Subs { get; set; }
    }

    private sealed class StreamSub
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }
}
