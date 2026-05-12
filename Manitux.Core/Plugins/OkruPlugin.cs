using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Plugins;

public class OkruPlugin : PluginBase
{
    private string MainUrl => string.IsNullOrWhiteSpace(Config.MainUrl)
        ? "https://ok.ru"
        : Config.MainUrl.TrimEnd('/');

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.okru",
        Name = "OKRu",
        Version = "1.0.0",
        Description = "OK.ru uzerinden video katalog, arama ve metadata saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://ok.ru",
        Favicon = "https://www.google.com/s2/favicons?domain=ok.ru&sz=64",
        Language = "en"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Videos", Url = $"{MainUrl}/video/showcase" },
            new() { Title = "Soviet", Url = $"{MainUrl}/video/kino/soviet" },
            new() { Title = "Drama", Url = $"{MainUrl}/video/kino/drama" },
            new() { Title = "Action", Url = $"{MainUrl}/video/kino/action" },
            new() { Title = "Family", Url = $"{MainUrl}/video/kino/family" },
            new() { Title = "Comedy Movies", Url = $"{MainUrl}/video/kino/comedy" },
            new() { Title = "Series", Url = $"{MainUrl}/video/serial" }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var html = pageNumber <= 1
                ? await HttpGet(category.Url, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144, useCookie: true)
                : await GetPagedCategoryHtml(category.Url, pageNumber);

            if (string.IsNullOrWhiteSpace(html)) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var items = category.Url.Contains("/video/serial", StringComparison.OrdinalIgnoreCase)
                ? ParseSeriesItems(document, category.Title)
                : ParseMovieItems(document, category.Title);

            return items.Count > 0 ? items.DistinctBy(x => x.Url).ToList() : null;
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
            var canonicalUrl = CanonicalVideoUrl(pageItem.Url);
            var html = await HttpGet(pageItem.Url, referer: MainUrl, headers: GetHeaders(), identifier: TlsClientIdentifier.Chrome144, useCookie: true);
            if (string.IsNullOrWhiteSpace(html)) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var isAlbum = pageItem.Url.Contains("/video/c", StringComparison.OrdinalIgnoreCase);
            var title = GetTitle(document, isAlbum) ?? pageItem.Title;
            var poster = isAlbum
                ? document.QuerySelector("div.ugrid_i.js-seen-item-movie img.video-card_img")?.GetAttribute("src")
                : document.QuerySelector("meta[property='og:image']")?.GetAttribute("content") ?? pageItem.Poster;

            var related = ParseRelatedItems(document);
            var author = document.QuerySelector("meta[property='ya:ovs:login']")?.GetAttribute("content");

            if (isAlbum)
            {
                var episodes = ParseEpisodes(document);
                return new MediaInfoModel
                {
                    Title = title,
                    Url = canonicalUrl,
                    Poster = FixUrlNullable(poster),
                    Backdrop = FixUrlNullable(poster),
                    Tags = author,
                    Episodes = episodes.Count > 0 ? episodes : null,
                    VideoSources = episodes.Count == 1
                        ? new List<VideoSourceModel> { CreateOkVideoSource(episodes[0].Url) }
                        : null,
                    RelatedVideos = related.Count > 0 ? related : null
                };
            }

            var embedUrl = GetEmbedUrl(document, canonicalUrl);
            return new MediaInfoModel
            {
                Title = title,
                Url = canonicalUrl,
                Poster = FixUrlNullable(poster),
                Backdrop = FixUrlNullable(poster),
                Duration = GetDuration(document),
                Tags = author,
                VideoSources = new List<VideoSourceModel> { CreateOkVideoSource(embedUrl) },
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

            var url = $"{MainUrl}/video/search?st.cmd=video&st.psft=showcase&st.m=SEARCH&st.ft=search&st.fuvh=on&st.furl=%2Fvideo%2Fshowcase&cmd=VideoContentBlock";
            var html = await HttpPost(
                url,
                new Dictionary<string, string>
                {
                    ["st.v.sq"] = query,
                    ["gwt.requested"] = "9579ea2eT1774883610506"
                },
                referer: $"{MainUrl}/video/showcase");

            if (string.IsNullOrWhiteSpace(html)) return null;

            using var document = await HtmlParse(html);
            if (document is null) return null;

            var props = document.QuerySelector("video-search-result")?.GetAttribute("data-props");
            if (string.IsNullOrWhiteSpace(props)) return ParseMovieItems(document, "Search");

            var searchData = JsonSerializer.Deserialize<SearchData>(WebUtility.HtmlDecode(props));
            var items = searchData?.Videos?.List?
                .Select(ToSearchPageItem)
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();

            return items is { Count: > 0 } ? items : null;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        var extractor = Manitux.Core.Extractors.ExtractorManager.GetExtractorByName("Okru");
        return extractor is null
            ? await ExtractAsync(videoSource, videoSource.Referer ?? MainUrl)
            : await extractor.ExtractAsync(videoSource, videoSource.Referer ?? MainUrl);
    }

