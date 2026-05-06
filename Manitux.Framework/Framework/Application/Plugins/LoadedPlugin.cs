using System.Reflection;
using System.Runtime.Loader;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// Represents a plugin that has been loaded into the framework.
/// </summary>
public sealed class LoadedPlugin
{
    /// <summary>The plugin instance.</summary>
    public required IPlugin Instance { get; init; }
    /// <summary>The plugin's manifest metadata.</summary>
    public required PluginManifest Manifest { get; init; }
    /// <summary>The file path of the plugin assembly.</summary>
    public required string AssemblyPath { get; init; }
    /// <summary>The isolated assembly load context for this plugin.</summary>
    public required AssemblyLoadContext LoadContext { get; init; }
    //public required PluginLoadContext LoadContext { get; init; }
    /// <summary>Weak reference to the load context for GC tracking after unload.</summary>
    public WeakReference? WeakReference { get; init; }
    /// <summary>UTC timestamp when the plugin was loaded.</summary>
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>The plugin's runtime context, set after configuration.</summary>
    public PluginContext? Context { get; set; }
    /// <summary>The current lifecycle state of the plugin.</summary>
    public PluginState State { get; set; } = PluginState.Loaded;
    /// <summary>The exception that caused the plugin to fail, if any.</summary>
    public Exception? FailureException { get; set; }
}
