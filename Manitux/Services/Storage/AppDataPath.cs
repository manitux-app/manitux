namespace Manitux.Services.Storage;

public static class AppDataPath
{
    public static string GetDataPath_Old(params string[] paths)
    {
        var baseDir = OperatingSystem.IsAndroid()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : AppContext.BaseDirectory;

        return Path.Combine([baseDir, "data", .. paths]);
    }

    public static string GetDataPath(params string[] paths)
    {
        return Manitux.Core.Services.Storage.AppDataPath.GetDataPath(paths);
    }

    public static string GetAppPath(params string[] paths)
    {
        return Manitux.Core.Services.Storage.AppDataPath.GetAppPath(paths);
    }

    public static string GetPluginsPath(params string[] paths)
    {
        return Manitux.Core.Services.Storage.AppDataPath.GetPluginsPath(paths);
    }

    public static string GetBundledDataPath(params string[] paths)
    {
        return Manitux.Core.Services.Storage.AppDataPath.GetBundledDataPath(paths);
    }
}
