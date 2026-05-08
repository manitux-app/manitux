using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;

namespace Manitux.Core.Plugins;

public class RareFilm : PluginBase
{
    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.rarefilm",
        Name = "RareFilm",
        Version = "1.0.0",
        Description = "The Cave of Forgotten Films",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://rarefilmm.com",
        Favicon = "https://www.google.com/s2/favicons?domain=rarefilmm.com&sz=64",
        Language = "en"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        var mainUrl = Config.MainUrl.TrimEnd('/');

        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Latest", Url = mainUrl },
            new() { Title = "Action", Url = $"{mainUrl}/category/action/" },
            new() { Title = "Adventure", Url = $"{mainUrl}/category/adventure/" },
            new() { Title = "Arthouse", Url = $"{mainUrl}/category/arthouse/" },
            new() { Title = "Biography", Url = $"{mainUrl}/category/biography/" },
            new() { Title = "Comedy", Url = $"{mainUrl}/category/comedy/" },
            new() { Title = "Crime", Url = $"{mainUrl}/category/crime/" },
            new() { Title = "Documentary", Url = $"{mainUrl}/category/documentary/" },
            new() { Title = "Drama", Url = $"{mainUrl}/category/drama/" },
            new() { Title = "Fantasy", Url = $"{mainUrl}/category/fantasy/" },
            new() { Title = "Horror", Url = $"{mainUrl}/category/horror/" },
            new() { Title = "Mystery", Url = $"{mainUrl}/category/mystery/" },
            new() { Title = "Romance", Url = $"{mainUrl}/category/romance/" },
            new() { Title = "Sci-Fi", Url = $"{mainUrl}/category/sci-fi/" },
            new() { Title = "Short", Url = $"{mainUrl}/category/short/" },
            new() { Title = "Thriller", Url = $"{mainUrl}/category/thriller/" },
            new() { Title = "TV Movie", Url = $"{mainUrl}/category/tv-movie/" }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var targetUrl = BuildPageUrl(category.Url, pageNumber);
            var html = await HttpGet(targetUrl, referer: category.Url, headers: GetHeaders());
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var results = ParsePostItems(document, category.Title);
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
            var html = await HttpGet(pageItem.Url, referer: Config.MainUrl, headers: GetHeaders());
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var content = document.QuerySelector(".entry-content article, .entry-content") ?? document.DocumentElement;
            var title = GetMetaContent(document, "og:title")
                        ?? CleanTitle(document.QuerySelector("h1.entry-title, h1")?.TextContent)
                        ?? pageItem.Title;
            var description = GetDescription(content) ?? GetMetaContent(document, "og:description");
            var poster = GetMetaContent(document, "og:image") ?? GetPoster(document.DocumentElement) ?? pageItem.Poster;
            var tags = string.Join(", ", document
                .QuerySelectorAll(".entry-categories a, .entry-tags a, meta[property='article:tag'], meta[property='article:section']")
                .Select(x => x.LocalName.Equals("meta", StringComparison.OrdinalIgnoreCase)
                    ? x.GetAttribute("content")
                    : CleanString(x.TextContent))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct());
            var categories = string.Join(", ", document
                .QuerySelectorAll(".entry-categories a")
                .Select(x => CleanString(x.TextContent))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct());
            var comments = document
                .QuerySelectorAll(".comment-list article.comment")
                .Select(ToComment)
                .Where(x => x is not null)
                .Select(x => x!)
                .Take(8)
                .ToList();
            var related = document
                .QuerySelectorAll("nav.further-reading a[href]")
                .Select(x => ToRelatedVideo(x, poster))
                .Where(x => x is not null)
                .Select(x => x!)
                .Where(x => !string.Equals(x.Url, Config.MainUrl, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new MediaInfoModel
            {
                Title = title,
                Url = pageItem.Url,
                Description = description,
                Poster = poster,
                Backdrop = poster,
                Tags = tags,
                Actors = ExtractLabeledValue(content, "Stars"),
                Country = ExtractCountry(tags),
                Year = ExtractYear(title) ?? pageItem.Year,
                Comments = comments.Count > 0 ? comments : null,
                RelatedVideos = related.Count > 0 ? related : null,
                VideoSources = null
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

            var searchUrl = $"{Config.MainUrl.TrimEnd('/')}/?s={Uri.EscapeDataString(query)}";
            var html = await HttpGet(searchUrl, referer: Config.MainUrl, headers: GetHeaders());
            if (html is null) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var results = ParsePostItems(document, "Search");
            return results.Count > 0 ? results : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return await Task.FromResult<VideoSourceModel?>(null);
    }

    private List<PageItemModel> ParsePostItems(IDocument document, string categoryName)
    {
        var results = document
            .QuerySelectorAll("div.excerpt")
            .Select(x => ToPageItem(x, categoryName))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        if (results.Count > 0) return results;

        return document
            .QuerySelectorAll("h2.excerpt-title a, h1.entry-title a")
            .Select(x => ToPageItem(x, categoryName))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }

    private PageItemModel? ToPageItem(IElement article, string categoryName)
    {
        var titleElement = article.QuerySelector("h2.excerpt-title a, h1.entry-title a, .entry-title a");
        if (titleElement is null && article.Matches("a")) titleElement = article;

        var title = CleanTitle(titleElement?.TextContent);
        var href = titleElement?.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href)) return null;

        var poster = GetPoster(article);
        var category = CleanString(article.QuerySelector(".excerpt-meta .category a")?.TextContent ?? "");

        return new PageItemModel
        {
            CategoryName = !string.IsNullOrWhiteSpace(category) ? category : categoryName,
            Title = title,
            Url = FixUrl(href, Config.MainUrl),
            Poster = poster,
            Year = ExtractYear(title)
        };
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
        };
    }

    private string BuildPageUrl(string url, int pageNumber)
    {
        if (pageNumber <= 1) return url;

        return $"{url.TrimEnd('/')}/page/{pageNumber}/";
    }

    private string? GetDescription(IElement article)
    {
        var paragraphs = article
            .QuerySelectorAll("p")
            .Where(x => x.QuerySelector(".more-link, a[href*='gofile.io'], a[href*='1fichier.com']") is null)
            .Select(x => CleanString(x.TextContent))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !x.StartsWith("WATCH HERE", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.StartsWith("DL via", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Contains("DO NOT USE", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.StartsWith("How would you rate", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return paragraphs.Count > 0 ? string.Join(Environment.NewLine, paragraphs) : null;
    }

    private string? GetPoster(IElement article)
    {
        var featured = article.QuerySelector(".featured-image");
        var style = featured?.GetAttribute("style");
        var background = ExtractBackgroundImage(style);
        if (!string.IsNullOrWhiteSpace(background)) return FixUrl(background, Config.MainUrl);

        var img = article.QuerySelector("img");
        var poster = img?.GetAttribute("data-src")
                     ?? img?.GetAttribute("data-lazy-src")
                     ?? img?.GetAttribute("src");

        return string.IsNullOrWhiteSpace(poster) ? null : FixUrl(poster, Config.MainUrl);
    }

    private string? GetMetaContent(IDocument document, string property)
    {
        var value = document
            .QuerySelector($"meta[property='{property}'], meta[name='{property}']")
            ?.GetAttribute("content");

        return string.IsNullOrWhiteSpace(value) ? null : CleanString(value);
    }

    private static string? ExtractBackgroundImage(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;

        var match = Regex.Match(style, @"background-image:\s*url\((['""]?)(?<url>.*?)\1\)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["url"].Value : null;
    }

    private string? ExtractLabeledValue(IElement content, string label)
    {
        var text = CleanString(content.TextContent);
        var match = Regex.Match(text, $@"{Regex.Escape(label)}:\s*(?<value>.*?)(?:Director:|Stars:|WATCH HERE|DL via|$)", RegexOptions.IgnoreCase);
        return match.Success ? CleanString(match.Groups["value"].Value) : null;
    }

    private static string? ExtractCountry(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;

        return tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => !Regex.IsMatch(x, @"^\d{4}s?$", RegexOptions.IgnoreCase)
                                 && !string.Equals(x, "HD", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(x, "FHD", StringComparison.OrdinalIgnoreCase));
    }

    private CommentModel? ToComment(IElement comment)
    {
        var text = CleanString(comment.QuerySelector(".comment-content")?.TextContent ?? "");
        if (string.IsNullOrWhiteSpace(text)) return null;

        return new CommentModel
        {
            Name = CleanString(comment.QuerySelector(".author-name")?.TextContent ?? ""),
            Date = CleanString(comment.QuerySelector(".comment-date")?.TextContent ?? ""),
            Comment = text
        };
    }

    private RelatedVideoModel? ToRelatedVideo(IElement link, string? poster)
    {
        var title = CleanTitle(link.TextContent);
        var href = link.GetAttribute("href");

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href)) return null;

        return new RelatedVideoModel
        {
            Title = title,
            Url = FixUrl(href, Config.MainUrl),
            Poster = poster
        };
    }

    private static string? CleanTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return Regex.Replace(value, @"^\s*WATCH\s+HERE\s+", "", RegexOptions.IgnoreCase).Trim();
    }

    private static string? ExtractYear(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var match = Regex.Match(title, @"\((\d{4})\)");
        return match.Success ? match.Groups[1].Value : null;
    }
}
