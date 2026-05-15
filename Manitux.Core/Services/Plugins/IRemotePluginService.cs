using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Manitux.Core.Services.Plugins;

public interface IRemotePluginService
{
    Task<ManagedRemoteRepository> AddRepositoryAsync(string urlOrShortCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemotePluginManifest>> GetRepositoryPluginsAsync(string repositoryUrlOrShortCode, CancellationToken cancellationToken = default);
    Task<RemotePluginInstallResult> InstallAsync(string urlOrShortCode, string? internalName = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string internalName, CancellationToken cancellationToken = default);
    Task<RemotePluginInstallResult> UpdateAsync(string internalName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemotePluginInstallResult>> UpdateAllAsync(CancellationToken cancellationToken = default);
    Task<RemotePluginSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
}
