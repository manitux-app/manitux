using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Extractors;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Plugins;

public class DiziBox : PluginBase
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36";

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.dizibox",
        Name = "DiziBox",
        Version = "1.0.0",
        Description = "DiziBox uzerinden yabanci dizi katalog, metadata ve kaynak kesfi saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://www.dizibox.live",
        Favicon = "https://www.google.com/s2/favicons?domain=www.dizibox.live&sz=64",
        Language = "tr"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        var mainUrl = Config.MainUrl.TrimEnd('/');

        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Yerli", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?ulke[]=turkiye&yil=&imdb" },
            new() { Title = "Aile", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=aile&yil&imdb" },
            new() { Title = "Aksiyon", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=aksiyon&yil&imdb" },
            new() { Title = "Animasyon", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=animasyon&yil&imdb" },
            new() { Title = "Belgesel", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=belgesel&yil&imdb" },
            new() { Title = "Bilimkurgu", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=bilimkurgu&yil&imdb" },
            new() { Title = "Biyografi", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=biyografi&yil&imdb" },
            new() { Title = "Dram", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=dram&yil&imdb" },
            new() { Title = "Drama", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=drama&yil&imdb" },
            new() { Title = "Fantastik", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=fantastik&yil&imdb" },
            new() { Title = "Gerilim", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=gerilim&yil&imdb" },
            new() { Title = "Gizem", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=gizem&yil&imdb" },
            new() { Title = "Komedi", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=komedi&yil&imdb" },
            new() { Title = "Korku", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=korku&yil&imdb" },
            new() { Title = "Macera", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=macera&yil&imdb" },
            new() { Title = "Muzik", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=muzik&yil&imdb" },
            new() { Title = "Muzikal", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=muzikal&yil&imdb" },
            new() { Title = "Reality TV", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=reality-tv&yil&imdb" },
            new() { Title = "Romantik", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=romantik&yil&imdb" },
            new() { Title = "Savas", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=savas&yil&imdb" },
            new() { Title = "Spor", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=spor&yil&imdb" },
            new() { Title = "Suc", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=suc&yil&imdb" },
            new() { Title = "Tarih", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=tarih&yil&imdb" },
            new() { Title = "Western", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=western&yil&imdb" },
            new() { Title = "Yarisma", Url = $"{mainUrl}/dizi-arsivi/page/SAYFA/?tur[0]=yarisma&yil&imdb" }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var targetUrl = category.Url.Replace("SAYFA", pageNumber.ToString(), StringComparison.OrdinalIgnoreCase);
            var html = await HttpGet(targetUrl, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var results = ParseCards(document, category.Title);
            return results.Count > 0 ? results : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        try
        {
            var url = $"{Config.MainUrl.TrimEnd('/')}/?s={Uri.EscapeDataString(query)}";
            var html = await HttpGet(url, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var results = ParseCards(document, "Arama");
            return results.Count > 0 ? results : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem)
    {
        try
        {
            var html = await HttpGet(pageItem.Url, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var title = document.QuerySelector("div.tv-overview h1 a")?.TextContent?.Trim()
                ?? pageItem.Title;
            var poster = document.QuerySelector("div.tv-overview figure img")?.GetAttribute("data-src")
                ?? document.QuerySelector("div.tv-overview figure img")?.GetAttribute("src")
                ?? pageItem.Poster;
            var description = document.QuerySelector("div.tv-story p")?.TextContent?.Trim();
            var year = Regex.Match(document.QuerySelector("a[href*='/yil/']")?.TextContent ?? "", @"\d{4}").Value;
            var rating = Regex.Match(document.QuerySelector("span.label-imdb b")?.TextContent ?? "", @"[\d.,]+").Value;
            var tags = JoinTexts(document.QuerySelectorAll("a[href*='/tur/']"));
            var actors = JoinTexts(document.QuerySelectorAll("a[href*='/oyuncu/']"));
            var episodes = await LoadEpisodesAsync(document);
            var videoSources = CreateVideoSources(document, pageItem.Url);

            return new MediaInfoModel
            {
                Title = title,
                Url = pageItem.Url,
                Poster = string.IsNullOrWhiteSpace(poster) ? null : FixUrl(poster, Config.MainUrl),
                Backdrop = string.IsNullOrWhiteSpace(poster) ? null : FixUrl(poster, Config.MainUrl),
                Description = description,
                Year = string.IsNullOrWhiteSpace(year) ? null : year,
                Rating = string.IsNullOrWhiteSpace(rating) ? null : rating,
                Tags = tags,
                Actors = actors,
                Episodes = episodes.Count > 0 ? episodes : null,
                VideoSources = videoSources.Count > 0 ? videoSources : null
            };
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        try
        {
            if (videoSource.IsTrailer)
            {
                return await ExtractAsync(videoSource, "https://youtube.com/");
            }

            var sourcePageUrl = FixUrl(videoSource.Url, Config.MainUrl);
            var html = await HttpGet(sourcePageUrl, referer: videoSource.Referer ?? Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) return await ExtractAsync(videoSource, Config.MainUrl);

            using var document = await HtmlParse(html);
            if (document is null) return await ExtractAsync(videoSource, Config.MainUrl);

            var selectedName = document.QuerySelector("div.video-toolbar option[selected]")?.TextContent?.Trim();
            var mainIframe = document.QuerySelector("div#video-area iframe")?.GetAttribute("src");
            var sourceName = string.IsNullOrWhiteSpace(videoSource.Name)
                ? string.IsNullOrWhiteSpace(selectedName) ? Manifest.Name : selectedName
                : videoSource.Name;

            var decoded = await DecodeIframeAsync(sourceName, mainIframe, sourcePageUrl);
            return await ExtractFirstAsync(decoded, sourceName, sourcePageUrl, GetForcedExtractorName(sourceName, mainIframe));
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private List<PageItemModel> ParseCards(IParentNode document, string categoryName)
    {
        var results = new List<PageItemModel>();

        foreach (var item in document.QuerySelectorAll("article.detailed-article"))
        {
            var title = item.QuerySelector("h3 a")?.TextContent?.Trim();
            var href = item.QuerySelector("h3 a")?.GetAttribute("href");
            var poster = item.QuerySelector("img")?.GetAttribute("data-src")
                ?? item.QuerySelector("img")?.GetAttribute("src");

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href)) continue;

            results.Add(new PageItemModel
            {
                Title = title,
                Url = FixUrl(href, Config.MainUrl),
                Poster = string.IsNullOrWhiteSpace(poster) ? null : FixUrl(poster, Config.MainUrl),
                CategoryName = categoryName
            });
        }

        return results;
    }

    private async Task<List<EpisodeModel>> LoadEpisodesAsync(IParentNode document)
    {
        var episodes = new Dictionary<(int Season, int Episode), EpisodeModel>();
        var seasonLinks = document.QuerySelectorAll("div#seasons-list a[href]")
            .Select(link => FixUrl(link.GetAttribute("href") ?? "", Config.MainUrl))
            .Where(link => !string.IsNullOrWhiteSpace(link))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var seasonLink in seasonLinks)
        {
            var html = await HttpGet(seasonLink, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) continue;

            using var seasonDocument = await HtmlParse(html);
            if (seasonDocument is null) continue;

            foreach (var item in seasonDocument.QuerySelectorAll("article.grid-box"))
            {
                var name = item.QuerySelector("div.post-title a")?.TextContent?.Trim();
                var href = item.QuerySelector("div.post-title a")?.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

                var (season, episode) = ExtractSeasonEpisode($"{name} {href}");
                episodes[(season, episode)] = new EpisodeModel
                {
                    SeasonNumber = season,
                    EpisodeNumber = episode,
                    Title = name,
                    Url = FixUrl(href, Config.MainUrl)
                };
            }
        }

        return episodes.Values
            .OrderBy(item => item.SeasonNumber)
            .ThenBy(item => item.EpisodeNumber)
            .ToList();
    }

    private List<VideoSourceModel> CreateVideoSources(IParentNode document, string pageUrl)
    {
        var sources = new List<VideoSourceModel>();
        var trailer = FindTrailerUrl(document);

        if (!string.IsNullOrWhiteSpace(trailer))
        {
            sources.Add(new VideoSourceModel
            {
                Name = "Fragman",
                Url = FixUrl(trailer, Config.MainUrl),
                IsTrailer = true
            });
        }

        var toolbarOptions = document.QuerySelectorAll("div.video-toolbar option[value]")
            .Where(option => !string.IsNullOrWhiteSpace(option.GetAttribute("value")))
            .ToList();

        foreach (var option in toolbarOptions)
        {
            var sourceUrl = FixUrl(option.GetAttribute("value")!, Config.MainUrl);
            var sourceName = option.TextContent?.Trim();

            if (sources.Any(source => string.Equals(source.Url, sourceUrl, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            sources.Add(new VideoSourceModel
            {
                Name = string.IsNullOrWhiteSpace(sourceName) ? Manifest.Name : sourceName,
                Url = sourceUrl,
                Referer = pageUrl
            });
        }

        if (toolbarOptions.Count == 0 && HasPlayableIframe(document))
        {
            sources.Add(new VideoSourceModel
            {
                Name = Manifest.Name,
                Url = pageUrl,
                Referer = Config.MainUrl
            });
        }

        return sources;
    }

    private static bool HasPlayableIframe(IParentNode document) =>
        document.QuerySelector("div#video-area iframe") is not null
        || document.QuerySelector("iframe[src*='/player/']") is not null;

    private string? FindTrailerUrl(IParentNode document)
    {
        //var trailerLink = document.QuerySelector("a[href*='youtube'], a[href*='youtu.be'], a[href*='fragman'], a[data-video_url]")
            //?.GetAttribute("data-video_url")
            //?? document.QuerySelector("a[href*='youtube'], a[href*='youtu.be'], a[href*='fragman']")?.GetAttribute("href");

        var trailerLink = document.QuerySelector("iframe[src*='youtube']")?.GetAttribute("src") ?? "";

        return string.IsNullOrWhiteSpace(trailerLink) ? null : trailerLink;
    }

    private async Task<List<string>> DecodeIframeAsync(string name, string? iframeLink, string referer)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(iframeLink)) return results;

        iframeLink = FixUrl(iframeLink, Config.MainUrl);

        if (iframeLink.Contains("/player/king/king.php", StringComparison.OrdinalIgnoreCase))
        {
            iframeLink = iframeLink.Replace("king.php?v=", "king.php?wmode=opaque&v=", StringComparison.OrdinalIgnoreCase);
            var html = await HttpGet(iframeLink, referer: referer, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) return results;

            using var document = await HtmlParse(html);
            var nestedIframe = document?.QuerySelector("div#Player iframe")?.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(nestedIframe)) return results;

            var iframeHtml = await HttpGet(FixUrl(nestedIframe, Config.MainUrl), referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (iframeHtml is null) return results;

            var cryptData = Regex.Match(iframeHtml, "CryptoJS\\.AES\\.decrypt\\(\"(.*)\",\"").Groups[1].Value;
            var cryptPass = Regex.Match(iframeHtml, "\",\"(.*)\"\\);").Groups[1].Value;
            var decoded = CryptoJSHelper.Decrypt(cryptPass, cryptData);
            if (string.IsNullOrWhiteSpace(decoded)) return results;

            var file = Regex.Match(decoded, "file:\\s*'(.*)',").Groups[1].Value;
            results.Add(string.IsNullOrWhiteSpace(file) ? decoded : file);
        }
        else if (iframeLink.Contains("/player/moly/moly.php", StringComparison.OrdinalIgnoreCase))
        {
            iframeLink = iframeLink.Replace("moly.php?h=", "moly.php?wmode=opaque&h=", StringComparison.OrdinalIgnoreCase);
            var html = await HttpGet(iframeLink, referer: referer, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
            if (html is null) return results;

            var escaped = Regex.Match(html, "unescape\\(\"(.*)\"\\)").Groups[1].Value;
            if (string.IsNullOrWhiteSpace(escaped)) return results;

            var decodedHtml = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(escaped)));
            using var document = await HtmlParse(decodedHtml);
            var nestedIframe = document?.QuerySelector("div#Player iframe")?.GetAttribute("src");
            if (!string.IsNullOrWhiteSpace(nestedIframe))
                results.Add(FixUrl(nestedIframe, Config.MainUrl));
        }
        else if (iframeLink.Contains("/player/debx", StringComparison.OrdinalIgnoreCase))
        {
            results.AddRange(await DecodeGenericPlayerPageAsync(iframeLink, referer));
        }
        else if (iframeLink.Contains("/player/haydi.php", StringComparison.OrdinalIgnoreCase))
        {
            var encoded = iframeLink.Split("?v=").LastOrDefault();
            if (!string.IsNullOrWhiteSpace(encoded))
                results.Add(Encoding.UTF8.GetString(Convert.FromBase64String(NormalizeBase64(encoded))));
        }
        else
        {
            results.Add(iframeLink);
        }

        return results;
    }

    private async Task<List<string>> DecodeGenericPlayerPageAsync(string playerUrl, string referer)
    {
        var results = new List<string>();
        var html = await HttpGet(playerUrl, referer: referer, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144);
        if (string.IsNullOrWhiteSpace(html)) return results;

        var decodedHtml = TryDecodeEmbeddedBase64Html(html);
        if (!string.IsNullOrWhiteSpace(decodedHtml))
            html = decodedHtml;

        using var document = await HtmlParse(html);
        var nestedIframe = document?.QuerySelector("div#Player iframe")?.GetAttribute("src")
            ?? document?.QuerySelector("iframe[src]")?.GetAttribute("src")
            ?? document?.QuerySelector("iframe[data-src]")?.GetAttribute("data-src");

        if (!string.IsNullOrWhiteSpace(nestedIframe))
            results.Add(FixUrl(nestedIframe, Config.MainUrl));

        var direct = ExtractDirectMediaUrl(html);
        if (!string.IsNullOrWhiteSpace(direct))
            results.Add(FixUrl(direct, Config.MainUrl));

        return results
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<VideoSourceModel?> ExtractFirstAsync(List<string> urls, string name, string referer, string? forcedExtractorName = null)
    {
        foreach (var url in urls.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            var source = new VideoSourceModel { Name = name, Url = FixUrl(url, Config.MainUrl), Referer = referer };
            var extractor = string.IsNullOrWhiteSpace(forcedExtractorName)
                ? null
                : ExtractorManager.GetExtractorByName(forcedExtractorName);
            var extracted = extractor is null
                ? await ExtractAsync(source, referer)
                : await extractor.ExtractAsync(source, referer);
            extracted ??= await ExtractAsync(source, referer);

            if (extracted is not null)
            {
                if (string.IsNullOrWhiteSpace(extracted.Name)) extracted.Name = name;
                return extracted;
            }

            return source;
        }

        return null;
    }

    private static string? GetForcedExtractorName(string? sourceName, string? iframeLink)
    {
        if ((sourceName?.Contains("moly", StringComparison.OrdinalIgnoreCase) == true
             || iframeLink?.Contains("/player/moly/", StringComparison.OrdinalIgnoreCase) == true))
        {
            return "Molystream";
        }

        return null;
    }

    private static string? TryDecodeEmbeddedBase64Html(string html)
    {
        var escaped = Regex.Match(html, "unescape\\([\"'](?<value>.*?)[\"']\\)", RegexOptions.Singleline).Groups["value"].Value;
        var encoded = string.IsNullOrWhiteSpace(escaped)
            ? Regex.Match(html, "atob\\([\"'](?<value>[A-Za-z0-9+/=_-]+)[\"']\\)", RegexOptions.Singleline).Groups["value"].Value
            : Uri.UnescapeDataString(escaped);

        if (string.IsNullOrWhiteSpace(encoded)) return null;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(NormalizeBase64(encoded)));
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractDirectMediaUrl(string html)
    {
        var jwResult = JwPlayerHelper.ExtractStreamLinks(html, "DiziBox", "https://www.dizibox.live");
        var jwSource = jwResult.VideoSources.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(jwSource?.Url)) return jwSource.Url;

        var direct = Regex.Match(
            html,
            @"https?://[^\s""'<>]+?(?:\.m3u8|\.mp4|master\.txt)[^\s""'<>]*",
            RegexOptions.IgnoreCase).Value;

        return string.IsNullOrWhiteSpace(direct)
            ? null
            : direct.Replace("\\/", "/", StringComparison.Ordinal);
    }

    private static (int Season, int Episode) ExtractSeasonEpisode(string value)
    {
        var match = Regex.Match(value, @"(\d+)\.?\s*sezon.*?(\d+)\.?\s*b[oö]l[uü]m", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(value, @"(\d+)-sezon-(\d+)-bolum", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(value, @"(\d+)x(\d+)", RegexOptions.IgnoreCase);

        return match.Success
            ? (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))
            : (0, 0);
    }

    private static string? JoinTexts(IEnumerable<IElement> elements)
    {
        var values = elements
            .Select(item => item.TextContent.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count > 0 ? string.Join(", ", values) : null;
    }

    private Dictionary<string, string> GetHeaders() => new()
    {
        ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
        ["User-Agent"] = UserAgent,
        ["Cookie"] = $"isTrustedUser=true; dbxu={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        ["Referer"] = $"{Config.MainUrl}/",
    };

    private static string NormalizeBase64(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        return normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
    }
}
