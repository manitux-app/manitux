namespace Manitux.Core.Services.Storage;

public static class AppDataPath
{
    private const string AppFolderName = "Manitux";

    public static string GetRootPath()
    {
        if (OperatingSystem.IsAndroid())
        {
            return GetRequiredFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                GetRequiredFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                AppFolderName);
        }

        if (OperatingSystem.IsLinux())
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (IsUsableXdgDataHome(xdgDataHome))
            {
                return Path.Combine(xdgDataHome!, AppFolderName);
            }

            return Path.Combine(
                GetRequiredFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share",
                AppFolderName);
        }

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                GetRequiredFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            return Path.Combine(localAppData, AppFolderName);
        }

        return Path.Combine(
            GetRequiredFolderPath(Environment.SpecialFolder.UserProfile),
            ".local",
            "share",
            AppFolderName);
    }

    public static string GetDataPath(params string[] paths)
    {
        return Path.Combine([GetRootPath(), "data", .. paths]);
    }

    public static string GetAppPath(params string[] paths)
    {
        return GetDataPath(["app", .. paths]);
    }

    public static string GetPluginsPath(params string[] paths)
    {
        return GetDataPath(["plugins", .. paths]);
    }

    public static string GetBundledDataPath(params string[] paths)
    {
        return Path.Combine([AppContext.BaseDirectory, "data", .. paths]);
    }

    private static string GetRequiredFolderPath(Environment.SpecialFolder folder)
    {
        var path = Environment.GetFolderPath(folder);
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        throw new InvalidOperationException($"Could not resolve {folder} path.");
    }

    private static bool IsUsableXdgDataHome(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            return true;
        }

        var snapCodePath = Path.Combine(userProfile, "snap", "code");
        return !fullPath.StartsWith(snapCodePath, StringComparison.Ordinal);
    }
}
