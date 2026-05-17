using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using TlsClient.Core.Models.Entities;
using TMDbLib.Objects.General;

namespace Manitux.Core;

public class FilmModu : PluginBase
{
    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.filmmodu",
        Name = "FilmModu",
        Version = "1.0.0",
        Description = "FilmModu uzerinden katalog, arama ve metadata kesfi saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://filmmodu.one",
        Favicon = "https://www.google.com/s2/favicons?domain=filmmodu.one&sz=64",
        Language = "tr"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        string mainUrl = Config.MainUrl;
        return new List<CategoryModel>
    {
        new() { Title = "4K", Url = $"{mainUrl}/hd-film-kategori/4k-film-izle?page=[pageNumber]" },
        new() { Title = "Aksiyon", Url = $"{mainUrl}/film-tur/aksiyon?page=[pageNumber]" },
        new() { Title = "Animasyon", Url = $"{mainUrl}/hd-film-kategori/animasyon?page=[pageNumber]" },
        new() { Title = "Bilim-Kurgu", Url = $"{mainUrl}/hd-film-kategori/bilim-kurgu-filmleri?page=[pageNumber]" },
        new() { Title = "Dram", Url = $"{mainUrl}/hd-film-kategori/dram-filmleri?page=[pageNumber]" },
        new() { Title = "Korku", Url = $"{mainUrl}/hd-film-kategori/korku-filmleri?page=[pageNumber]" },
        new() { Title = "Oscar Ödüllü", Url = $"{mainUrl}/hd-film-kategori/odullu-filmler-izle?page=[pageNumber]" }

    };
    }

    public override async Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem)
    {
        var html = await HttpGet(pageItem.Url);
        if (html is null) return null;

        var document = await HtmlParse(html);
        if (document is null) return null;

        var orgTitle = document.QuerySelector("div.titles h1")?.TextContent?.Trim();
        var altTitle = document.QuerySelector("div.titles h2")?.TextContent?.Trim();

        var actors = document.QuerySelectorAll("a[itemprop='actor'] span").Select(x => x.TextContent.Trim());
        var tags = document.QuerySelectorAll("a[href*='film-tur/']").Select(x => x.TextContent.Trim());

        var videoSources = new List<VideoSourceModel>();

        // 1. Alternatif butonlarını bul (Altyazılı, Dublaj vb.)
        var alternates = document.QuerySelectorAll("div.alternates a");

        foreach (var alt in alternates)
        {
            string altLink = FixUrl(alt.GetAttribute("href") ?? "", Config.MainUrl);
            string altName = alt.TextContent.Trim();

            if (string.IsNullOrEmpty(altLink)) continue;

            // 2. Her alternatifin sayfasına git ve video ID'lerini Regex ile yakala
            var altHtml = await HttpGet(altLink);
            if (altHtml is null) continue;

            string vidId = Regex.Match(altHtml, @"var videoId = '([^']*)'").Groups[1].Value;
            string vidType = Regex.Match(altHtml, @"var videoType = '([^']*)'").Groups[1].Value;

            if (string.IsNullOrEmpty(vidId)) continue;

            // 3. get-source API'sine istek at (JSON döner)
            string apiUrl = $"{Config.MainUrl}/get-source?movie_id={vidId}&type={vidType}";
            var jsonResponse = await HttpGet(apiUrl);
            if (jsonResponse is null) continue;

            // JSON içindeki 'sources' ve 'subtitle' alanlarını işle (System.Text.Json varsayalım)
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;

            string? subtitle = root.TryGetProperty("subtitle", out var subProp) ? FixUrl(subProp.GetString() ?? "", Config.MainUrl) : null;

            if (root.TryGetProperty("sources", out var sources))
            {
                foreach (var source in sources.EnumerateArray())
                {
                    string label = source.GetProperty("label").GetString() ?? "";
                    string srcUrl = source.GetProperty("src").GetString() ?? "";

                    videoSources.Add(new VideoSourceModel
                    {
                        Name = $"{altName} | {label}",
                        Url = FixUrl(srcUrl, Config.MainUrl),
                        Referer = Config.MainUrl,
                        Subtitles = new List<SubtitleModel>
                        {
                            new() { Name = altName, Url = subtitle ?? ""}
                        }
                    });
                }
            }
        }

        string backdrop = document.QuerySelector("div.embed-responsive div")?.GetAttribute("poster")?.ToString() ?? "";

        return new MediaInfoModel
        {
            Title = pageItem.Title,
            Url = pageItem.Url,
            Poster = FixUrl(document.QuerySelector("img.img-responsive")?.GetAttribute("src") ?? "", Config.MainUrl),
            Backdrop = FixUrl(backdrop, Config.MainUrl),
            Description = document.QuerySelector("p[itemprop='description']")?.TextContent?.Trim(),
            Rating = document.QuerySelector("span[itemprop='ratingValue']")?.TextContent?.Trim(),
            Year = Regex.Match(document.QuerySelector("span[itemprop='dateCreated']")?.TextContent ?? "", @"\d{4}").Value,
            Actors = string.Join(", ", actors),
            Tags = string.Join(", ", tags),
            VideoSources = videoSources.Any() ? videoSources : null
        };
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var results = new List<PageItemModel>();
            string targetUrl = category.Url.Replace("[pageNumber]", pageNumber.ToString());
            Debug.WriteLine(targetUrl);

             var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Referer", $"{category.Url}/");

            var html = await HttpGet(targetUrl, headers: headers, identifier: TlsClientIdentifier.Cloudscraper, followRedirects: true);
            if (html is null) return null;

            var document = await HtmlParse(html);
            if (document is null) return null;

            var items = document.QuerySelectorAll("div.movie");
            //items = items.Take(10).ToList<IHtmlCollection>();

            foreach (var item in items)
            {
                var title = item.QuerySelector("a")?.TextContent?.Trim();
                var href = item.QuerySelector("a")?.GetAttribute("href");
                var poster = item.QuerySelector("picture img")?.GetAttribute("data-src") ?? "";

                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(href))
                {
                    results.Add(new PageItemModel
                    {
                        Title = title,
                        Url = FixUrl(href, Config.MainUrl),
                        Poster = FixUrl(poster, Config.MainUrl),
                        CategoryName = category.Title
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        throw new NotImplementedException();
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return videoSource;
    }

    private Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Referer"] = Config.MainUrl
        };
    }
}
