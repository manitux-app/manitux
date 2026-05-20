using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;

namespace Manitux.Seyirturk;

public sealed class Seyirturk : PluginBase
{
    private readonly SeyirturkApiClient _api = new();
    private readonly SeyirturkParser _parser = new();

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.seyirturk",
        Name = "SeyirTurk",
        Version = "3.70.6",
        Description = "SeyirTurk Online Video Browser",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://cekke.uk/",
        Favicon = "https://www.google.com/s2/favicons?domain=seyirturk.net&sz=64",
        Language = "tr",
        UseProxy = false,
        IsAdult = false
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        try
        {
            // https://cekke.uk/sey/kodi/main.php?ct=b106dc966238b027c5fc2af0327f6280&surum=3.70.6
            // http://cekke.uk/sey/kodi/filmler.php?ct=b106dc966238b027c5fc2af0327f6280
            // http://cekke.uk/sey/kodi/diziler.php?ct=b106dc966238b027c5fc2af0327f6280
            // https://cekke.uk/sey/back/streams.php?id=420979
            
            // var main = await _api.GetMainAsync(Config.MainUrl);
            // var categories = main
            //     .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Link))
            //     .Where(x => Config.IsAdult || !ContainsAdultSignal(x.Title, x.Link))
            //     .Select(x => new CategoryModel
            //     {
            //         Title = SeyirturkParser.CleanKodiText(x.Title),
            //         Url = _api.AppendClientToken(x.Link!),
            //         Poster = x.Icon
            //     })
            //     .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            //     .ToList();

            //return categories.Count == 0 ? null : categories;

            return new List<CategoryModel>(){
            new()
            {
                Title = "Son Eklenen Filmler",
                Url = $"{Config.MainUrl}sey/back/show.php?type=all",
            },
            new()
            {
                Title = "En Popüler Filmler",
                Url = $"{Config.MainUrl}sey/back/show.php?type=pop",
            },
            new()
            {
                Title = "IMDB En Popüler Filmler",
                Url = $"{Config.MainUrl}sey/back/show.php?type=fav",
            },
            new()
            {
                Title = "IMDB Top 250 Film",
                Url = $"{Config.MainUrl}sey/back/show.php?type=top",
            },
            new()
            {
                Title = "4K Filmler",
                Url = $"{Config.MainUrl}sey/back/show.php?type=genre&genre=4KK&start=0&size=500",
            },
            new()
            {
                Title = "Yeni Bölümü Çıkan Diziler",
                Url = $"{Config.MainUrl}sey/back/show.php?type=all&p_type=TV",
            },
            new()
            {
                Title = "Yerli diziler",
                Url = $"{Config.MainUrl}sey/back/show.php?start=0&size=20&type=tr_tv",
            },
            new()
            {
                Title = "Yabancı diziler",
                Url = $"{Config.MainUrl}sey/back/show.php?start=0&size=20&type=en_tv",
            }
        };
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var url = category.Url;
            if (pageNumber > 1 && !TryBuildNextPageUrl(category.Url, pageNumber, out url))
            {
                return null;
            }

            var root = await _api.GetRootAsync(Config.MainUrl);
            var raw = await _api.GetRawAsync(url);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (raw.Contains("\"movies\":", StringComparison.OrdinalIgnoreCase))
            {
                var movies = await _api.GetMoviesAsync(url);
                return MapMovies(movies?.Movies, category.Title, root);
            }

            if (raw.Contains("\"links\":", StringComparison.OrdinalIgnoreCase))
            {
                var links = await _api.GetLinksAsync(url);
                return MapLinksAsPageItems(links?.Links, category.Title);
            }

            if (raw.Contains("\"episodes\":", StringComparison.OrdinalIgnoreCase))
            {
                var episodes = await _api.GetEpisodesAsync(url);
                return MapEpisodesAsPageItems(episodes?.Episodes, category.Title, root);
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return null;
    }

    public override async Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        try
        {
            var root = await _api.GetRootAsync(Config.MainUrl);
            var searchUrl = _api.BuildSearchUrl(root, query);
            var movies = await _api.GetMoviesAsync(searchUrl);
            return MapMovies(movies?.Movies, "Arama", root);
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
            var root = await _api.GetRootAsync(Config.MainUrl);
            var raw = await _api.GetRawAsync(pageItem.Url);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return CreateDirectMediaInfo(pageItem);
            }

            if (raw.Contains("\"episodes\":", StringComparison.OrdinalIgnoreCase))
            {
                var episodes = await _api.GetEpisodesAsync(pageItem.Url);
                return new MediaInfoModel
                {
                    Title = pageItem.Title,
                    Url = pageItem.Url,
                    Poster = pageItem.Poster,
                    Backdrop = pageItem.Backdrop,
                    Episodes = MapEpisodes(episodes?.Episodes, root)
                };
            }

            if (raw.Contains("\"links\":", StringComparison.OrdinalIgnoreCase))
            {
                var links = await _api.GetLinksAsync(pageItem.Url);
                return new MediaInfoModel
                {
                    Title = pageItem.Title,
                    Url = pageItem.Url,
                    Poster = pageItem.Poster,
                    Backdrop = pageItem.Poster,
                    VideoSources = MapVideoSources(links?.Links)
                };
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        return CreateDirectMediaInfo(pageItem);
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return await _parser.ResolveAsync(videoSource);
    }

    private MediaInfoModel CreateDirectMediaInfo(PageItemModel pageItem)
    {
        return new MediaInfoModel
        {
            Title = pageItem.Title,
            Url = pageItem.Url,
            Poster = pageItem.Poster,
            Backdrop = pageItem.Backdrop,
            Rating = pageItem.Rating,
            Year = pageItem.Year,
            VideoSources =
            [
                _parser.CreateSource(pageItem.Title, pageItem.Url)
            ]
        };
    }

    private List<PageItemModel>? MapMovies(IEnumerable<SeyirturkMovieItem>? movies, string categoryName, string root)
    {
        var items = movies?
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Where(x => Config.IsAdult || !ContainsAdultSignal(x.Name, x.Genres))
            .Select(x =>
            {
                var id = x.ID ?? string.Empty;
                var type = x.Type ?? string.Empty;
                var url = !string.IsNullOrWhiteSpace(x.Link)
                    ? _api.AppendClientToken(x.Link!)
                    : type.Equals("TV", StringComparison.OrdinalIgnoreCase)
                        ? _api.BuildEpisodesUrl(root, id)
                        : _api.BuildStreamsUrl(root, id);

                return new PageItemModel
                {
                    Title = BuildMovieTitle(x),
                    Url = url,
                    Poster = x.Image,
                    Rating = x.IMDBScore,
                    Year = ExtractYear(x.ReleaseDate),
                    CategoryName = categoryName
                };
            })
            .ToList();

        return items is { Count: > 0 } ? items : null;
    }

    private static List<PageItemModel>? MapLinksAsPageItems(IEnumerable<SeyirturkLinkItem>? links, string categoryName)
    {
        var items = links?
            .Where(x => !string.IsNullOrWhiteSpace(x.Link))
            .Select(x => new PageItemModel
            {
                Title = BuildLinkTitle(x),
                Url = x.Link!,
                Poster = x.Image,
                CategoryName = categoryName
            })
            .ToList();

        return items is { Count: > 0 } ? items : null;
    }

    private List<PageItemModel>? MapEpisodesAsPageItems(IEnumerable<SeyirturkEpisodeItem>? episodes, string categoryName, string root)
    {
        var items = episodes?
            .Where(x => !string.IsNullOrWhiteSpace(x.ID))
            .Select(x => new PageItemModel
            {
                Title = BuildEpisodeTitle(x),
                Url = BuildEpisodeStreamsUrl(root, x),
                Poster = x.Image ?? x.Backdrop,
                Rating = x.IMDBScore,
                Year = ExtractYear(x.ReleaseDate),
                CategoryName = categoryName
            })
            .ToList();

        return items is { Count: > 0 } ? items : null;
    }

    private List<EpisodeModel>? MapEpisodes(IEnumerable<SeyirturkEpisodeItem>? episodes, string root)
    {
        var mapped = episodes?
            .Where(x => !string.IsNullOrWhiteSpace(x.ID))
            .Select(x => new EpisodeModel
            {
                SeasonNumber = ParseInt(x.Season),
                EpisodeNumber = ParseInt(x.Episode),
                Title = BuildEpisodeTitle(x),
                Url = BuildEpisodeStreamsUrl(root, x),
                Description = x.Summary
            })
            .ToList();

        return mapped is { Count: > 0 } ? mapped : null;
    }

    private List<VideoSourceModel>? MapVideoSources(IEnumerable<SeyirturkLinkItem>? links)
    {
        var sources = links?
            .Where(x => !string.IsNullOrWhiteSpace(x.Link))
            .Select(x => _parser.CreateSource(BuildLinkTitle(x), x.Link!, x.Link))
            .ToList();

        return sources is { Count: > 0 } ? sources : null;
    }

    private string BuildEpisodeStreamsUrl(string root, SeyirturkEpisodeItem episode)
    {
        var url = _api.BuildStreamsUrl(root, episode.ID!);
        url += "&isTv=1";

        if (!string.IsNullOrWhiteSpace(episode.Episode))
        {
            url += "&e=" + Uri.EscapeDataString(episode.Episode);
        }

        if (!string.IsNullOrWhiteSpace(episode.Season))
        {
            url += "&s=" + Uri.EscapeDataString(episode.Season);
        }

        if (!string.IsNullOrWhiteSpace(episode.E_ID))
        {
            url += "&e_id=" + Uri.EscapeDataString(episode.E_ID);
        }

        return url;
    }

    private static string BuildMovieTitle(SeyirturkMovieItem movie)
    {
        var suffix = LanguageSuffix(movie.Language);
        return SeyirturkParser.CleanKodiText($"{movie.Name} {suffix}");
    }

    private static string BuildLinkTitle(SeyirturkLinkItem link)
    {
        var provider = link.Provider ?? link.Name ?? link.MainProvider ?? "Kaynak";
        var language = LanguageSuffix(link.isTurkish);
        var title = link.name;
        return SeyirturkParser.CleanKodiText(string.IsNullOrWhiteSpace(title)
            ? $"{provider} {language}"
            : $"{provider} {language} - {title}");
    }

    private static string BuildEpisodeTitle(SeyirturkEpisodeItem episode)
    {
        var season = ParseInt(episode.Season);
        var number = ParseInt(episode.Episode);
        var suffix = season > 0 || number > 0 ? $" S{season}B{number}" : string.Empty;
        return SeyirturkParser.CleanKodiText($"{episode.Name}{suffix}");
    }

    private static string LanguageSuffix(string? value)
    {
        return value switch
        {
            "0" => "TA",
            "1" => "TD",
            "2" => "TA - TD",
            "3" => "GE",
            "7" => "YK",
            _ => string.Empty
        };
    }

    private static bool ContainsAdultSignal(params string?[] values)
    {
        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value)
            && (value.Contains("Adult", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Erotik", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Yetiskin", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Yetişkin", StringComparison.OrdinalIgnoreCase)));
    }

    private static string? ExtractYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = Regex.Match(value, @"\d{4}");
        return match.Success ? match.Value : null;
    }

    private static int ParseInt(string? value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    private static bool TryBuildNextPageUrl(string url, int pageNumber, out string pagedUrl)
    {
        pagedUrl = url;
        var match = Regex.Match(url, @"start=(?<start>\d+)&size=(?<size>\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        var size = int.Parse(match.Groups["size"].Value);
        var start = (Math.Max(1, pageNumber) - 1) * size;
        pagedUrl = Regex.Replace(url, @"start=\d+", "start=" + start, RegexOptions.IgnoreCase);
        return true;
    }
}
