using Manitux.Core.Models;
using Manitux.Models;

namespace Manitux.Services.Favorites;

public interface IFavoritesService
{
    Task<List<FavoriteItemModel>> GetFavoritesAsync(CancellationToken cancellationToken = default);
    Task<List<FavoriteItemModel>> SearchAsync(string? query, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(PageItemModel pageItem, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(PageItemModel pageItem, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(PageItemModel pageItem, CancellationToken cancellationToken = default);
}
