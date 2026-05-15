using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Manitux.Core.Services.Plugins;

public sealed class RemotePluginRepository
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("manifestVersion")]
    public int ManifestVersion { get; set; }

    [JsonPropertyName("pluginLists")]
    public List<string> PluginLists { get; set; } = [];
}

public sealed class RemotePluginManifest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("apiVersion")]
    public int ApiVersion { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("internalName")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("authors")]
    public List<string> Authors { get; set; } = [];

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("repositoryUrl")]
    public string? RepositoryUrl { get; set; }

    [JsonPropertyName("tvTypes")]
    public List<string>? TvTypes { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("isAdult")]
    public bool? IsAdult { get; set; }
}

public sealed class ManagedRemotePlugin
{
    public string Name { get; set; } = string.Empty;
    public string InternalName { get; set; } = string.Empty;
    public int Version { get; set; }
    public int ApiVersion { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? RepositoryUrl { get; set; }
    public string? PluginListUrl { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTimeOffset InstalledAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public RemotePluginManifest? Manifest { get; set; }
}

public sealed class ManagedRemoteRepository
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ManifestVersion { get; set; }
    public List<string> PluginLists { get; set; } = [];
    public DateTimeOffset AddedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RemotePluginSettings
{
    public List<ManagedRemoteRepository> Repositories { get; set; } = [];
    public List<ManagedRemotePlugin> InstalledPlugins { get; set; } = [];
}

public sealed class RemotePluginInstallResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public ManagedRemotePlugin? Plugin { get; init; }
}
