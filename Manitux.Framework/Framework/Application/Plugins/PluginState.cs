namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Tracks the lifecycle state of a loaded plugin.
/// Mirrors <see cref="Libraries.LibraryState"/> but with an additional <see cref="Discovered"/> state
/// for plugins found on disk but not yet loaded into memory.
/// </summary>
public enum PluginState
{
    /// <summary>Found on disk (by <see cref="PluginManager.DiscoverAsync"/>) but not yet loaded into memory.</summary>
    Discovered,

    /// <summary>Assembly loaded into an isolated <c>PluginLoadContext</c> and the plugin instance created.</summary>
    Loaded,

    /// <summary><see cref="IPlugin.OnConfigureAsync"/> completed and config/localization files were loaded.</summary>
    Configured,

    /// <summary><see cref="IPlugin.OnInitializeAsync"/> completed successfully.</summary>
    Initialized,

    /// <summary><see cref="IPlugin.OnStartAsync"/> completed — the plugin is fully operational.</summary>
    Started,

    /// <summary><see cref="IPlugin.OnUnloadAsync"/> completed — the plugin has been unloaded cleanly.</summary>
    Stopped,

    /// <summary>An exception occurred during any lifecycle phase. The plugin is not operational.</summary>
    Failed
}
