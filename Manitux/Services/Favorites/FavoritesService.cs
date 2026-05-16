using System;
using System.Text.Json;
using Manitux.Core.Models;
using Manitux.Models;

namespace Manitux.Services.Favorites;

public class FavoritesService : IFavoritesService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<List<FavoriteItemModel>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadFavoritesAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<FavoriteItemModel>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        query = query?.Trim();
        var favorites = await GetFavoritesAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(query))
        {
            return favorites;
        }

        return favorites
            .Where(x => Contains(x.Title, query)
                        || Contains(x.Year, query)
                        || Contains(x.Rating, query)
                        || Contains(x.PluginName, query)
                        || Contains(x.CategoryName, query))
            .ToList();
    }

    public async Task<bool> ExistsAsync(PageItemModel pageItem, CancellationToken cancellationToken = default)
    {
        var favorites = await GetFavoritesAsync(cancellationToken);
        return favorites.Any(x => IsSameFavorite(x, pageItem));
    }

    public async Task AddOrUpdateAsync(PageItemModel pageItem, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var favorites = await ReadFavoritesAsync(cancellationToken);
            var existing = favorites.FirstOrDefault(x => IsSameFavorite(x, pageItem));

            var favorite = FavoriteItemModel.FromPageItem(pageItem);
            if (existing is null)
            {
                favorite.AddedAt = DateTimeOffset.UtcNow;
                favorites.Add(favorite);
            }
            else
            {
                favorite.AddedAt = existing.AddedAt;
                var index = favorites.IndexOf(existing);
                favorites[index] = favorite;
            }

            await WriteFavoritesAsync(favorites, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> RemoveAsync(PageItemModel pageItem, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var favorites = await ReadFavoritesAsync(cancellationToken);
            var removed = favorites.RemoveAll(x => IsSameFavorite(x, pageItem)) > 0;
            if (removed)
            {
                await WriteFavoritesAsync(favorites, cancellationToken);
            }

            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<FavoriteItemModel>> ReadFavoritesAsync(CancellationToken cancellationToken)
    {
        var path = GetFavoritesFilePath();
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<FavoriteItemModel>>(stream, _jsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteFavoritesAsync(List<FavoriteItemModel> favorites, CancellationToken cancellationToken)
    {
        var path = GetFavoritesFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, favorites.OrderByDescending(x => x.AddedAt).ToList(), _jsonOptions, cancellationToken);
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsSameFavorite(FavoriteItemModel favorite, PageItemModel pageItem)
    {
        return string.Equals(favorite.Url, pageItem.Url, StringComparison.OrdinalIgnoreCase)
               && string.Equals(favorite.PluginId, pageItem.PluginId, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFavoritesFilePath()
    {
        var baseDir = OperatingSystem.IsAndroid()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : AppContext.BaseDirectory;

        return Path.Combine(baseDir, "data", "favorites", "favorites.json");
    }
}
