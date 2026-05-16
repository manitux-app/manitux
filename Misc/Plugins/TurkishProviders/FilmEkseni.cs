using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Extractors.Helpers;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Plugins;

public class FilmEkseni : PluginBase
{
    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.filmekseni",
        Name = "FilmEkseni",
        Version = "1.0.0",
        Description = "FilmEkseni uzerinden katalog, arama ve metadata kesfi saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://filmekseni.cc",
        Favicon = "https://www.google.com/s2/favicons?domain=filmekseni.cc&sz=64",
        Language = "tr"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        var mainUrl = Config.MainUrl.TrimEnd('/');

        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Anasayfa", Url = $"{mainUrl}/" },
            new() { Title = "Kesfet", Url = $"{mainUrl}/kesfet/" },
            new() { Title = "Diziler", Url = $"{mainUrl}/diziler/" },
            new() { Title = "En Cok Izlenenler", Url = $"{mainUrl}/en-cok-izlenenler/" },
            new() { Title = "IMDb 250", Url = $"{mainUrl}/imdb-250/" },
            new() { Title = "Tavsiye Filmler", Url = $"{mainUrl}/kategori/tavsiye-filmler" },
            new() { Title = "Aile", Url = $"{mainUrl}/tur/aile-filmleri/" },
            new() { Title = "Aksiyon", Url = $"{mainUrl}/tur/aksiyon-filmleri/" },
            new() { Title = "Animasyon", Url = $"{mainUrl}/tur/animasyon-film-izle/" },
            new() { Title = "Belgesel", Url = $"{mainUrl}/tur/belgesel-filmleri/" },
            new() { Title = "Bilim Kurgu", Url = $"{mainUrl}/tur/bilim-kurgu-filmleri/" },
            new() { Title = "Biyografi", Url = $"{mainUrl}/tur/biyografi-filmleri/" },
            new() { Title = "Dram", Url = $"{mainUrl}/tur/dram-filmleri-izle/" },
            new() { Title = "Fantastik", Url = $"{mainUrl}/tur/fantastik-filmler/" },
            new() { Title = "Gerilim", Url = $"{mainUrl}/tur/gerilim-filmleri/" },
            new() { Title = "Gizem", Url = $"{mainUrl}/tur/gizem-filmleri/" },
            new() { Title = "Komedi", Url = $"{mainUrl}/tur/komedi-filmleri/" },
            new() { Title = "Korku", Url = $"{mainUrl}/tur/korku-filmleri/" },
            new() { Title = "Macera", Url = $"{mainUrl}/tur/macera-filmleri/" },
            new() { Title = "Muzik", Url = $"{mainUrl}/tur/muzik-filmleri/" },
            new() { Title = "Muzikal", Url = $"{mainUrl}/tur/muzikal/" },
            new() { Title = "Romantik", Url = $"{mainUrl}/tur/romantik-filmler/" },
            new() { Title = "Savas", Url = $"{mainUrl}/tur/savas-filmleri/" },
            new() { Title = "Spor", Url = $"{mainUrl}/tur/spor-filmleri/" },
            new() { Title = "Suc", Url = $"{mainUrl}/tur/suc-filmleri/" },
            new() { Title = "Tarih", Url = $"{mainUrl}/tur/tarih-filmleri/" },
            new() { Title = "Western", Url = $"{mainUrl}/tur/western-filmler/" }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var targetUrl = BuildPageUrl(category.Url, pageNumber);
            var html = await HttpGet(targetUrl, referer: category.Url, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var results = ParsePosterItems(document, category.Title);
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
            var html = await HttpGet(pageItem.Url, referer: Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var movie = ParseMovieJsonLd(document);
            var title = CleanTitle(GetFirstText(document, "div.page-title h1", "h1")) ?? movie.Title ?? pageItem.Title;
            var poster = movie.Image ?? GetPoster(document.DocumentElement) ?? pageItem.Poster;
            var tags = GetTags(document);
            var rating = FormatRating(movie.Rating)
                         ?? FormatRating(GetFirstText(document, "div.rate"))
                         ?? FormatRating(pageItem.Rating);
            var duration = movie.Duration ?? ExtractNumber(GetFirstText(document, "div.d-flex.flex-column.text-nowrap"));
            var actors = movie.Actors ?? GetActors(document);
            var year = movie.Year ?? ExtractYear(GetFirstText(document, "div.page-title strong a")) ?? pageItem.Year;
            var related = ParsePosterItems(document, "Related")
                .Where(x => !string.Equals(x.Url, pageItem.Url, StringComparison.OrdinalIgnoreCase))
                .Take(12)
                .Select(x => new RelatedVideoModel { Title = x.Title, Url = x.Url, Poster = x.Poster })
                .ToList();

            var videoSources = new List<VideoSourceModel>();
            var trailer = movie.TrailerUrl ?? GetTrailerUrl(document);
            if (!string.IsNullOrWhiteSpace(trailer))
            {
                videoSources.Add(new VideoSourceModel
                {
                    Name = "Fragman",
                    Url = trailer,
                    IsTrailer = true
                });
            }

            videoSources.AddRange(ParseVideoSources(document, pageItem.Url));

            return new MediaInfoModel
            {
                Title = title,
                Url = pageItem.Url,
                Description = movie.Description ?? GetDescription(document),
                Poster = poster,
                Backdrop = GetBackdrop(document) ?? poster,
                Trailer = trailer,
                Tags = tags,
                Rating = rating,
                Year = year,
                Duration = duration,
                Actors = actors,
                Country = movie.Country,
                VideoSources = videoSources.Count > 0 ? videoSources : null,
                RelatedVideos = related.Count > 0 ? related : null
            };
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
            if (string.IsNullOrWhiteSpace(query)) return null;

            var headers = GetHeaders();
            headers["X-Requested-With"] = "XMLHttpRequest";
            headers["Origin"] = Config.MainUrl.TrimEnd('/');

            var json = await HttpPost(
                $"{Config.MainUrl.TrimEnd('/')}/search/",
                new Dictionary<string, string> { ["query"] = query },
                referer: $"{Config.MainUrl.TrimEnd('/')}/",
                headers: headers);

            if (string.IsNullOrWhiteSpace(json)) return null;

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("result", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var items = results
                .EnumerateArray()
                .Select(ToPageItem)
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            return items.Count > 0 ? items : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        if (videoSource.IsTrailer) return videoSource;

        try
        {
            return await ResolveSourceLink(videoSource);
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    private List<PageItemModel> ParsePosterItems(IDocument document, string categoryName)
    {
        return document
            .QuerySelectorAll(".poster-container .poster > a[href]")
            .Select(x => ToPageItem(x, categoryName))
            .Where(x => x is not null)
            .Select(x => x!)
            .GroupBy(x => x.Url)
            .Select(x => x.First())
            .ToList();
    }

    private PageItemModel? ToPageItem(IElement anchor, string categoryName)
    {
        var href = anchor.GetAttribute("href");
        var title = CleanTitle(anchor.QuerySelector(".poster-title .title")?.TextContent
                               ?? anchor.GetAttribute("title")
                               ?? anchor.QuerySelector("img")?.GetAttribute("alt"));

        if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(title)) return null;

        return new PageItemModel
        {
            CategoryName = categoryName,
            Title = title,
            Url = FixUrl(href, Config.MainUrl),
            Poster = GetPoster(anchor),
            Year = CleanString(anchor.QuerySelector(".poster-year")?.TextContent ?? ""),
            Rating = FormatRating(ExtractRating(anchor.QuerySelector(".poster-imdb")?.TextContent))
        };
    }

    private PageItemModel? ToPageItem(JsonElement item)
    {
        var title = GetString(item, "title");
        var slug = GetString(item, "slug");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(slug)) return null;

        var slugPrefix = GetString(item, "slug_prefix") ?? string.Empty;
        var poster = GetString(item, "poster") ?? GetString(item, "cover");

        return new PageItemModel
        {
            CategoryName = "Search",
            Title = title,
            Url = $"{Config.MainUrl.TrimEnd('/')}/{slugPrefix}{slug}".TrimEnd('/') + "/",
            Poster = string.IsNullOrWhiteSpace(poster) ? null : $"{Config.MainUrl.TrimEnd('/')}/uploads/poster/{poster}",
            Year = GetString(item, "year"),
            Rating = FormatRating(GetString(item, "imdb"))
        };
    }

    private MovieJsonLd ParseMovieJsonLd(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var json = script.TextContent?.Trim();
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"@type\":\"Movie\"", StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                using var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;
                var trailer = root.TryGetProperty("trailer", out var trailerElement) ? GetString(trailerElement, "embedUrl") : null;
                var rating = root.TryGetProperty("aggregateRating", out var ratingElement) ? GetString(ratingElement, "ratingValue") : null;
                var actors = root.TryGetProperty("actor", out var actorElement) && actorElement.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", actorElement.EnumerateArray().Select(x => GetString(x, "name")).Where(x => !string.IsNullOrWhiteSpace(x)))
                    : null;

                return new MovieJsonLd
                {
                    Title = GetString(root, "name"),
                    Image = GetString(root, "image"),
                    Description = GetString(root, "description"),
                    Duration = ParseDuration(GetString(root, "duration")),
                    Country = root.TryGetProperty("countryOfOrigin", out var countryElement) ? GetString(countryElement, "name") : null,
                    TrailerUrl = NormalizeYouTubeUrl(trailer),
                    Rating = FormatRating(rating),
                    Actors = actors,
                    Year = ExtractYear(GetString(root, "datePublished"))
                };
            }
            catch (Exception ex)
            {
                Log(LogLevel.Warning, ex.ToString());
            }
        }

        return new MovieJsonLd();
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
        };
    }

    private List<VideoSourceModel> ParseVideoSources(IDocument document, string pageUrl)
    {
        var sources = ParseTabbedVideoSources(document, pageUrl);
        if (sources.Count == 0)
        {
            sources = ParseCardNavVideoSources(document, pageUrl);
        }

        if (sources.Count == 0 && document.QuerySelector("div.card-video iframe") is not null)
        {
            sources.Add(new VideoSourceModel
            {
                Name = "VIP",
                Url = pageUrl,
                Referer = pageUrl
            });
        }

        return sources
            .GroupBy(x => $"{x.Name}|{x.Url}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private List<VideoSourceModel> ParseTabbedVideoSources(IDocument document, string pageUrl)
    {
        var langTabs = document
            .QuerySelectorAll("ul.nav-tabs.nav-slider a.nav-link")
            .Where(x => !ContainsAny(x.TextContent, "fragman"))
            .ToList();

        var tabPanes = document.QuerySelectorAll("div.tab-pane").ToList();
        if (langTabs.Count == 0 || tabPanes.Count == 0) return new List<VideoSourceModel>();

        var sources = new List<VideoSourceModel>();
        for (var i = 0; i < tabPanes.Count && i < langTabs.Count; i++)
        {
            var langName = CleanString(langTabs[i].TextContent);
            foreach (var link in tabPanes[i].QuerySelectorAll("a.nav-link[href]"))
            {
                var playerName = CleanString(link.TextContent);
                var href = link.GetAttribute("href");
                if (!IsValidSourceLink(playerName, href)) continue;

                sources.Add(new VideoSourceModel
                {
                    Name = string.IsNullOrWhiteSpace(langName) ? playerName : $"{playerName} | {langName}",
                    Url = FixUrl(href!, Config.MainUrl),
                    Referer = pageUrl
                });
            }
        }

        return sources;
    }

    private List<VideoSourceModel> ParseCardNavVideoSources(IDocument document, string pageUrl)
    {
        var sources = new List<VideoSourceModel>();
        foreach (var link in document.QuerySelectorAll("nav.card-nav a.nav-link[href]"))
        {
            var name = CleanString(link.TextContent);
            var href = link.GetAttribute("href");
            if (!IsValidSourceLink(name, href)) continue;

            sources.Add(new VideoSourceModel
            {
                Name = name,
                Url = FixUrl(href!, Config.MainUrl),
                Referer = pageUrl
            });
        }

        return sources;
    }

    private async Task<VideoSourceModel?> ResolveSourceLink(VideoSourceModel source)
    {
        var pageUrl = FixUrl(source.Url, Config.MainUrl);
        var html = await HttpGet(pageUrl, referer: source.Referer ?? Config.MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Cloudscraper);
        if (html is null) return null;

        using var document = await HtmlParse(html);
        if (document is null) return null;

        var iframeUrl = GetIframeUrl(document);
        if (string.IsNullOrWhiteSpace(iframeUrl)) return null;

        var headers = new List<HeaderModel>
            {
               new() { Name = "User-Agent", Value= "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"},
               new() { Name = "upgrade-insecure-requests", Value = "1"}
            };

        iframeUrl = FixUrl(iframeUrl, Config.MainUrl);
        if (IsEksenLoadUrl(iframeUrl) || string.Equals(source.Name, "VIP", StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveEksenLoadSource(source, iframeUrl, pageUrl);
        }

        return await ExtractAsync(new VideoSourceModel
        {
            Name = source.Name,
            Url = iframeUrl,
            Referer = Config.MainUrl + "/",
            Headers = headers,
        }, Config.MainUrl);
    }

    private async Task<VideoSourceModel?> ResolveEksenLoadSource(VideoSourceModel source, string iframeUrl, string pageUrl)
    {
        var iframeBaseUrl = GetBaseUrl(iframeUrl);
        var iframeHeaders = GetHeaders();
        iframeHeaders["Origin"] = Config.MainUrl.TrimEnd('/');

        var iframeHtml = await HttpGet(iframeUrl, referer: pageUrl, headers: iframeHeaders, identifier: TlsClientIdentifier.Cloudscraper);
        if (string.IsNullOrWhiteSpace(iframeHtml)) return null;

        var playbackHeaders = new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            ["Origin"] = iframeBaseUrl
        };

        var jwPlayerResult = JwPlayerHelper.ExtractStreamLinks(iframeHtml, source.Name, iframeBaseUrl, playbackHeaders);
        var resolvedSource = jwPlayerResult.VideoSources.FirstOrDefault();
        if (resolvedSource is null)
        {
            var videoId = iframeUrl.TrimEnd('/').Split('/').LastOrDefault();
            if (string.IsNullOrWhiteSpace(videoId)) return null;

            resolvedSource = new VideoSourceModel
            {
                Name = source.Name,
                Url = $"{iframeBaseUrl}/uploads/encode/{videoId}/master.m3u8",
                Headers = playbackHeaders
                    .Select(x => new HeaderModel { Name = x.Key, Value = x.Value })
                    .ToList()
            };
        }

        resolvedSource.Name = source.Name;
        resolvedSource.Referer = iframeUrl;
        resolvedSource.Subtitles = jwPlayerResult.Subtitles.Count > 0 ? jwPlayerResult.Subtitles : null;
        return resolvedSource;
    }

    private static string? GetIframeUrl(IDocument document)
    {
        var iframe = document.QuerySelector("div.card-video iframe");
        return iframe?.GetAttribute("data-src") ?? iframe?.GetAttribute("src");
    }

    private static bool IsEksenLoadUrl(string url)
    {
        return url.Contains("eksenload", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseUrl(string url)
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}";
    }

    private static bool IsValidSourceLink(string name, string? href)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href) || href == "#") return false;
        return !ContainsAny(name, "paylas", "paylaş", "indir", "hata", "sinema modu", "fragman");
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private string BuildPageUrl(string url, int pageNumber)
    {
        return pageNumber <= 1 ? url : $"{url.TrimEnd('/')}/page/{pageNumber}/";
    }

    private string? GetPoster(IElement root)
    {
        var poster = root.QuerySelector("picture.poster-auto img[data-src]")?.GetAttribute("data-src")
                     ?? root.QuerySelector("picture.poster-auto img")?.GetAttribute("src")
                     ?? root.QuerySelector("source[type='image/jpeg']")?.GetAttribute("data-srcset")
                     ?? root.QuerySelector("img[data-src]")?.GetAttribute("data-src")
                     ?? root.QuerySelector("img")?.GetAttribute("src");

        return string.IsNullOrWhiteSpace(poster) || poster.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)
            ? null
            : FixUrl(poster, Config.MainUrl);
    }

    private string? GetBackdrop(IDocument document)
    {
        var backdrop = document.QuerySelector(".play-that-video img")?.GetAttribute("src");
        return string.IsNullOrWhiteSpace(backdrop) ? null : FixUrl(backdrop, Config.MainUrl);
    }

    private string? GetDescription(IDocument document)
    {
        return CleanString(GetFirstText(document, "article.text-white p", "article p") ?? "");
    }

    private string GetTags(IDocument document)
    {
        return string.Join(", ", document
            .QuerySelectorAll("div.pb-2 a[href*='/tur/'], article a[href*='/tur/'], .breadcrumb a[href*='/tur/']")
            .Select(x => CleanString(x.TextContent).Replace("✅", "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct());
    }

    private string? GetActors(IDocument document)
    {
        var actors = document
            .QuerySelectorAll("div.card-body.p-0.pt-2 .story-item .story-item-title")
            .Select(x => CleanString(x.TextContent))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        return actors.Count > 0 ? string.Join(", ", actors) : null;
    }

    private string? GetTrailerUrl(IDocument document)
    {
        var key = document.QuerySelector("[data-trailer]")?.GetAttribute("data-trailer");
        return NormalizeYouTubeUrl(key);
    }

    private static string? NormalizeYouTubeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return value;
        return $"https://youtube.com/embed/{value.TrimStart('/')}";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : null;
    }

    private static string? ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = Regex.Match(value, @"PT(?<minutes>\d+)M", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["minutes"].Value : value;
    }

    private static string? GetFirstText(IDocument document, params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var value = document.QuerySelector(selector)?.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    private static string? ExtractNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Regex.Match(text, @"\d+");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractRating(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Regex.Match(text, @"\d+(?:[.,]\d+)?");
        return match.Success ? match.Value.Replace(',', '.') : null;
    }

    private static string? FormatRating(string? rating)
    {
        if (string.IsNullOrWhiteSpace(rating)) return null;

        if (!double.TryParse(rating.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return rating.Trim();
        }

        if (value <= 0) return string.Empty;

        return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? CleanTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return Regex.Replace(value, @"\s+izle\s*$", "", RegexOptions.IgnoreCase).Trim();
    }

    private static string? ExtractYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = Regex.Match(value, @"\b\d{4}\b");
        return match.Success ? match.Value : null;
    }

    private sealed class MovieJsonLd
    {
        public string? Title { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
        public string? Duration { get; set; }
        public string? Country { get; set; }
        public string? TrailerUrl { get; set; }
        public string? Rating { get; set; }
        public string? Actors { get; set; }
        public string? Year { get; set; }
    }
}
