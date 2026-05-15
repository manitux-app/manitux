# Manitux Remote Plugin Repository Example

This folder contains example files for a GitHub-hosted Manitux plugin repository.

Expected GitHub layout:

```text
repo.json
plugins.json
plugins/
  ExamplePlugin.dll
icons/
  example.png
```

`repo.json` points to one or more plugin list files through `pluginLists`.
`plugins.json` contains plugin entries and direct URLs to downloadable plugin files.
Manitux plugins are published as compiled `.dll` files.

Status values follow the Cloudstream-style convention:

```text
0 = Down
1 = Ok
2 = Slow
3 = Beta only
```

Replace `YOUR_GITHUB_USER`, `YOUR_PLUGIN_REPOSITORY`, branch name, plugin file names, and metadata before publishing.
