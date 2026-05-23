using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace Manitux.Core.Plugins;

public class TmdbPlugin : PluginBase
{
    private const string Movie = "movie";
    private const string Tv = "tv";
    private const string ImageFallbackBaseUrl = "https://image.tmdb.org/t/p/";

    private TMDbClient? _client;

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.tmdb",
        Name = "TMDb",
        Version = "1.0.0",
        Description = "The Movie Database uzerinden film ve dizi metadata kesfi saglar.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://www.themoviedb.org",
        Favicon = "https://www.google.com/s2/favicons?domain=themoviedb.org&sz=64",
        Language = "tr",
        ApiKey = "1fe5b65d4ae5cb5d10da74818a572875"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Populer Filmler", Url = BuildCategoryUrl(Movie, "popular") },
            new() { Title = "En Iyi Filmler", Url = BuildCategoryUrl(Movie, "top-rated") },
            new() { Title = "Vizyondaki Filmler", Url = BuildCategoryUrl(Movie, "now-playing") },
            new() { Title = "Yaklasan Filmler", Url = BuildCategoryUrl(Movie, "upcoming") },
            new() { Title = "Populer Diziler", Url = BuildCategoryUrl(Tv, "popular") },
            new() { Title = "En Iyi Diziler", Url = BuildCategoryUrl(Tv, "top-rated") }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var client = await GetClientAsync();
            if (client is null) return null;

            var (mediaType, listType) = ParseUrl(category.Url);
            var page = Math.Max(pageNumber, 1);

            return mediaType switch
            {
                Movie => await GetMovieItems(client, listType, page, category.Title),
                Tv => await GetTvItems(client, listType, page, category.Title),
                _ => null
            };
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
            var client = await GetClientAsync();
            if (client is null) return null;

            var (mediaType, idText) = ParseUrl(pageItem.Url);
            if (!int.TryParse(idText, out var id)) return null;

            return mediaType switch
            {
                Movie => await GetMovieInfo(client, id, pageItem),
                Tv => await GetTvInfo(client, id, pageItem),
                _ => null
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

            var client = await GetClientAsync();
            if (client is null) return null;

            var movies = await client.SearchMovieAsync(query, Config.Language, 1, Config.IsAdult, 0, null, 0);
            var tvShows = await client.SearchTvShowAsync(query, Config.Language, 1, Config.IsAdult, 0);

            var results = new List<PageItemModel>();
            results.AddRange((movies?.Results ?? []).Select(x => ToPageItem(x, "Film Arama")));
            results.AddRange((tvShows?.Results ?? []).Select(x => ToPageItem(x, "Dizi Arama")));

            return results
                .OrderByDescending(x => double.TryParse(x.Rating, out var rating) ? rating : 0)
                .ToList();
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return await ExtractAsync(videoSource);
    }

    private async Task<TMDbClient?> GetClientAsync()
    {
        if (string.IsNullOrWhiteSpace(Config.ApiKey))
        {
            Log(LogLevel.Warning, "TMDb ApiKey bos. Plugin config dosyasina ApiKey eklenmeli.");
            return null;
        }

        if (_client is not null) return _client;

        _client = new TMDbClient(Config.ApiKey);
        await _client.GetConfigAsync();
        return _client;
    }

    private async Task<List<PageItemModel>?> GetMovieItems(TMDbClient client, string listType, int page, string categoryName)
    {
        SearchContainer<SearchMovie>? results = listType switch
        {
            "top-rated" => await client.GetMovieTopRatedListAsync(Config.Language, page),
            "now-playing" => await client.GetMovieNowPlayingListAsync(Config.Language, page),
            "upcoming" => await client.GetMovieUpcomingListAsync(Config.Language, page),
            _ => await client.GetMoviePopularListAsync(Config.Language, page)
        };

        return results?.Results?.Select(x => ToPageItem(x, categoryName)).ToList();
    }

    private async Task<List<PageItemModel>?> GetTvItems(TMDbClient client, string listType, int page, string categoryName)
    {
        SearchContainer<SearchTv>? results = listType switch
        {
            "top-rated" => await client.GetTvShowTopRatedAsync(page, Config.Language),
            _ => await client.GetTvShowPopularAsync(page, Config.Language)
        };

        return results?.Results?.Select(x => ToPageItem(x, categoryName)).ToList();
    }

    private async Task<MediaInfoModel?> GetMovieInfo(TMDbClient client, int id, PageItemModel pageItem)
    {
        var movie = await client.GetMovieAsync(
            id,
            Config.Language,
            null,
            MovieMethods.Credits | MovieMethods.Videos | MovieMethods.Recommendations | MovieMethods.ExternalIds);

        if (movie is null) return null;

        var videoSources = GetTrailers(movie.Videos?.Results).ToList();
        var relatedVideos = movie.Recommendations?.Results?
            .Select(x => new RelatedVideoModel
            {
                Title = x.Title ?? "TMDb Movie",
                Url = BuildItemUrl(Movie, x.Id),
                Poster = BuildImageUrl(x.PosterPath, "w500")
            })
            .ToList();

        return new MediaInfoModel
        {
            ImdbId = movie.ImdbId,
            Title = movie.Title ?? pageItem.Title,
            Url = BuildItemUrl(Movie, movie.Id),
            Description = movie.Overview,
            Poster = BuildImageUrl(movie.PosterPath, "w500"),
            Backdrop = BuildImageUrl(movie.BackdropPath, "w1280") ?? BuildImageUrl(movie.PosterPath, "w500"),
            Trailer = videoSources.FirstOrDefault()?.Url,
            Tags = string.Join(", ", movie.Genres?.Select(x => x.Name) ?? []),
            Rating = FormatRating(movie.VoteAverage),
            Year = movie.ReleaseDate?.Year.ToString(),
            Duration = movie.Runtime?.ToString(),
            Actors = string.Join(", ", movie.Credits?.Cast?.Take(8).Select(x => x.Name) ?? []),
            Country = string.Join(", ", movie.ProductionCountries?.Select(x => x.Name) ?? []),
            VideoSources = videoSources.Count > 0 ? videoSources : null,
            RelatedVideos = relatedVideos is { Count: > 0 } ? relatedVideos : null
        };
    }

