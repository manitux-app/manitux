using System.Text.Json.Serialization;

namespace Manitux.Seyirturk;

internal sealed class SeyirturkMainResponse
{
    [JsonPropertyName("main")]
    public List<SeyirturkMainItem>? Main { get; set; }
}

internal sealed class SeyirturkMainItem
{
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? Icon { get; set; }
    public string? Backdrop { get; set; }
    public string? Summary { get; set; }
}

internal sealed class SeyirturkMoviesResponse
{
    [JsonPropertyName("movies")]
    public List<SeyirturkMovieItem>? Movies { get; set; }
}

internal sealed class SeyirturkMovieItem
{
    public string? ID { get; set; }
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? Backdrop { get; set; }
    public string? Summary { get; set; }
    public string? IMDBScore { get; set; }
    public string? ReleaseDate { get; set; }
    public string? Runtime { get; set; }
    public string? Genres { get; set; }
    public string? Link { get; set; }
    public string? Type { get; set; }
    public string? Language { get; set; }
    public string? isAdult { get; set; }
}

internal sealed class SeyirturkLinksResponse
{
    [JsonPropertyName("links")]
    public List<SeyirturkLinkItem>? Links { get; set; }

    public string? hasTrailer { get; set; }
    public string? IMDB { get; set; }
}

internal sealed class SeyirturkLinkItem
{
    public string? Name { get; set; }
    public string? name { get; set; }
    public string? Link { get; set; }
    public string? Provider { get; set; }
    public string? MainProvider { get; set; }
    public string? Image { get; set; }
    public string? Backdrop { get; set; }
    public string? isTurkish { get; set; }
    public string? isForeign { get; set; }
    public string? E_ID { get; set; }
    public string? Season { get; set; }
    public string? Episode { get; set; }
    public string? IMDB { get; set; }
    public string? hasTrailer { get; set; }
}

internal sealed class SeyirturkEpisodesResponse
{
    [JsonPropertyName("episodes")]
    public List<SeyirturkEpisodeItem>? Episodes { get; set; }

    public string? hasTrailer { get; set; }
    public string? IMDB { get; set; }
}

internal sealed class SeyirturkEpisodeItem
{
    public string? ID { get; set; }
    public string? E_ID { get; set; }
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? Backdrop { get; set; }
    public string? Summary { get; set; }
    public string? Season { get; set; }
    public string? Episode { get; set; }
    public string? IMDBScore { get; set; }
    public string? ReleaseDate { get; set; }
    public string? Genres { get; set; }
}