    private async Task<string?> GetPagedCategoryHtml(string categoryUrl, int pageNumber)
    {
        var isShowcase = categoryUrl.Contains("showcase", StringComparison.OrdinalIgnoreCase);
        var tag = categoryUrl.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
        var postUrl = isShowcase
            ? $"{MainUrl}/video/showcase?st.cmd=anonymVideo&st.m=SHOWCASE&st.ft=showcase&st.furl=%2Fvideo%2Fshowcase&cmd=VideoUniversalContentBlock"
            : $"{MainUrl}/video/serial?st.cmd=anonymVideo&st.fltag={Uri.EscapeDataString(tag)}&st.m=ALBUMS_CATALOG&st.ft=serial&st.furl=%2Fvideo%2Fserial%2F{Uri.EscapeDataString(tag)}&cmd=VideoUniversalContentBlock";

        return await HttpPost(
            postUrl,
            new Dictionary<string, string>
            {
                ["fetch"] = "false",
                ["st.page"] = pageNumber.ToString(),
                ["st.lastelem"] = isShowcase ? "18" : "1669805881145",
                ["gwt.requested"] = "9579ea2eT1774883610506"
            },
            referer: categoryUrl,
            headers: new Dictionary<string, string>
            {
                ["ok-screen"] = "anonymVideo",
                ["X-Requested-With"] = "XMLHttpRequest",
                ["User-Agent"] = GetUserAgent()
            });
    }

    private List<PageItemModel> ParseMovieItems(IDocument document, string categoryName)
    {
        var items = new List<PageItemModel>();
        foreach (var item in document.QuerySelectorAll("div.ugrid_i.js-seen-item-movie"))
        {
            var card = item.QuerySelector("div.video-card");
            var title = card?.QuerySelector("a.video-card_n")?.TextContent?.Trim();
            var href = card?.QuerySelector("a.video-card_lk")?.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(href)) continue;

            items.Add(new PageItemModel
            {
                CategoryName = categoryName,
                Title = title,
                Url = FixUrl(href, MainUrl),
                Poster = FixUrlNullable(card?.QuerySelector("img.video-card_img")?.GetAttribute("src"))
            });
        }