    private async Task<MediaInfoModel?> GetTvInfo(TMDbClient client, int id, PageItemModel pageItem)
    {
        var tvShow = await client.GetTvShowAsync(
            id,
            TvShowMethods.Credits | TvShowMethods.Videos | TvShowMethods.Recommendations | TvShowMethods.ExternalIds,
            Config.Language);

        if (tvShow is null) return null;

        var videoSources = GetTrailers(tvShow.Videos?.Results).ToList();
        var relatedVideos = tvShow.Recommendations?.Results?
            .Select(x => new RelatedVideoModel
            {
                Title = x.Name ?? "TMDb TV Show",
                Url = BuildItemUrl(Tv, x.Id),
                Poster = BuildImageUrl(x.PosterPath, "w500")
            })
            .ToList();

        var episodes = tvShow.Seasons?
            .Where(x => x.SeasonNumber > 0)
            .Select(x => new EpisodeModel
            {
                SeasonNumber = x.SeasonNumber,
                EpisodeNumber = 0,
                Title = x.Name,
                Description = x.Overview,
                Url = BuildItemUrl(Tv, tvShow.Id)
            })
            .ToList();

        return new MediaInfoModel
        {
            ImdbId = tvShow.ExternalIds?.ImdbId,
            Title = tvShow.Name ?? pageItem.Title,
            Url = BuildItemUrl(Tv, tvShow.Id),
            Description = tvShow.Overview,
            Poster = BuildImageUrl(tvShow.PosterPath, "w500"),
            Backdrop = BuildImageUrl(tvShow.BackdropPath, "w1280") ?? BuildImageUrl(tvShow.PosterPath, "w500"),
            Trailer = videoSources.FirstOrDefault()?.Url,
            Tags = string.Join(", ", tvShow.Genres?.Select(x => x.Name) ?? []),
            Rating = FormatRating(tvShow.VoteAverage),
            Year = tvShow.FirstAirDate?.Year.ToString(),
            Duration = tvShow.EpisodeRunTime?.FirstOrDefault().ToString(),
            Actors = string.Join(", ", tvShow.Credits?.Cast?.Take(8).Select(x => x.Name) ?? []),
            Country = string.Join(", ", tvShow.OriginCountry ?? []),
            VideoSources = videoSources.Count > 0 ? videoSources : null,
            Episodes = episodes is { Count: > 0 } ? episodes : null,
            RelatedVideos = relatedVideos is { Count: > 0 } ? relatedVideos : null
        };
    }

    private PageItemModel ToPageItem(SearchMovie movie, string categoryName)
    {
        return new PageItemModel
        {
            CategoryName = categoryName,
            Title = movie.Title ?? "TMDb Movie",
            Url = BuildItemUrl(Movie, movie.Id),
            Poster = BuildImageUrl(movie.PosterPath, "w500"),
            Rating = FormatRating(movie.VoteAverage),
            Year = movie.ReleaseDate?.Year.ToString()
        };
    }

    private PageItemModel ToPageItem(SearchTv tvShow, string categoryName)
    {
        return new PageItemModel
        {
            CategoryName = categoryName,
            Title = tvShow.Name ?? "TMDb TV Show",
            Url = BuildItemUrl(Tv, tvShow.Id),
            Poster = BuildImageUrl(tvShow.PosterPath, "w500"),
            Rating = FormatRating(tvShow.VoteAverage),
            Year = tvShow.FirstAirDate?.Year.ToString()
        };
    }

    private IEnumerable<VideoSourceModel> GetTrailers(IEnumerable<Video>? videos)
    {
        if (videos is null) yield break;

        foreach (var video in videos
                     .Where(x => string.Equals(x.Site, "YouTube", StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(x => string.Equals(x.Type, "Trailer", StringComparison.OrdinalIgnoreCase))
                     .ThenByDescending(x => x.Official))
        {
            yield return new VideoSourceModel
            {
                Name = video.Name ?? "Trailer",
                Url = $"https://www.youtube.com/watch?v={video.Key}",
                IsTrailer = true
            };
        }
    }

    private string? BuildImageUrl(string? path, string size)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var baseUrl = _client?.Config?.Images?.SecureBaseUrl ?? ImageFallbackBaseUrl;
        return $"{baseUrl}{size}{path}";
    }

    private static string FormatRating(double rating)
    {
        return rating > 0 ? rating.ToString("0.0") : string.Empty;
    }

    private static string BuildCategoryUrl(string mediaType, string listType)
    {
        return $"tmdb://{mediaType}/{listType}";
    }

    private static string BuildItemUrl(string mediaType, int id)
    {
        return $"tmdb://{mediaType}/{id}";
    }

    private static (string MediaType, string Value) ParseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return (string.Empty, string.Empty);

        const string prefix = "tmdb://";
        var value = url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? url[prefix.Length..]
            : url;

        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (string.Empty, string.Empty);
    }
}
