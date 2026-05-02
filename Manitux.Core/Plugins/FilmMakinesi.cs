using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Application;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using TlsClient.Core.Models.Entities;

namespace Manitux.Core.Plugins;

public class FilmMakinesi : PluginBase
{
    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.filmmakinesi",
        Name = "FilmMakinesi",
        Version = "1.0.0",
        Description = "Film Makinesi ile en yeni ve güncel filmleri Full HD kalite farkı ile izleyebilirsiniz.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://filmmakinesi.to",
        Favicon = "https://www.google.com/s2/favicons?domain=filmmakinesi.to&sz=64",
        Language = "tr"
    };


    public override async Task<List<CategoryModel>?> GetCategories()
    {
        string mainUrl = Config.MainUrl.TrimEnd('/');

        return new List<CategoryModel>
        {
            new() { Title = "Son Filmler", Url = $"{mainUrl}/filmler-1/" },
            new() { Title = "Aksiyon", Url = $"{mainUrl}/tur/aksiyon-fm1/film/" },
            new() { Title = "Aile", Url = $"{mainUrl}/tur/aile-fm1/film/" },
            new() { Title = "Animasyon", Url = $"{mainUrl}/tur/animasyon-fm2/film/" },
            new() { Title = "Belgesel", Url = $"{mainUrl}/tur/belgesel/film/" },
            new() { Title = "Biyografi", Url = $"{mainUrl}/tur/biyografi/film/" },
            new() { Title = "Bilim Kurgu", Url = $"{mainUrl}/tur/bilim-kurgu-fm3/film/" },
            new() { Title = "Dram", Url = $"{mainUrl}/tur/dram-fm1/film/" },
            new() { Title = "Fantastik", Url = $"{mainUrl}/tur/fantastik-fm1/film/" },
            new() { Title = "Gerilim", Url = $"{mainUrl}/tur/gerilim-fm1/film/" },
            new() { Title = "Gizem", Url = $"{mainUrl}/tur/gizem/film/" },
            new() { Title = "Komedi", Url = $"{mainUrl}/tur/komedi-fm1/film/" },
            new() { Title = "Korku", Url = $"{mainUrl}/tur/korku-fm1/film/" },
            new() { Title = "Macera", Url = $"{mainUrl}/tur/macera-fm1/film/" },
            new() { Title = "Müzik", Url = $"{mainUrl}/tur/muzik/film/" },
            new() { Title = "Polisiye", Url = $"{mainUrl}/tur/polisiye/film/" },
            new() { Title = "Romantik", Url = $"{mainUrl}/tur/romantik-fm1/film/" },
            new() { Title = "Savaş", Url = $"{mainUrl}/tur/savas-fm1/film/" },
            new() { Title = "Spor", Url = $"{mainUrl}/tur/spor/film/" },
            new() { Title = "Tarih", Url = $"{mainUrl}/tur/tarih/film/" },
            new() { Title = "Western", Url = $"{mainUrl}/tur/western-fm1/film/" }
        };
    }



    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var results = new List<PageItemModel>();

            string targetUrl = pageNumber == 1 ? category.Url : $"{category.Url.TrimEnd('/')}/page/{pageNumber}/";

            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Referer", $"{category.Url}/");

            //string referer = "https://hdfilmcehennemi.nl/";

            if (!targetUrl.EndsWith("/")) targetUrl += "/"; // önemli!

            string? html = await HttpGet(targetUrl, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);

            //string? html = await HttpGet(targetUrl);

            if (html is null) return null;

            using var document = await HtmlParse(html);

            if (document is null) return null;

            var items = document.QuerySelectorAll("div.item-relative");

            foreach (var item in items)
            {
                var title = item.QuerySelector("div.title")?.TextContent?.Trim();
                var href = item.QuerySelector("a")?.GetAttribute("href");

                var imgElement = item.QuerySelector("img");
                var poster = imgElement?.GetAttribute("data-src") ?? imgElement?.GetAttribute("src");
                poster = poster?.Replace("liste", "detay");

                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(href))
                {
                    results.Add(new PageItemModel
                    {
                        CategoryName = category.Title,
                        Title = title,
                        Url = FixUrl(href, Config.MainUrl),
                        Poster = !string.IsNullOrEmpty(poster) ? FixUrl(poster, Config.MainUrl) : null
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

    public override async Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem)
    {
        try
        {
            string url = pageItem.Url;

            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Referer", $"{Config.MainUrl}/");

            string? html = await HttpGet(url, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);

            if (html is null) return null;

            using var document = await HtmlParse(html);

            if (document is null) return null;

            var title = document.QuerySelector("h1.title")?.TextContent?.Trim();
            var poster = document.QuerySelector("img.cover-img")?.GetAttribute("src");
            var description = document.QuerySelector("div.info-description")?.TextContent?.Trim();
            var rating = document.QuerySelector("div.info div.imdb b")?.TextContent?.Trim();
            var year = document.QuerySelector("span.date a")?.TextContent?.Trim();

            var actors = string.Join(", ", document.QuerySelectorAll("div.cast-name").Select(x => x.TextContent.Trim()));
            var tags = string.Join(", ", document.QuerySelectorAll("div.type a[href*='/tur/']").Select(x => x.TextContent.Trim()));

            var timeText = document.QuerySelector("div.time")?.TextContent?.Trim() ?? "";
            var duration = Regex.Match(timeText, @"(\d+)").Value;

            var relatedVideos = new List<RelatedVideoModel>();

            var relateds = document.QuerySelectorAll("div.item-relative");

            if(relateds is not null)
            {
                foreach (var related in relateds)
                {
                   var relatedTitle = related.QuerySelector("div.title")?.TextContent.Trim() ?? ""; 
                   var relatedUrl = related.QuerySelector("a.item")?.GetAttribute("href")?.Trim() ?? "";
                   var relatedPoster = related.QuerySelector("img")?.GetAttribute("src")?.Trim() ?? "";

                   relatedVideos.Add(new() {Title = relatedTitle, Url = FixUrl(relatedUrl, Config.MainUrl), Poster = FixUrl(relatedPoster, Config.MainUrl)});
                }
            }

            var videoLinks = document.QuerySelectorAll("div.video-parts a");

            var videoSources = new List<VideoSourceModel>();

            var trailer = document.QuerySelector("a.button trailer-button")?.GetAttribute("data-video_url") ?? "";
            if (!string.IsNullOrEmpty(trailer))
            {
                videoSources.Add(new() { Name = "Fragman", Url = trailer });
            }

            // video links
            if (videoLinks.Any())
            {
                foreach (var link in videoLinks)
                {
                    // Butonun içindeki metin (Örn: Altyazılı Close)
                    string name = link.TextContent.Trim();

                    // 'data-video_url' özniteliğindeki link
                    string videoUrl = link.GetAttribute("data-video_url") ?? "";

                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        videoSources.Add(new VideoSourceModel
                        {
                            Name = name,
                            Url = FixUrl(videoUrl, Config.MainUrl),
                            Referer = url
                        });
                    }
                }
            }
            else
            {
                var iframe = document.QuerySelector("iframe[data-src]");
                if (iframe != null)
                {
                    string videoUrl = iframe.GetAttribute("data-src") ?? "";
                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        videoSources.Add(new VideoSourceModel
                        {
                            Name = "Play",
                            Url = FixUrl(videoUrl, Config.MainUrl)
                        });
                    }
                }
            }

            var allLinks = document.QuerySelectorAll("a[href]");
            var tmpEpisodes = new List<EpisodeModel>();

            foreach (var link in allLinks)
            {
                var href = link.GetAttribute("href");
                if (string.IsNullOrEmpty(href)) continue;

                var sMatch = Regex.Match(href, @"(\d+)-sezon", RegexOptions.IgnoreCase);
                var eMatch = Regex.Match(href, @"(\d+)-bolum", RegexOptions.IgnoreCase);

                if (sMatch.Success && eMatch.Success)
                {
                    int s = int.Parse(sMatch.Groups[1].Value);
                    int e = int.Parse(eMatch.Groups[1].Value);

                    var linkText = link.TextContent.Trim();
                    var epTitle = linkText.Contains("Bölüm") ? linkText.Split("Bölüm").Last().Trim() : "";

                    tmpEpisodes.Add(new EpisodeModel
                    {
                        SeasonNumber = s,
                        EpisodeNumber = e,
                        Title = epTitle,
                        Url = FixUrl(href, Config.MainUrl)
                    });
                }
            }

            // Aynı sezon ve bölüme ait mükerrer linkleri temizleyip, sezon ve bölüme göre sırala
            var episodes = tmpEpisodes
                .GroupBy(x => new { x.SeasonNumber, x.EpisodeNumber })
                .Select(g => g.First())
                .OrderBy(x => x.SeasonNumber)
                .ThenBy(x => x.EpisodeNumber)
                .ToList();

            return new MediaInfoModel
            {
                Title = title ?? pageItem.Title,
                Url = pageItem.Url,
                Poster = !string.IsNullOrEmpty(poster) ? FixUrl(poster, Config.MainUrl) : pageItem.Poster,
                Description = description,
                Rating = rating,
                Year = year,
                Actors = actors,
                Tags = tags,
                Duration = duration,
                Episodes = episodes.Any() ? episodes : null,
                RelatedVideos = relatedVideos.Any() ? relatedVideos : null,
                VideoSources = videoSources
            };
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override async Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        var results = new List<PageItemModel>();

        string searchUrl = $"{Config.MainUrl.TrimEnd('/')}/arama/?s={Uri.EscapeDataString(query)}";

        try
        {
            string? html = await HttpGet(url: searchUrl);

            if (html is null) return null;

            using var document = await HtmlParse(html);

            if (document is null) return null;

            var items = document.QuerySelectorAll("div.item-relative");

            foreach (var item in items)
            {
                var title = item.QuerySelector("div.title")?.TextContent?.Trim();
                var href = item.QuerySelector("a")?.GetAttribute("href");

                var imgElement = item.QuerySelector("img");
                var poster = imgElement?.GetAttribute("data-src") ?? imgElement?.GetAttribute("src");

                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(href))
                {
                    results.Add(new PageItemModel
                    {
                        Title = title,
                        Url = FixUrl(href, Config.MainUrl),
                        Poster = !string.IsNullOrEmpty(poster) ? FixUrl(poster, Config.MainUrl) : null
                    });
                }
            }

            if(results.Any()) return results;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return await ExtractAsync(videoSource, Config.MainUrl);
    }

    private async Task<string?> GetIframeSrc(string url, string referer)
    {
        // https://rapid.filmmakinesi.to/embed-bq52vqh9f4y5/ (url sonundaki / çok önemli!)
        // https://closeload.filmmakinesi.to/video/embed/C5H7iEmMfra/?imdb_id=tt18259538

        //Log(LogLevel.Debug, $"GetIframeSrc Url: {url} Referer: {referer}");

        try
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Content-Type", "application/json");
            headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 15.7; rv:135.0) Gecko/20100101 Firefox/135.0");
            headers.Add("X-Requested-With", "fetch");
            headers.Add("Referer", referer);

            string? html = await HttpGet(url: url, referer: referer, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
            //Log(LogLevel.Debug, $"GetIframeSrc Html: {html}");

            if (html is not null)
            {
                return html;
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }
}
