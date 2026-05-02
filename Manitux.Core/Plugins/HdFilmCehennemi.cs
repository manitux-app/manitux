using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Application;
using Manitux.Core.Extractors;
using Manitux.Core.Helpers;
using Manitux.Core.Models;
using TlsClient.Core.Models.Entities;
using static Manitux.Core.Helpers.LogHelper;

namespace Manitux.Core.Plugins;

public class HdFilmCehennemi : PluginBase
{
    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.hdfilmcehennemi",
        Name = "HdFilmCehennemi",
        Version = "1.0.0",
        Description = "Türkiye'nin en hızlı hd film izleme sitesi.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://www.hdfilmcehennemi.nl",
        Favicon = "https://www.google.com/s2/favicons?domain=hdfilmcehennemi.nl&sz=64",
        Language = "tr"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        string mainUrl = Config.MainUrl;

        return new List<CategoryModel>
        {
            new() { Title = "Yeni Eklenen Filmler", Url = $"{mainUrl}" },
            new() { Title = "Yeni Eklenen Diziler", Url = $"{mainUrl}/yabancidiziizle-5" },
            new() { Title = "Türkçe Dublaj Filmler", Url = $"{mainUrl}/dil/turkce-dublajli-film-izleyin-5" },
            new() { Title = "Türkçe Altyazılı Filmler", Url = $"{mainUrl}/dil/turkce-altyazili-filmleri-izleme-sitesi-3" },
            new() { Title = "Tavsiye Filmler", Url = $"{mainUrl}/category/tavsiye-filmler-izle3" },
            new() { Title = "IMDB 7+ Filmler", Url = $"{mainUrl}/imdb-7-puan-uzeri-filmler-2" },
            new() { Title = "En Çok Yorumlananlar", Url = $"{mainUrl}/en-cok-yorumlananlar-2" },
            new() { Title = "En Çok Beğenilenler", Url = $"{mainUrl}/en-cok-begenilen-filmleri-izle-4" },
            new() { Title = "Nette İlk Filmler", Url = $"{mainUrl}/category/nette-ilk-filmler" },
            new() { Title = "1080p Filmler", Url = $"{mainUrl}/category/1080p-hd-film-izle-5" },
            new() { Title = "Amazon Yapımları", Url = $"{mainUrl}/category/amazon-yapimlarini-izle" },
            new() { Title = "Netflix Yapımları", Url = $"{mainUrl}/category/netflix-yapimlari-izle" },
            new() { Title = "Marvel Filmleri", Url = $"{mainUrl}/category/marvel-yapimlarini-izle-5" },
            new() { Title = "DC Filmleri", Url = $"{mainUrl}/category/dc-yapimlarini-izle-1" },
            new() { Title = "Aile Filmleri", Url = $"{mainUrl}/tur/aile-filmleri-izleyin-7" },
            new() { Title = "Aksiyon Filmleri", Url = $"{mainUrl}/tur/aksiyon-filmleri-izleyin-6" },
            new() { Title = "Animasyon Filmleri", Url = $"{mainUrl}/tur/animasyon-filmlerini-izleyin-5" },
            new() { Title = "Belgesel Filmleri", Url = $"{mainUrl}/tur/belgesel-filmlerini-izle-2" },
            new() { Title = "Bilim Kurgu Filmleri", Url = $"{mainUrl}/tur/bilim-kurgu-filmlerini-izleyin-5" },
            new() { Title = "Biyografi Filmleri", Url = $"{mainUrl}/tur/biyografi-filmleri-izle-3" },
            new() { Title = "Dram Filmleri", Url = $"{mainUrl}/tur/dram-filmlerini-izle-2" },
            new() { Title = "Fantastik Filmleri", Url = $"{mainUrl}/tur/fantastik-filmlerini-izleyin-3" },
            new() { Title = "Gerilim Filmleri", Url = $"{mainUrl}/tur/gerilim-filmlerini-izle-2" },
            new() { Title = "Gizem Filmleri", Url = $"{mainUrl}/tur/gizem-filmleri-izle-3" },
            new() { Title = "Komedi Filmleri", Url = $"{mainUrl}/tur/komedi-filmlerini-izleyin-2" },
            new() { Title = "Korku Filmleri", Url = $"{mainUrl}/tur/korku-filmlerini-izle-5" },
            new() { Title = "Macera Filmleri", Url = $"{mainUrl}/tur/macera-filmlerini-izleyin-4" },
            new() { Title = "Romantik Filmleri", Url = $"{mainUrl}/tur/romantik-filmleri-izle-3" },
            new() { Title = "Savaş Filmleri", Url = $"{mainUrl}/tur/savas-filmleri-izle-5" },
            new() { Title = "Spor Filmleri", Url = $"{mainUrl}/tur/spor-filmleri-izle-3" },
            new() { Title = "Suç Filmleri", Url = $"{mainUrl}/tur/suc-filmleri-izle-3" },
            new() { Title = "Tarih Filmleri", Url = $"{mainUrl}/tur/tarih-filmleri-izle-5" },
            new() { Title = "Western Filmleri", Url = $"{mainUrl}/tur/western-filmleri-izle-3" }
        };
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

