using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CodeLogic.Core.Logging;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;

namespace Manitux.Core.Plugins;
public class InternetArchive : PluginBase
{
    private const string MainBaseUrl = "https://archive.org";
    private const string AdvancedSearchUrl = "https://archive.org/advancedsearch.php";
    private const string MetadataBaseUrl = "https://archive.org/metadata";
    private const int PageSize = 26;

    private static readonly string[] VideoExtensions =
    [
        ".mp4",
        ".m4v",
        ".webm",
        ".ogv",
        ".mkv",
        ".mov"
    ];

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "plugin.internetarchive",
        Name = "Internet Archive",
        Version = "1.0.0",
        Description = "Internet Archive is a non-profit library of millions of free texts, movies, software, music, websites, and more.",
        Author = "Team Manitux"
    };

    public override PluginConfig Config { get; set; } = new()
    {
        MainUrl = MainBaseUrl,
        Favicon = "https://www.google.com/s2/favicons?domain=archive.org&sz=64",
        Language = "en"
    };

    public override async Task<List<CategoryModel>?> GetCategories()
    {
        return await Task.FromResult(new List<CategoryModel>
        {
            new() { Title = "Movies", Url = BuildCategoryUrl("movies") },
            new() { Title = "Feature Films", Url = BuildCategoryUrl("feature_films") },
            new() { Title = "Television", Url = BuildCategoryUrl("television") },
            new() { Title = "Animation", Url = BuildCategoryUrl("animationandcartoons") },
            new() { Title = "Short Films", Url = BuildCategoryUrl("short_films") }
        });
    }

    public override async Task<List<PageItemModel>?> GetPageItems(int pageNumber, CategoryModel category)
    {
        try
        {
            var collection = ParseCategoryCollection(category.Url);
            if (string.IsNullOrWhiteSpace(collection)) return null;

            var query = $"mediatype:movies AND collection:{collection}";
            return await SearchArchive(query, Math.Max(1, pageNumber), category.Title);
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
            var identifier = GetIdentifier(pageItem.Url);
            if (string.IsNullOrWhiteSpace(identifier)) return null;

            var response = await HttpGet($"{MetadataBaseUrl}/{Uri.EscapeDataString(identifier)}", referer: MainUrl, headers: GetHeaders());
            if (!LooksLikeJson(response)) return null;

            var detail = JsonSerializer.Deserialize<ArchiveMetadataResponse>(response!);
            if (detail?.Metadata is null) return null;

            var metadata = detail.Metadata;
            var videoFile = detail.Files?
                .Where(IsPlayableVideo)
                .OrderBy(GetVideoPreference)
                .FirstOrDefault();

            var title = FirstText(metadata.Title) ?? pageItem.Title;
            var poster = BuildPosterUrl(identifier);
            var sourceUrl = videoFile is null ? BuildItemUrl(identifier) : BuildDownloadUrl(identifier, videoFile.Name!);
            var year = GetYear(FirstNonEmpty(
                FirstText(metadata.Year),
                FirstText(metadata.Date),
                FirstText(metadata.PublicDate)));
            var duration = FirstNonEmpty(
                FirstText(metadata.Runtime),
                FirstText(metadata.Duration),
                FormatDuration(videoFile?.Length));
            var people = JoinText(metadata.Creator, metadata.Contributor, metadata.Publisher);
            var country = FirstNonEmpty(
                JoinText(metadata.Country),
                JoinText(metadata.Coverage),
                JoinText(metadata.Spatial));

            return new MediaInfoModel
            {
                Title = title,
                Url = BuildItemUrl(identifier),
                Description = FirstText(metadata.Description),
                Poster = poster,
                Backdrop = poster,
                Tags = JoinText(metadata.Subject),
                Year = year,
                Duration = duration,
                Actors = people,
                Country = country,
                VideoSources = new List<VideoSourceModel>
                {
                    new()
                    {
                        Name = videoFile?.Format ?? "Internet Archive",
                        Url = sourceUrl,
                        Referer = MainUrl
                    }
                }
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

            var archiveQuery = $"mediatype:movies AND ({query})";
            return await SearchArchive(archiveQuery, 1, "Search");
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
            return null;
        }
    }

    public override async Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
    {
        return await Task.FromResult(videoSource);
    }

    private string MainUrl => string.IsNullOrWhiteSpace(Config.MainUrl)
        ? MainBaseUrl
        : Config.MainUrl.TrimEnd('/');

    private async Task<List<PageItemModel>?> SearchArchive(string query, int page, string categoryName)
    {
        var url = BuildSearchUrl(query, page);
        var response = await HttpGet(url, referer: MainUrl, headers: GetHeaders());
        if (!LooksLikeJson(response)) return null;

        var results = JsonSerializer.Deserialize<ArchiveSearchResponse>(response!);
        var items = results?.Response?.Docs?
            .Select(x => ToPageItem(x, categoryName))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        return items is { Count: > 0 } ? items : null;
    }

    private static Dictionary<string, string> GetHeaders()
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "application/json, text/plain, */*",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"
        };
    }

    private static PageItemModel? ToPageItem(ArchiveSearchDoc item, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(item.Identifier)) return null;

        var title = FirstText(item.Title) ?? item.Identifier;
        return new PageItemModel
        {
            CategoryName = categoryName,
            Title = title,
            Url = BuildItemUrl(item.Identifier),
            Poster = BuildPosterUrl(item.Identifier),
            Year = GetYear(FirstText(item.Date)),
            Rating = item.Downloads > 0 ? item.Downloads.ToString() : string.Empty
        };
    }

    private static string BuildSearchUrl(string query, int page)
    {
        var fields = new[]
        {
            "identifier",
            "title",
            "date",
            "downloads"
        };

        var fieldQuery = string.Join("&", fields.Select(x => $"fl[]={Uri.EscapeDataString(x)}"));
        return $"{AdvancedSearchUrl}?q={Uri.EscapeDataString(query)}&{fieldQuery}&sort[]=downloads desc&rows={PageSize}&page={page}&output=json";
    }

    private static string BuildCategoryUrl(string collection)
    {
        return $"archive://{collection}";
    }

    private static string BuildItemUrl(string identifier)
    {
        return $"{MainBaseUrl}/details/{Uri.EscapeDataString(identifier)}";
    }

    private static string BuildPosterUrl(string identifier)
    {
        return $"{MainBaseUrl}/services/img/{Uri.EscapeDataString(identifier)}";
    }

    private static string BuildDownloadUrl(string identifier, string fileName)
    {
        return $"{MainBaseUrl}/download/{Uri.EscapeDataString(identifier)}/{Uri.EscapeDataString(fileName).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}";
    }

    private static string ParseCategoryCollection(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        const string prefix = "archive://";
        return url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? url[prefix.Length..]
            : url;
    }

    private static string? GetIdentifier(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var match = Regex.Match(url, @"/details/(?<id>[^/?#]+)", RegexOptions.IgnoreCase);
        return match.Success ? Uri.UnescapeDataString(match.Groups["id"].Value) : null;
    }

    private static bool IsPlayableVideo(ArchiveFile file)
    {
        if (string.IsNullOrWhiteSpace(file.Name)) return false;

        var extension = Path.GetExtension(file.Name);
        if (VideoExtensions.Any(x => string.Equals(x, extension, StringComparison.OrdinalIgnoreCase))) return true;

        return file.Format?.Contains("MPEG4", StringComparison.OrdinalIgnoreCase) == true
            || file.Format?.Contains("h.264", StringComparison.OrdinalIgnoreCase) == true
            || file.Format?.Contains("WebM", StringComparison.OrdinalIgnoreCase) == true
            || file.Format?.Contains("Ogg Video", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static int GetVideoPreference(ArchiveFile file)
    {
        var name = file.Name ?? string.Empty;
        var extension = Path.GetExtension(name);

        if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(extension, ".m4v", StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(extension, ".webm", StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(extension, ".ogv", StringComparison.OrdinalIgnoreCase)) return 3;

        return 10;
    }

    private static string? FirstText(JsonElement? element)
    {
        if (element is null) return null;

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.String => EmptyToNull(value.GetString()),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.Array => value.EnumerateArray()
                .Select(x => FirstText(x))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
            _ => null
        };
    }

    private static string? JoinText(JsonElement? element)
    {
        if (element is null) return null;

        var value = element.Value;
        if (value.ValueKind == JsonValueKind.Array)
        {
            var values = value.EnumerateArray()
                .Select(x => FirstText(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Take(12);

            return string.Join(", ", values);
        }

        return FirstText(value);
    }

    private static string? JoinText(params JsonElement?[] elements)
    {
        var values = elements
            .SelectMany(ReadTextValues)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(12);

        return EmptyToNull(string.Join(", ", values));
    }

    private static IEnumerable<string?> ReadTextValues(JsonElement? element)
    {
        if (element is null) yield break;

        var value = element.Value;
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                var text = FirstText(item);
                if (!string.IsNullOrWhiteSpace(text)) yield return text;
            }

            yield break;
        }

        var single = FirstText(value);
        if (!string.IsNullOrWhiteSpace(single)) yield return single;
    }

    private static string? FirstText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => EmptyToNull(element.GetString()),
            JsonValueKind.Number => element.ToString(),
            _ => null
        };
    }

    private static string? GetYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = Regex.Match(value, @"\b(?<year>\d{4})\b");
        return match.Success ? match.Groups["year"].Value : null;
    }

    private static string? FormatDuration(JsonElement? length)
    {
        var value = FirstText(length);
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (TimeSpan.TryParse(value, out var parsed))
        {
            return parsed.TotalMinutes >= 1 ? Math.Round(parsed.TotalMinutes).ToString() : null;
        }

        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            return seconds >= 60 ? Math.Round(TimeSpan.FromSeconds(seconds).TotalMinutes).ToString() : null;
        }

        return value;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool LooksLikeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private sealed class ArchiveSearchResponse
    {
        [JsonPropertyName("response")]
        public ArchiveSearchBody? Response { get; set; }
    }

    private sealed class ArchiveSearchBody
    {
        [JsonPropertyName("docs")]
        public List<ArchiveSearchDoc>? Docs { get; set; }
    }

    private sealed class ArchiveSearchDoc
    {
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        [JsonPropertyName("title")]
        public JsonElement? Title { get; set; }

        [JsonPropertyName("date")]
        public JsonElement? Date { get; set; }

        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }
    }

    private sealed class ArchiveMetadataResponse
    {
        [JsonPropertyName("metadata")]
        public ArchiveMetadata? Metadata { get; set; }

        [JsonPropertyName("files")]
        public List<ArchiveFile>? Files { get; set; }
    }

    private sealed class ArchiveMetadata
    {
        [JsonPropertyName("title")]
        public JsonElement? Title { get; set; }

        [JsonPropertyName("description")]
        public JsonElement? Description { get; set; }

        [JsonPropertyName("creator")]
        public JsonElement? Creator { get; set; }

        [JsonPropertyName("contributor")]
        public JsonElement? Contributor { get; set; }

        [JsonPropertyName("publisher")]
        public JsonElement? Publisher { get; set; }

        [JsonPropertyName("subject")]
        public JsonElement? Subject { get; set; }

        [JsonPropertyName("date")]
        public JsonElement? Date { get; set; }

        [JsonPropertyName("publicdate")]
        public JsonElement? PublicDate { get; set; }

        [JsonPropertyName("year")]
        public JsonElement? Year { get; set; }

        [JsonPropertyName("runtime")]
        public JsonElement? Runtime { get; set; }

        [JsonPropertyName("duration")]
        public JsonElement? Duration { get; set; }

        [JsonPropertyName("coverage")]
        public JsonElement? Coverage { get; set; }

        [JsonPropertyName("country")]
        public JsonElement? Country { get; set; }

        [JsonPropertyName("spatial")]
        public JsonElement? Spatial { get; set; }
    }

    private sealed class ArchiveFile
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("length")]
        public JsonElement? Length { get; set; }
    }
}