        return items;
    }

    private List<PageItemModel> ParseSeriesItems(IDocument document, string categoryName)
    {
        var items = new List<PageItemModel>();
        foreach (var slider in document.QuerySelectorAll("video-channels-vitrine-slider"))
        {
            var props = WebUtility.HtmlDecode(slider.GetAttribute("data-props") ?? string.Empty)
                .Replace("\\u0026", "&");

            foreach (Match match in Regex.Matches(props, @"""id"":""[^""]*"".*?""href"":""(?<href>[^""]+)"",""name"":""(?<name>[^""]+)"",""imageUrl"":""(?<image>[^""]+)""", RegexOptions.Singleline))
            {
                var name = CleanJsonString(match.Groups["name"].Value);
                var href = CleanJsonString(match.Groups["href"].Value);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;

                items.Add(new PageItemModel
                {
                    CategoryName = categoryName,
                    Title = name,
                    Url = FixUrl(href, MainUrl),
                    Poster = FixUrlNullable(CleanJsonString(match.Groups["image"].Value))
                });
            }
        }

        return items;
    }

    private List<EpisodeModel> ParseEpisodes(IDocument document)
    {
        var episodes = new List<EpisodeModel>();
        var index = 1;

        foreach (var item in document.QuerySelectorAll("div.ugrid_i.js-seen-item-movie"))
        {
            var id = item.QuerySelector("div.video-card")?.GetAttribute("data-id");
            if (string.IsNullOrWhiteSpace(id)) continue;

            episodes.Add(new EpisodeModel
            {
                EpisodeNumber = index++,
                Url = $"{MainUrl}/video/{id}",
                Title = item.QuerySelector("a.video-card_n")?.TextContent?.Trim(),
                Description = item.QuerySelector("div.video-card_duration")?.TextContent?.Trim()
            });
        }

        return episodes.DistinctBy(x => x.Url).Reverse().ToList();
    }

    private List<RelatedVideoModel> ParseRelatedItems(IDocument document)
    {
        return document.QuerySelectorAll("li.reco-content_item__7rrgi")
            .Select(el =>
            {
                var raw = el.QuerySelector("a.link__91azp")?.GetAttribute("href");
                var title = el.QuerySelector("div.card-title__el6vj")?.TextContent?.Trim()
                            ?? el.QuerySelector("img")?.GetAttribute("alt");
                if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(title)) return null;

                var url = CanonicalVideoUrl(FixUrl(raw, MainUrl));
                return new RelatedVideoModel
                {
                    Title = title,
                    Url = url,
                    Poster = FixUrlNullable(el.QuerySelector("img")?.GetAttribute("src"))
                };
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .DistinctBy(x => x.Url)
            .ToList();
    }

    private PageItemModel? ToSearchPageItem(SearchItem item)
    {
        var title = item.Movie?.Title ?? item.Name;
        var id = item.Movie?.Id;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(id)) return null;

        return new PageItemModel
        {
            CategoryName = "Search",
            Title = title,
            Url = $"{MainUrl}/video/{id}",
            Poster = FixUrlNullable(item.Movie?.Thumbnail?.Big ?? item.Movie?.Thumbnail?.Small ?? item.ImageUrl)
        };
    }

    private VideoSourceModel CreateOkVideoSource(string url)
    {
        return new VideoSourceModel
        {
            Name = "Okru",
            Url = url,
            Referer = MainUrl
        };
    }

    private string GetEmbedUrl(IDocument document, string canonicalUrl)
    {
        return document.QuerySelector("meta[property='og:video:secure_url']")?.GetAttribute("content")
               ?? document.QuerySelector("meta[property='og:video:url']")?.GetAttribute("content")
               ?? canonicalUrl.Replace("/video/", "/videoembed/", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetTitle(IDocument document, bool album)
    {
        if (album)
        {
            var albumTitle = document.QuerySelector("h3.album-info_name")?.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(albumTitle)) return albumTitle;
        }

        return document.QuerySelector("meta[property='og:title']")?.GetAttribute("content")
               ?? document.QuerySelector("title")?.TextContent?.Trim();
    }

    private static string? GetDuration(IDocument document)
    {
        var seconds = document.QuerySelector("meta[property='video:duration']")?.GetAttribute("content");
        return int.TryParse(seconds, out var value) ? Math.Max(1, value / 60).ToString() : null;
    }

    private string CanonicalVideoUrl(string url)
    {
        var match = Regex.Match(url, @"video/(?<id>\d+)", RegexOptions.IgnoreCase);
        return match.Success ? $"{MainUrl}/video/{match.Groups["id"].Value}" : url.Split('?')[0];
    }

    private string? FixUrlNullable(string? url)
    {
        return string.IsNullOrWhiteSpace(url) ? null : FixUrl(url, MainUrl);
    }

    private static string CleanJsonString(string value)
    {
        return Regex.Unescape(value).Replace("\\u0026", "&").Replace("\\/", "/");
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["User-Agent"] = GetUserAgent()
        };
    }

    private static string GetUserAgent()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
    }

    private sealed class SearchData
    {
        [JsonPropertyName("videos")]
        public SearchVideos? Videos { get; set; }
    }

    private sealed class SearchVideos
    {
        [JsonPropertyName("list")]
        public List<SearchItem>? List { get; set; }

        [JsonPropertyName("hasMore")]
        public bool? HasMore { get; set; }
    }

    private sealed class SearchItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("movie")]
        public SearchMovie? Movie { get; set; }
    }

    private sealed class SearchMovie
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("thumbnail")]
        public SearchThumbnail? Thumbnail { get; set; }
    }

    private sealed class SearchThumbnail
    {
        [JsonPropertyName("small")]
        public string? Small { get; set; }

        [JsonPropertyName("big")]
        public string? Big { get; set; }
    }
}