            //string referer = "https://hdfilmcehennemi.nl/";

            string? html = await HttpGet(url, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);

            //string? html = await HttpGet(url, referer: $"{Config.MainUrl}/", headers: headers);
            //string? html = await HttpGet(url, headers: headers);
            //System.Console.WriteLine(html + Environment.NewLine);

            if (html is null) return null;

            using var document = await HtmlParse(html);

            if (document is null) return null;


            // --- Temel Metadata Ayıklama ---
            var title = document.QuerySelector("h1.section-title")?.TextContent?.Trim();
            var poster = document.QuerySelector("aside.post-info-poster img.lazyload")?.GetAttribute("data-src")
                         ?? document.QuerySelector("aside.post-info-poster img.lazyload")?.GetAttribute("src");

            var description = document.QuerySelector("article.post-info-content > p")?.TextContent?.Trim();

            // Türler (Tags)
            var tagsList = document.QuerySelectorAll("div.post-info-genres a").Select(x => x.TextContent.Trim());
            var tags = string.Join(", ", tagsList);

            // Rating (Örn: "8.5 (1200)" -> "8.5")
            var ratingRaw = document.QuerySelector("div.post-info-imdb-rating span")?.TextContent?.Trim();
            var rating = ratingRaw?.Split('(')[0].Trim();

            var year = document.QuerySelector("div.post-info-year-country a")?.TextContent?.Trim();

            // Oyuncular
            var actorsList = document.QuerySelectorAll("div.post-info-cast a > strong").Select(x => x.TextContent.Trim());
            var actors = string.Join(", ", actorsList);

            // Süre (Regex ile sadece sayıları al)
            var durationText = document.QuerySelector("div.post-info-duration")?.TextContent?.Trim() ?? "";
            var duration = Regex.Match(durationText, @"(\d+)").Value;

            // --- Dizi/Bölüm Kontrolü ---
            var epLinks = document.QuerySelectorAll("div.seasons-tab-content a");

            var videoSources = new List<VideoSourceModel>();
            var episodes = new List<EpisodeModel>();
            var relatedVideos = new List<RelatedVideoModel>();
            //var seasons = new List<SeasonModel>();


            if (epLinks.Any()) // dizi
            {
                // Bölümleri sezonlarına göre gruplayarak SeasonModel listesini doldur
                foreach (var ep in epLinks)
                {
                    var epName = ep.QuerySelector("h4")?.TextContent?.Trim();
                    var epHref = ep.GetAttribute("href");

                    if (!string.IsNullOrEmpty(epName) && !string.IsNullOrEmpty(epHref))
                    {
                        // Sezon ve Bölüm numarasını metinden ayıkla (Örn: "1. Sezon 5. Bölüm")
                        var sMatch = Regex.Match(epName, @"(\d+)\.?\s*Sezon", RegexOptions.IgnoreCase);
                        var eMatch = Regex.Match(epName, @"(\d+)\.?\s*Bölüm", RegexOptions.IgnoreCase);

                        int seasonNumber = sMatch.Success ? int.Parse(sMatch.Groups[1].Value) : 1;
                        int episodeNumber = eMatch.Success ? int.Parse(eMatch.Groups[1].Value) : 1;

                        episodes.Add(new EpisodeModel
                        {
                            SeasonNumber = seasonNumber,
                            EpisodeNumber = episodeNumber,
                            Title = epName,
                            Url = FixUrl(epHref, Config.MainUrl)
                        });
                    }
                }

                // Bölümleri SeasonModel yapısına dönüştür
                // seasons = episodes
                //     .GroupBy(e => e.SeasonNumber)
                //     .Select(g => new SeasonModel
                //     {
                //         SeasonNumber = g.Key,
                //         Episodes = g.OrderBy(e => e.EpisodeNumber).ToList()
                //     })
                //     .OrderBy(s => s.SeasonNumber)
                //     .ToList();
            }

