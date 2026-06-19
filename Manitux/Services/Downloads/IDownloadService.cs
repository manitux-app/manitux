using Manitux.Core.Models;
using Manitux.Models;

namespace Manitux.Services.Downloads;

public interface IDownloadService
{
    event EventHandler? DownloadsChanged;

    IReadOnlyList<DownloadItemModel> Downloads { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<DownloadItemModel> AddAsync(VideoSourceModel source, string fileName, string filePath, CancellationToken cancellationToken = default);
    Task StartAsync(string id, CancellationToken cancellationToken = default);
    Task PauseAsync(string id, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
