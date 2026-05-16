using System;
using Manitux.Core.Models;

namespace Manitux.Models;

public class FavoriteItemModel
{
    public required string Title { get; set; }
    public required string Url { get; set; }
    public string? CategoryName { get; set; }
    public string? Poster { get; set; }
    public string? Rating { get; set; }
    public string? Year { get; set; }
    public string? PluginId { get; set; }
    public string? PluginName { get; set; }
    public string? PluginFavicon { get; set; }
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    public PageItemModel ToPageItem()
    {
        return new PageItemModel
        {
            Title = Title,
            Url = Url,
            CategoryName = CategoryName,
            Poster = Poster,
            Rating = Rating,
            Year = Year,
            PluginId = PluginId,
            PluginName = PluginName,
            PluginFavicon = PluginFavicon
        };
    }

    public static FavoriteItemModel FromPageItem(PageItemModel pageItem)
    {
        return new FavoriteItemModel
        {
            Title = pageItem.Title,
            Url = pageItem.Url,
            CategoryName = pageItem.CategoryName,
            Poster = pageItem.Poster,
            Rating = pageItem.Rating,
            Year = pageItem.Year,
            PluginId = pageItem.PluginId,
            PluginName = pageItem.PluginName,
            PluginFavicon = pageItem.PluginFavicon
        };
    }
}