            var relateds = document.QuerySelectorAll("div.slider-slide");
            if (relateds is not null)
            {
                foreach (var related in relateds)
                {
                    var relatedTitle = related.QuerySelector("strong.poster-title")?.TextContent.Trim() ?? "";
                    var relatedUrl = related.QuerySelector("a.poster poster-slider")?.GetAttribute("href")?.Trim() ?? "";
                    var relatedPoster = related.QuerySelector("img")?.GetAttribute("data-src")?.Trim() ?? "";

                    relatedVideos.Add(new() { Title = relatedTitle, Url = FixUrl(relatedUrl, Config.MainUrl), Poster = FixUrl(relatedPoster, Config.MainUrl) });
                }
            }

            var trailerMatch = Regex.Match(html, @"data-modal=""trailer/([^""]+)""");
            if (trailerMatch.Success)
            {
                if (trailerMatch.Groups[1].Value != "0")
                {
                    videoSources.Add(new() { Name = "Fragman", Url = $"https://www.youtube.com/embed/{trailerMatch.Groups[1].Value}" });
                }
            }

            // 1. Alternatif Link Gruplarını (Dil Grupları) Bul
            var alternatives = document.QuerySelectorAll("div.alternative-links");
            if (alternatives is not null)
            {
                foreach (var alternative in alternatives)
                {
                    string langCode = alternative.GetAttribute("data-lang")?.ToUpper() ?? "";

                    // Dil metnini butondan çek (Örn: "Türkçe Dublaj")
                    if (!string.IsNullOrEmpty(langCode))
                    {
                        var langBtn = document.QuerySelector($"button.language-link[data-lang='{langCode.ToLower()}']");
                        if (langBtn != null)
                        {
                            string langText = langBtn.TextContent.Trim();
                            // DUAL kontrolü
                            langCode = langText.Contains("DUAL", StringComparison.OrdinalIgnoreCase) ? "DUAL" : langText;
                        }
                    }

                    // 2. Grup İçindeki Video Kaynaklarını Bul
                    var links = alternative.QuerySelectorAll("button.alternative-link");

                    foreach (var link in links)
                    {
                        string sourceText = link.TextContent.Trim().Replace("(HDrip Xbet)", "").Trim();
                        string sourceName = $"{langCode} | {sourceText}".Trim('|', ' ');
                        string videoId = link.GetAttribute("data-video") ?? "";

                        if (!string.IsNullOrEmpty(videoId))
                        {
                            // https://www.hdfilmcehennemi.nl/video/220666
                            string tmplink = $"{Config.MainUrl}/video/{videoId}/";

                            string? iframeLink = await GetIframeSrc(tmplink, url);

                            if (iframeLink is not null)
                            {
                                videoSources.Add(new() { Name = sourceName, Url = iframeLink });
                            }

                        }
                    }
                }

            }

            return new MediaInfoModel
            {
                Title = title ?? "Bilinmiyor",
                Url = url,
                Poster = !string.IsNullOrEmpty(poster) ? FixUrl(poster, Config.MainUrl) : null,
                Description = description,
                Tags = tags,
                Rating = rating,
                Year = year,
                Actors = actors,
                Duration = duration,
                VideoSources = videoSources.Any() ? videoSources : null,
                Episodes = episodes.Any() ? episodes : null,
                RelatedVideos = relatedVideos.Any() ? relatedVideos : null
            };
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            // https://www.hdfilmcehennemi.nl/load/page/3/genres/aile-filmleri-izleyin-7/
            //string? html = await HttpGet($"{category.Url}/{pageNumber}");

            string apiUrl = category.Url;

            if(!category.Url.EndsWith("/")) category.Url += "/"; // önemli!

            apiUrl += "/?router=1";

            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Referer", $"{category.Url}");

            //string referer = "https://hdfilmcehennemi.nl/";

            //string? html = await HttpGet(apiUrl, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);

            string? json = await HttpGet(apiUrl, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);

            if (json is null) return null;

            string? html = null;

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                //string name = root.GetProperty("Name").GetString();
                //int number = root.GetProperty("Number").GetInt32();

