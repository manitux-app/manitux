# PluginTemplate

`PluginTemplate` is a starter Manitux plugin project for plugin developers.

Build:

```text
dotnet build Misc/Plugins/PluginTemplate/PluginTemplate.csproj -c Release
```

Publish the compiled DLL from:

```text
Misc/Plugins/PluginTemplate/bin/Release/net10.0/PluginTemplate.dll
```

Then point your remote `plugins.json` entry to that DLL:

```json
{
  "url": "https://raw.githubusercontent.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY/builds/plugins/PluginTemplate.dll",
  "status": 1,
  "version": 1,
  "apiVersion": 1,
  "name": "Plugin Template Package",
  "internalName": "PluginTemplate",
  "authors": ["Your Name"],
  "description": "A starter Manitux plugin DLL.",
  "plugins": [
    {
      "name": "Plugin Template",
      "internalName": "plugin.template",
      "description": "A starter Manitux plugin.",
      "language": "en",
      "iconUrl": "https://raw.githubusercontent.com/YOUR_GITHUB_USER/YOUR_PLUGIN_REPOSITORY/builds/icons/plugin-template.png",
      "isAdult": false
    }
  ]
}
```

Replace the sample data in `PluginTemplate.cs` with real categories, list parsing, media metadata, and video source extraction logic.
