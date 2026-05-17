using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Plugins;

public class DiziPal : PluginBase
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36";

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.dizipal",
        Name = "DiziPal",
        Version = "1.0.0",
        Description = "DiziPal uzerinden dizi, film, bolum ve metadata kesfi saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://dizipal.im",
        Favicon = "https://www.google.com/s2/favicons?domain=dizipal.im&sz=64",
        Language = "tr"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        var mainUrl = Config.MainUrl.TrimEnd('/');

        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Aile", Url = $"{mainUrl}/kategori/aile/page/" },
            new() { Title = "Aksiyon", Url = $"{mainUrl}/kategori/aksiyon/page/" },
            new() { Title = "Animasyon", Url = $"{mainUrl}/kategori/animasyon/page/" },
            new() { Title = "Belgesel", Url = $"{mainUrl}/kategori/belgesel/page/" },
            new() { Title = "Bilim Kurgu", Url = $"{mainUrl}/kategori/bilim-kurgu/page/" },
            new() { Title = "Dram", Url = $"{mainUrl}/kategori/dram/page/" },
            new() { Title = "Fantastik", Url = $"{mainUrl}/kategori/fantastik/page/" },
            new() { Title = "Gerilim", Url = $"{mainUrl}/kategori/gerilim/page/" },
            new() { Title = "Gizem", Url = $"{mainUrl}/kategori/gizem/page/" },
            new() { Title = "Komedi", Url = $"{mainUrl}/kategori/komedi/page/" },
            new() { Title = "Korku", Url = $"{mainUrl}/kategori/korku/page/" },
            new() { Title = "Macera", Url = $"{mainUrl}/kategori/macera/page/" },
            new() { Title = "Muzik", Url = $"{mainUrl}/kategori/muzik/page/" },
            new() { Title = "Romantik", Url = $"{mainUrl}/kategori/romantik/page/" },
            new() { Title = "Savas", Url = $"{mainUrl}/kategori/savas/page/" },
            new() { Title = "Suc", Url = $"{mainUrl}/kategori/suc/page/" },
            new() { Title = "Tarih", Url = $"{mainUrl}/kategori/tarih/page/" },
            new() { Title = "Vahsi Bati", Url = $"{mainUrl}/kategori/vahsi-bati/page/" },
            new() { Title = "Yerli", Url = $"{mainUrl}/kategori/yerli/page/" }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var targetUrl = category.Url.EndsWith("/page/", StringComparison.OrdinalIgnoreCase)
                ? $"{category.Url}{pageNumber}/"
                : category.Url;

            var html = await HttpGet(targetUrl, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
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
            return await SearchByQueryPage(query);
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
            var html = await HttpGet(pageItem.Url, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var isSeries = pageItem.Url.Contains("/dizi/", StringComparison.OrdinalIgnoreCase);
            var title = document.QuerySelector("main h1")?.TextContent?.Trim()
                ?? document.QuerySelector("body h1")?.TextContent?.Trim()
                ?? document.QuerySelector("meta[property='og:title']")?.GetAttribute("content")?.Trim();

            var poster = document.QuerySelector("meta[property='og:image']")?.GetAttribute("content");
            var description = document.QuerySelector("meta[property='og:description']")?.GetAttribute("content")?.Trim()
                ?? document.QuerySelector("meta[name='description']")?.GetAttribute("content")?.Trim();
            var year = Regex.Match(GetDetailValue(document, "Yapım Yılı") ?? "", @"\d{4}").Value;
            var rating = GetDetailValue(document, "IMDB Puanı");
            var duration = ParseDuration(GetDetailValue(document, "Süre"));
            var tags = GetMetaList(document, "Tür");
            var actors = GetMetaList(document, "Oyuncular");
            if (string.IsNullOrWhiteSpace(actors))
                actors = string.Join(", ", document.QuerySelectorAll("div.swiper-slide a[title]")
                    .Select(item => item.GetAttribute("title")?.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            var episodes = isSeries ? await LoadEpisodesAsync(document) : null;
            var videoSources = CreateVideoSources(document, pageItem.Url, !isSeries || pageItem.Url.Contains("/bolum/", StringComparison.OrdinalIgnoreCase));

            return new MediaInfoModel
            {
                Title = CleanTitle(string.IsNullOrWhiteSpace(title) ? pageItem.Title : title),
                Url = pageItem.Url,
                Poster = string.IsNullOrWhiteSpace(poster) ? pageItem.Poster : FixUrl(poster, Config.MainUrl),
                Backdrop = string.IsNullOrWhiteSpace(poster) ? pageItem.Poster : FixUrl(poster, Config.MainUrl),
                Description = description,
                Tags = tags,
                Actors = actors,
                Rating = rating,
                Year = string.IsNullOrWhiteSpace(year) ? null : year,
                Duration = duration,
                Episodes = episodes?.Count > 0 ? episodes : null,
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
            var pageUrl = videoSource.Url;
            var html = await HttpGet(pageUrl, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
            if (html is null) return await ExtractAsync(videoSource, Config.MainUrl);

            using var document = await HtmlParse(html);
            if (document is null) return await ExtractAsync(videoSource, Config.MainUrl);

            var iframe = document.QuerySelector(".series-player-container iframe")?.GetAttribute("src")
                ?? document.QuerySelector("div#vast_new iframe")?.GetAttribute("src")
                ?? document.QuerySelector("div.video-player-area iframe")?.GetAttribute("src")
                ?? document.QuerySelector("div.responsive-player iframe")?.GetAttribute("src")
                ?? document.QuerySelector("iframe")?.GetAttribute("src");

            if (string.IsNullOrWhiteSpace(iframe))
                return await ExtractAsync(videoSource, Config.MainUrl);

            iframe = FixUrl(iframe, Config.MainUrl);
            var iframeHtml = await HttpGet(iframe, referer: $"{Config.MainUrl.TrimEnd('/')}/", headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
            if (iframeHtml is null)
                return await ExtractAsync(new VideoSourceModel { Name = videoSource.Name, Url = iframe, Referer = Config.MainUrl }, Config.MainUrl);

            var m3u8 = Regex.Match(iframeHtml, "file:\\s*\"([^\"]+)\"").Groups[1].Value;
            if (string.IsNullOrWhiteSpace(m3u8))
                return await ExtractAsync(new VideoSourceModel { Name = videoSource.Name, Url = iframe, Referer = Config.MainUrl }, Config.MainUrl);

            return new VideoSourceModel
            {
                Name = string.IsNullOrWhiteSpace(videoSource.Name) ? "DiziPal" : videoSource.Name,
                Url = FixUrl(m3u8, Config.MainUrl),
                Referer = $"{Config.MainUrl.TrimEnd('/')}/",
                Headers =
                [
                    new HeaderModel { Name = "Referer", Value = $"{Config.MainUrl.TrimEnd('/')}/" },
                    new HeaderModel { Name = "User-Agent", Value = UserAgent }
                ],
                Subtitles = ParseSubtitles(iframeHtml)
            };
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private List<PageItemModel> ParseLatestEpisodes(IParentNode document, string categoryName)
    {
        var results = new List<PageItemModel>();

        foreach (var item in document.QuerySelectorAll("div.episode-item"))
        {
            var name = item.QuerySelector("div.name")?.TextContent?.Trim();
            var episode = item.QuerySelector("div.episode")?.TextContent?.Trim();
            var href = item.QuerySelector("a")?.GetAttribute("href");
            var poster = item.QuerySelector("img")?.GetAttribute("src");

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

            var title = string.IsNullOrWhiteSpace(episode)
                ? name
                : $"{name} {episode.Replace(". Sezon ", "x").Replace(". Bölüm", "")}";

            results.Add(new PageItemModel
            {
                Title = title,
                Url = FixUrl(href.Split("/sezon", StringSplitOptions.None)[0], Config.MainUrl),
                Poster = string.IsNullOrWhiteSpace(poster) ? null : FixUrl(poster, Config.MainUrl),
                CategoryName = categoryName
            });
        }

        return results;
    }

    private List<PageItemModel> ParseCards(IParentNode document, string categoryName)
    {
        var results = new List<PageItemModel>();

        foreach (var item in document.QuerySelectorAll("div.grid div.post-item, div.post-item"))
        {
            var href = item.QuerySelector("a")?.GetAttribute("href");
            var title = item.QuerySelector("a")?.GetAttribute("title")?.Trim()
                ?? item.QuerySelector("img")?.GetAttribute("alt")?.Trim();
            var poster = item.QuerySelector("div.poster img")?.GetAttribute("data-src")
                ?? item.QuerySelector("div.poster img")?.GetAttribute("src")
                ?? item.QuerySelector("img")?.GetAttribute("data-src")
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

    private List<EpisodeModel> ParseEpisodes(IParentNode document)
    {
        var episodes = new List<EpisodeModel>();

        foreach (var item in document.QuerySelectorAll("div.episode-item"))
        {
            var href = item.QuerySelector("a")?.GetAttribute("href");
            var titleAttr = item.QuerySelector("a")?.GetAttribute("title") ?? "";
            var name = item.QuerySelector("h4 a")?.TextContent?.Trim()
                ?? item.QuerySelector("img")?.GetAttribute("alt")?.Trim();
            var episodeText = titleAttr;

            if (string.IsNullOrWhiteSpace(href)) continue;

            var season = Regex.Match(episodeText, @"(\d+)\.\s*Sezon", RegexOptions.IgnoreCase);
            var episode = Regex.Match(episodeText, @"(\d+)\.\s*Bölüm", RegexOptions.IgnoreCase);

            episodes.Add(new EpisodeModel
            {
                Title = name,
                Url = FixUrl(href, Config.MainUrl),
                SeasonNumber = season.Success ? int.Parse(season.Groups[1].Value) : 0,
                EpisodeNumber = episode.Success ? int.Parse(episode.Groups[1].Value) : 0
            });
        }

        return episodes
            .OrderBy(item => item.SeasonNumber)
            .ThenBy(item => item.EpisodeNumber)
            .ToList();
    }

    private List<VideoSourceModel> CreateVideoSources(IParentNode document, string pageUrl, bool includePlayableSource)
    {
        var sources = new List<VideoSourceModel>();
        var trailer = FindTrailerUrl(document);

        if (!string.IsNullOrWhiteSpace(trailer))
        {
            sources.Add(new VideoSourceModel
            {
                Name = "Fragman",
                Url = FixUrl(trailer, Config.MainUrl),
                Referer = Config.MainUrl,
                IsTrailer = true
            });
        }

        if (includePlayableSource)
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

    private string? FindTrailerUrl(IParentNode document)
    {
        var trailerLink = document.QuerySelector("a[href*='youtube'], a[href*='youtu.be'], a[href*='fragman'], a[data-video_url]")
            ?.GetAttribute("data-video_url")
            ?? document.QuerySelector("a[href*='youtube'], a[href*='youtu.be'], a[href*='fragman']")?.GetAttribute("href");

        return string.IsNullOrWhiteSpace(trailerLink) ? null : trailerLink;
    }

    private List<SubtitleModel>? ParseSubtitles(string iframeHtml)
    {
        var subtitlesRaw = Regex.Match(iframeHtml, "\"subtitle\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
        if (string.IsNullOrWhiteSpace(subtitlesRaw)) return null;

        var subtitles = new List<SubtitleModel>();

        foreach (var item in subtitlesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var lang = item.Contains('[') ? item[(item.IndexOf('[') + 1)..item.IndexOf(']')] : "Subtitle";
            var url = Regex.Replace(item, @"\[[^\]]+\]", "").Trim();
            if (string.IsNullOrWhiteSpace(url)) continue;

            subtitles.Add(new SubtitleModel
            {
                Name = lang,
                Url = FixUrl(url, Config.MainUrl)
            });
        }

        return subtitles.Count > 0 ? subtitles : null;
    }

    private async Task<List<EpisodeModel>> LoadEpisodesAsync(IParentNode document)
    {
        var seasonLinks = document.QuerySelectorAll("#season-options-list ul li a[href]")
            .Select(item => FixUrl(item.GetAttribute("href") ?? "", Config.MainUrl))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (seasonLinks.Count == 0)
            return ParseEpisodes(document);

        var episodes = new Dictionary<(int Season, int Episode), EpisodeModel>();

        foreach (var seasonLink in seasonLinks)
        {
            var html = await HttpGet(seasonLink, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
            if (html is null) continue;

            using var seasonDocument = await HtmlParse(html);
            if (seasonDocument is null) continue;

            foreach (var episode in ParseEpisodes(seasonDocument))
                episodes[(episode.SeasonNumber, episode.EpisodeNumber)] = episode;
        }

        return episodes.Values
            .OrderBy(item => item.SeasonNumber)
            .ThenBy(item => item.EpisodeNumber)
            .ToList();
    }

    private string? GetDetailValue(IParentNode document, string label)
    {
        var labelNode = document.QuerySelectorAll("span")
            .FirstOrDefault(item => string.Equals(item.TextContent?.Trim(), label, StringComparison.OrdinalIgnoreCase));

        return labelNode?.ParentElement?.TextContent?.Replace(label, "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    private string? GetMetaList(IParentNode document, string label)
    {
        var labelNode = document.QuerySelectorAll("span")
            .FirstOrDefault(item => string.Equals(item.TextContent?.Trim(), label, StringComparison.OrdinalIgnoreCase));

        var values = labelNode?.ParentElement?.QuerySelectorAll("a")
            .Select(item => item.TextContent.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values is { Count: > 0 } ? string.Join(", ", values) : null;
    }

    private static string? ParseDuration(string? durationRaw)
    {
        if (string.IsNullOrWhiteSpace(durationRaw)) return null;

        var hours = Regex.Match(durationRaw, @"(\d+)\s*s", RegexOptions.IgnoreCase);
        var minutes = Regex.Match(durationRaw, @"(\d+)\s*dk", RegexOptions.IgnoreCase);

        var total = 0;
        if (hours.Success) total += int.Parse(hours.Groups[1].Value) * 60;
        if (minutes.Success) total += int.Parse(minutes.Groups[1].Value);

        if (total > 0) return total.ToString();

        var plain = Regex.Match(durationRaw, @"\d+").Value;
        return string.IsNullOrWhiteSpace(plain) ? null : plain;
    }

    private async Task<List<PageItemModel>?> SearchByQueryPage(string query)
    {
        var mainUrl = Config.MainUrl.TrimEnd('/');
        var html = await HttpGet($"{mainUrl}/?s={Uri.EscapeDataString(query)}", referer: mainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
        if (html is null) return null;

        using var document = await HtmlParse(html);
        if (document is null) return null;

        var results = ParseCards(document, "Arama");
        return results.Count > 0 ? results : null;
    }

    private static string CleanTitle(string value) =>
        value.Replace(" izle - Dizipal", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" - Dizipal", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private Dictionary<string, string> GetHeaders() => new()
    {
        ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
        ["User-Agent"] = UserAgent
    };
}
