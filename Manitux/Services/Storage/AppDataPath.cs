namespace Manitux.Services.Storage;

public static class AppDataPath
{
    public static string GetDataPath(params string[] paths)
    {
        var baseDir = OperatingSystem.IsAndroid()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : AppContext.BaseDirectory;

        return Path.Combine([baseDir, "data", .. paths]);
    }
}