                html = root.GetProperty("html").GetString() ?? null;
            }

            //string? html = await HttpGet($"{category.Url}");

            if (html is null) return null;

            using var document = await HtmlParse(html);

            if (document is null) return null;

            var results = new List<PageItemModel>();

            // 3. "div.section-content a.poster" seçicisi ile elemanları buluyoruz
            var items = document.QuerySelectorAll("div.section-content a.poster");

            foreach (var item in items)
            {
                // Başlık: strong.poster-title içindeki metin
                var title = item.QuerySelector("strong.poster-title")?.TextContent?.Trim();

                // Link: a etiketinin href özniteliği
                var href = item.GetAttribute("href");

                // Poster: img etiketinin data-src özniteliği
                var poster = item.QuerySelector("img")?.GetAttribute("data-src");

                // Başlık ve link varsa modeli oluşturup listeye ekle
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

            if (results.Any()) return results;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        //if URL extraction is required
        return await ExtractAsync(videoSource, Config.MainUrl);

        // otherwise direct returned
        //return videoSource;
    }

    public override async Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        try
        {
            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
            headers.Add("X-Requested-With", "fetch");

            string? jsonString = await HttpGet($"{Config.MainUrl}/search/?q={query}", referer: $"{Config.MainUrl}/", headers: headers);

            if (jsonString is null) return null;

            var results = new List<PageItemModel>();

            using var jsonDoc = JsonDocument.Parse(jsonString);
            if (jsonDoc.RootElement.TryGetProperty("results", out var jsonResults))
            {
                foreach (var element in jsonResults.EnumerateArray())
                {
                    var rawHtml = element.GetString();
                    if (string.IsNullOrEmpty(rawHtml)) continue;

                    using var document = await HtmlParse(rawHtml);

                    if (document is null) continue;

                    var title = document.QuerySelector("h4.title")?.TextContent?.Trim();
                    var anchor = document.QuerySelector("a");
                    var href = anchor?.GetAttribute("href");

                    var img = document.QuerySelector("img");
                    var poster = img?.GetAttribute("data-src") ?? img?.GetAttribute("src");

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(href))
                    {
                        results.Add(new PageItemModel
                        {
                            Title = title,
                            Url = FixUrl(href, Config.MainUrl),
                            Poster = !string.IsNullOrEmpty(poster)
                                ? FixUrl(poster, Config.MainUrl).Replace("/thumb/", "/list/")
                                : null
                        });
                    }
                }
            }

            if (results.Any()) return results;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    private async Task<string?> GetIframeSrc(string url, string referer)
    {
        // 	https://www.hdfilmcehennemi.nl/video/220665/ (url sonundaki / çok önemli!)

        //Log(LogLevel.Debug, $"GetIframeSrc Url: {url} Referer: {referer}");

        try
        {
            var headers = new Dictionary<string, string>();
            // headers.Add("Accept", "*/*");
            // headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            // headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            // headers.Add("Authorization", "Bearer f3ba67190133d0d59ad3a64cd096ed9e");

            // headers.Add("Host", "www.hdfilmcehennemi.nl");
            // headers.Add("Connection", "keep-alive");
            // //headers.Add("Cookie", "_ga_LR91MEQ0YR=GS2.1.s1776953780$o34$g1$t1776953783$j57$l0$h0; _ga=GA1.1.758154099.1775683170; logged=f3ba67190133d0d59ad3a64cd096ed9e; login=1; ch-wbc=MTM1NzA2ODg2MjgxMjYxNg==; ch-wtsx=NjU1MzgzODAxMjE3MDE2ODAw; ch-wtsy=1; ch-wtsz=1; ch-totalPlayTime_local=NDgyMjM3MDI3MDgxNjg5OTAw; ch-totalPlayTime_global=MTIzMzkwODUwODEzNjQ3NzYw");

            // headers.Add("Sec-Fetch-Dest", "empty");
            // headers.Add("Sec-Fetch-Mode", "cors");
            // headers.Add("Sec-Fetch-Site", "same-origin");

            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            headers.Add("Content-Type", "application/json");
            headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 15.7; rv:135.0) Gecko/20100101 Firefox/135.0");
            headers.Add("X-Requested-With", "fetch");
            headers.Add("Referer", referer);

            string? html = await HttpGet(url: url, referer: referer, headers: headers, identifier: TlsClientIdentifier.Cloudscraper);
            //Log(LogLevel.Debug, $"GetIframeSrc Html: {html}");

            if (html is not null)
            {
                // {"success":true,"data":{"html":"<iframe class=\"rapidrame\" data-src=\"https:\/\/www.hdfilmcehennemi.nl\/rplayer\/plwv7auptkcc\/\" scrolling=\"no\" allowfullscreen=\"true\"><\/iframe>"}}

                using (JsonDocument doc = JsonDocument.Parse(html))
                {
                    // data -> html yolundaki string değeri al
                    string? iframe = doc.RootElement
                                            .GetProperty("data")
                                            .GetProperty("html")
                                            .GetString();

                    if (iframe is not null)
                    {
                        // 2. HTML içindeki data-src değerini Regex ile çek
                        string pattern = @"data-src=""(?<url>.*?)""";
                        Match match = Regex.Match(iframe, pattern);

                        if (match.Success)
                        {
                            string src = match.Groups["url"].Value;
                            //Log(LogLevel.Debug, $"GetIframeSrc Src: {src}");
                            return src;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }
}
