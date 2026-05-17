using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;

namespace Manitux.PluginTemplate;

public sealed class PluginTemplate : PluginBase
{
    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.template",
        Name = "Plugin Template",
        Version = "1.0.0",
        Description = "A starter Manitux plugin with example categories, page items, media details, episodes, and video sources.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = "https://example.com",
        Favicon = "https://www.google.com/s2/favicons?domain=example.com&sz=64",
        Language = "en",
        UseProxy = false,
        IsAdult = false
    };

    private static readonly List<PageItemModel> Items =
    [
        new()
        {
            CategoryName = "Featured",
            Title = "Example Movie",
            Url = "template://movie/example-movie",
            Poster = "https://placehold.co/300x450/1f2937/ffffff?text=Movie",
            Rating = "8.4",
            Year = "2026"
        },
        new()
        {
            CategoryName = "Featured",
            Title = "Example Series",
            Url = "template://series/example-series",
            Poster = "https://placehold.co/300x450/273449/ffffff?text=Series",
            Rating = "7.9",
            Year = "2026"
        },
        new()
        {
            CategoryName = "Latest",
            Title = "Example Documentary",
            Url = "template://movie/example-documentary",
            Poster = "https://placehold.co/300x450/334155/ffffff?text=Doc",
            Rating = "8.1",
            Year = "2025"
        }
    ];

    public override Task<List<CategoryModel>?> GetCategories()
    {
        return Task.FromResult<List<CategoryModel>?>([
            new()
            {
                Title = "Featured",
                Url = "template://category/featured",
                Poster = "https://placehold.co/600x320/1f2937/ffffff?text=Featured"
            },
            new()
            {
                Title = "Latest",
                Url = "template://category/latest",
                Poster = "https://placehold.co/600x320/334155/ffffff?text=Latest"
            }
        ]);
    }

    public override Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        var categoryName = category.Title;
        var items = Items
            .Where(item => string.Equals(item.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase))
            .Skip((Math.Max(1, pageNumber) - 1) * 24)
            .Take(24)
            .Select(ClonePageItem)
            .ToList();

        return Task.FromResult<List<PageItemModel>?>(items.Count == 0 ? null : items);
    }

    public override Task<List<PageItemModel>?> GetSearchResults(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<List<PageItemModel>?>(null);
        }

        var results = Items
            .Where(item => item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(ClonePageItem)
            .ToList();

        return Task.FromResult<List<PageItemModel>?>(results.Count == 0 ? null : results);
    }

    public override Task<MediaInfoModel?> GetMediaInfo(PageItemModel pageItem)
    {
        var isSeries = pageItem.Url.Contains("/series/", StringComparison.OrdinalIgnoreCase);

        var mediaInfo = new MediaInfoModel
        {
            Title = pageItem.Title,
            Url = pageItem.Url,
            Poster = pageItem.Poster,
            Backdrop = "https://placehold.co/1280x720/111827/ffffff?text=Manitux",
            Description = "This is example metadata returned by PluginTemplate. Replace it with scraped or API-backed data.",
            Tags = isSeries ? "Drama, Adventure" : "Action, Sample",
            Rating = pageItem.Rating,
            Year = pageItem.Year,
            Duration = isSeries ? "45" : "120",
            Actors = "Ada Lovelace, Alan Turing",
            Country = "US",
            VideoSources = isSeries
                ? null
                : [CreateSampleVideoSource("Main Source", "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8")],
            Episodes = isSeries
                ? CreateSampleEpisodes(pageItem.Url)
                : null,
            RelatedVideos = Items
                .Where(item => item.Url != pageItem.Url)
                .Select(item => new RelatedVideoModel
                {
                    Title = item.Title,
                    Url = item.Url,
                    Poster = item.Poster
                })
                .ToList()
        };

        return Task.FromResult<MediaInfoModel?>(mediaInfo);
    }

    public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return Task.FromResult<VideoSourceModel?>(videoSource);
    }

    private static List<EpisodeModel> CreateSampleEpisodes(string seriesUrl)
    {
        return
        [
            new()
            {
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Title = "Episode 1",
                Url = $"{seriesUrl}/season-1/episode-1",
                Description = "Template episode one."
            },
            new()
            {
                SeasonNumber = 1,
                EpisodeNumber = 2,
                Title = "Episode 2",
                Url = $"{seriesUrl}/season-1/episode-2",
                Description = "Template episode two."
            }
        ];
    }

    private static VideoSourceModel CreateSampleVideoSource(string name, string url)
    {
        return new VideoSourceModel
        {
            Name = name,
            Url = url,
            Headers =
            [
                new HeaderModel
                {
                    Name = "User-Agent",
                    Value = "Mozilla/5.0"
                }
            ],
            Subtitles =
            [
                new SubtitleModel
                {
                    Id = "en",
                    Name = "English",
                    Url = "https://test-streams.mux.dev/x36xhzz/subtitles.m3u8"
                }
            ]
        };
    }

    private static PageItemModel ClonePageItem(PageItemModel item)
    {
        return new PageItemModel
        {
            CategoryName = item.CategoryName,
            Title = item.Title,
            Url = item.Url,
            Poster = item.Poster,
            Rating = item.Rating,
            Year = item.Year
        };
    }
}
