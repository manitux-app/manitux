using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Manitux.Core.Application;

namespace Manitux.Core.Services.Plugins;

public sealed class RemotePluginRepository
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

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

    [JsonPropertyName("plugins")]
    public List<RemotePluginManifest>? Plugins { get; set; }

    [JsonIgnore]
    public string? PackageInternalName { get; set; }

    [JsonIgnore]
    public string? PackageName { get; set; }

    [JsonIgnore]
    public string? PluginListUrl { get; set; }

    [JsonIgnore]
    public bool IsInstalled { get; set; }

    [JsonIgnore]
    public AppStrings? Strings { get; set; }

    [JsonIgnore]
    public string LanguageDisplay => Language?.ToUpperInvariant() ?? string.Empty;
}

public sealed class ManagedRemotePlugin
{
    public string Name { get; set; } = string.Empty;
    public string InternalName { get; set; } = string.Empty;
    public int Version { get; set; }
    public int ApiVersion { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string PackageInternalName { get; set; } = string.Empty;
    public string? PackageName { get; set; }
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
    public string? IconUrl { get; set; }
    public int ManifestVersion { get; set; }
    public List<ManagedRemotePluginList> PluginLists { get; set; } = [];
    public DateTimeOffset AddedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

[JsonConverter(typeof(ManagedRemotePluginListJsonConverter))]
public sealed class ManagedRemotePluginList
{
    public string Url { get; set; } = string.Empty;
    public List<RemotePluginManifest> Packages { get; set; } = [];
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

public sealed class RemotePluginUpdateCheckResult
{
    public ManagedRemotePlugin InstalledPlugin { get; init; } = new();
    public RemotePluginManifest? LatestManifest { get; init; }
    public bool HasUpdate { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ManagedRemotePluginListJsonConverter : JsonConverter<ManagedRemotePluginList>
{
    public override ManagedRemotePluginList Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ManagedRemotePluginList
            {
                Url = reader.GetString() ?? string.Empty
            };
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var list = new ManagedRemotePluginList();

        if (document.RootElement.TryGetProperty("url", out var url)
            && url.ValueKind == JsonValueKind.String)
        {
            list.Url = url.GetString() ?? string.Empty;
        }

        if (document.RootElement.TryGetProperty("updatedAt", out var updatedAt)
            && updatedAt.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(updatedAt.GetString(), out var parsedUpdatedAt))
        {
            list.UpdatedAt = parsedUpdatedAt;
        }

        if (document.RootElement.TryGetProperty("packages", out var packages)
            && packages.ValueKind == JsonValueKind.Array)
        {
            list.Packages = JsonSerializer.Deserialize<List<RemotePluginManifest>>(
                                packages.GetRawText(),
                                options)
                            ?? [];
        }

        return list;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ManagedRemotePluginList value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("url", value.Url);
        writer.WritePropertyName("packages");
        JsonSerializer.Serialize(writer, value.Packages, options);
        writer.WriteString("updatedAt", value.UpdatedAt);
        writer.WriteEndObject();
    }
}
