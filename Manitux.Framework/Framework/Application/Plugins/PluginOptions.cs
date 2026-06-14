namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Configuration options for the plugin manager.
/// </summary>
public sealed class PluginOptions
{
    /// <summary>Base directory where plugin folders live. Default: {FrameworkRoot}/Plugins</summary>
    public string PluginsDirectory { get; set; } = "CodeLogic/Plugins";

    /// <summary>Enable auto-reload when plugin DLL changes on disk.</summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>Enable filesystem watching for DLL changes.</summary>
    public bool WatchForChanges { get; set; } = false;

    /// <summary>Debounce interval to avoid double-reloads on rapid file changes.</summary>
    public TimeSpan ReloadDebounce { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Optional per-plugin load filter. Return false to keep a discovered plugin disabled.
    /// </summary>
    public Func<PluginManifest, string, bool>? IsPluginEnabled { get; set; }
}
