using System.Reflection;
using System.Runtime.Loader;

namespace CodeLogic.Framework.Application.Plugins;

/// <summary>
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
/// For Android
/// Isolated AssemblyLoadContext for each plugin.
/// Allows true hot-unload by releasing the assembly from memory.
/// isCollectible=true enables GC collection after Unload().
/// </summary>
public sealed class PluginLoadContextForAndroid : AssemblyLoadContext
{
    /// <summary>
    /// Android için özel yükleme baðlamý. 
    /// Baðýmlýlýklarýn ana uygulamada olduðunu varsayar.
    /// </summary>
    public PluginLoadContextForAndroid(string pluginPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
    {
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Baðýmlýlýðý önce ana uygulama (Default) içerisinde ara.
        // Senin "tüm baðýmlýlýklar ana uygulamada mevcut" kuralýný bu satýr iþletir.
        var sharedAssembly = Default.Assemblies.FirstOrDefault(a =>
            string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));

        if (sharedAssembly != null)
            return sharedAssembly;

        // Eðer ana uygulamada yoksa, null dönerek runtime'ýn standart aramasýna býrakýyoruz.
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // Android'de unmanaged kütüphaneler genellikle sistem tarafýndan veya 
        // ana uygulama kütüphane klasöründen yüklenir.
        return IntPtr.Zero;
    }
}


