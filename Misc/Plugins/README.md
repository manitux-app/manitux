# Manitux Remote Plugin Repository Example

This folder contains example files for a GitHub-hosted Manitux plugin repository.

Expected GitHub layout:

```text
repo.json
plugins.json
plugins/
  TurkishProviders.dll
icons/
  repository.png
  turkish-providers.png
```

`repo.json` points to one or more plugin list files through `pluginLists`.
It can also include an `iconUrl` for the repository row shown in Manitux.
`plugins.json` contains downloadable DLL package entries. Each DLL package can expose
one or more Manitux plugins through its `plugins` array.

Example:

```json
[
  {
    "url": "plugins/TurkishProviders.dll",
    "status": 1,
    "version": 1,
    "apiVersion": 1,
    "name": "Turkish Providers",
    "internalName": "TurkishProviders",
    "plugins": [
      {
        "name": "DiziBox",
        "internalName": "plugin.dizibox",
        "description": "DiziBox series provider.",
        "language": "tr",
        "iconUrl": "icons/dizibox.png",
        "isAdult": false
      },
      {
        "name": "DiziPal",
        "internalName": "plugin.dizipal",
        "description": "DiziPal series provider.",
        "language": "tr",
        "iconUrl": "icons/dizipal.png",
        "isAdult": false
      }
    ]
  }
]
```

For backward compatibility, a DLL package without a `plugins` array is treated as a
single plugin entry.

Status values follow the Cloudstream-style convention:

```text
0 = Down
1 = Ok
2 = Slow
3 = Beta only
```

Replace `YOUR_GITHUB_USER`, `YOUR_PLUGIN_REPOSITORY`, branch name, plugin file names, and metadata before publishing.
