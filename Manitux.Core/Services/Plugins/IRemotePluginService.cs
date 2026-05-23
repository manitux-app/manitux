using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Manitux.Core.Services.Plugins;

public interface IRemotePluginService
{
    Task<ManagedRemoteRepository> AddRepositoryAsync(string urlOrShortCode, CancellationToken cancellationToken = default);
    Task<bool> RemoveRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemotePluginManifest>> GetRepositoryPluginsAsync(string repositoryUrlOrShortCode, CancellationToken cancellationToken = default);
    Task<RemotePluginInstallResult> InstallAsync(string urlOrShortCode, string? internalName = null, CancellationToken cancellationToken = default);
    Task<RemotePluginInstallResult> InstallAsync(RemotePluginManifest manifest, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string internalName, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string internalName, string? packageInternalName, CancellationToken cancellationToken = default);
    Task<RemotePluginUpdateCheckResult> CheckUpdateAsync(string internalName, CancellationToken cancellationToken = default);
    Task<RemotePluginUpdateCheckResult> CheckUpdateAsync(string internalName, string? packageInternalName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemotePluginUpdateCheckResult>> CheckUpdatesAsync(CancellationToken cancellationToken = default);
    Task<RemotePluginInstallResult> UpdateAsync(string internalName, CancellationToken cancellationToken = default);
    Task<RemotePluginInstallResult> UpdateAsync(string internalName, string? packageInternalName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RemotePluginInstallResult>> UpdateAllAsync(CancellationToken cancellationToken = default);
    Task<RemotePluginSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
}
