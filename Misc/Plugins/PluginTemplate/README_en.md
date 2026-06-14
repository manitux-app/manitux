[Türkçe](README.md)

# PluginTemplate

`PluginTemplate` is a starter Manitux plugin project for plugin developers. It includes example categories, page items, media details, episodes, and video sources.

## Build

Run this command from the repository root:

```bash
dotnet build PluginTemplate/PluginTemplate.csproj -c Release
```

The compiled output is created at:

```text
PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

## Resolving Video Sources

Manitux.Core includes ready-made extractor classes. They are defined under `Manitux.Core/Extractors/` and support services such as Dailymotion, YouTube, Okru, MailRu, Voe, VidMoly, StreamWish, Filemoon, MixDrop, StreamTape, Supervideo, Uqload, DoodStream, and others.

If a `VideoSourceModel.Url` matches the `MainUrl` or `SupportedDomains` value of one of these extractor classes, you can use the `ExtractAsync` method directly from your plugin. `PluginBase.ExtractAsync` finds the matching extractor with `ExtractorManager.GetExtractorByUrl`; when a match exists, it calls that extractor's `ExtractAsync(videoSource, referer)` method, otherwise it returns the existing `VideoSourceModel`.

Example:

```csharp
public override Task<VideoSourceModel?> GetVideoSources(VideoSourceModel videoSource)
{
    return ExtractAsync(videoSource, Config.MainUrl);
}
```

The extractor result may update `Url`, `Referer`, `Headers`, and `Subtitles`. For supported services, prefer the built-in extractor flow before manually parsing embed or share URLs; write custom source-resolution code only for unmatched sources.

## Publish

This repository now keeps published outputs in the `builds/` directory on the `main` branch. To publish the template plugin, copy the compiled DLL under `builds/` and add an entry for the same DLL to `builds/plugins.json`.

Example DLL entry:

```json
{
  "url": "https://raw.githubusercontent.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY/main/builds/PluginTemplate.dll",
  "status": 1,
  "version": 1,
  "apiVersion": 1,
  "authors": ["Your Name"],
  "repositoryUrl": "https://github.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY",
  "plugins": [
    {
      "name": "Plugin Template",
      "internalName": "plugin.template",
      "description": "A starter Manitux plugin.",
      "language": "en",
      "iconUrl": "https://www.google.com/s2/favicons?domain=example.com&sz=64",
      "isAdult": false,
      "tvTypes": ["Movie", "TvSeries"]
    }
  ]
}
```

Replace the sample data in `PluginTemplate.cs` with real category, listing, media metadata, and video source extraction logic. The `PluginManifest.Id` value in code must match the `internalName` value in `builds/plugins.json`.
