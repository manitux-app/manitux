using System.Reflection;
using System.Runtime.Loader;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
/// For Desktop ***
/// Isolated AssemblyLoadContext for each plugin.
/// Allows true hot-unload by releasing the assembly from memory.
/// isCollectible=true enables GC collection after Unload().
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>Creates a new plugin load context for the assembly at the specified path.</summary>
    public PluginLoadContext(string pluginPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var shared = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly =>
                string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (shared is not null)
            return shared;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}


/// <summary>
/// For Android ***
/// Isolated AssemblyLoadContext for each plugin.
/// Allows true hot-unload by releasing the assembly from memory.
/// isCollectible=true enables GC collection after Unload().
/// </summary>
public sealed class PluginLoadContextForAndroid : AssemblyLoadContext
{
    /// <summary>Creates a new plugin load context for the assembly at the specified path.</summary>
    public PluginLoadContextForAndroid(string pluginPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
    {
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // First, look for the dependency within the main application (Default).
        var sharedAssembly = Default.Assemblies.FirstOrDefault(a =>
            string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (sharedAssembly != null)
            return sharedAssembly;

        return null;
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // In Android, unmanaged libraries are usually loaded by the system or
        // from the main application library folder.
        return IntPtr.Zero;
    }
}


