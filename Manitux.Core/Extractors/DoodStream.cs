using System.Text;
using System.Text.RegularExpressions;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Extractors;

public class DoodStream : ExtractorBase
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public override string Name => "DoodStream";
    public override string MainUrl => "https://dood.la";
    public override List<string> SupportedDomains => new()
    {
        "dood.la", "doods.pro", "dsvplay.com", "d0000d.com", "d000d.com", "doodstream.com",
        "dooood.com", "dood.wf", "dood.cx", "dood.sh", "dood.watch", "dood.pm",
        "dood.to", "dood.so", "dood.ws", "dood.yt", "dood.li", "ds2play.com",
        "ds2video.com", "vide0.net", "myvidplay.com", "playmogo.com"
    };

    public override async Task<VideoSourceModel?> ExtractAsync(VideoSourceModel videoSource, string? referer = null)
    {
        var embedUrl = videoSource.Url.Replace("/d/", "/e/", StringComparison.OrdinalIgnoreCase);
        var html = await HttpGet(embedUrl, referer: referer, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(html)) return null;

        var host = GetBaseUrl(embedUrl);
        var passPath = Regex.Match(html, @"/pass_md5/[^'""\s<]+", RegexOptions.IgnoreCase).Value;
        if (string.IsNullOrWhiteSpace(passPath)) return null;

        var md5 = host + passPath;
        var baseStream = await HttpGet(md5, referer: embedUrl, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(baseStream)) return null;

        videoSource.Name = string.IsNullOrWhiteSpace(videoSource.Name) ? Name : videoSource.Name;
        videoSource.Url = baseStream + CreateHashTable(10) + "?token=" + md5.Split('/').Last();
        videoSource.Referer = host + "/";
        return videoSource;
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
}
