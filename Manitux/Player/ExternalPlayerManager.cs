using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Manitux.Core.Models;
using Manitux.Services.Storage;

namespace Manitux.Player;

public class ExternalPlayerManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public bool Play(int playerNumber, VideoSourceModel source)
    {
        return playerNumber switch
        {
            1 => MpvPlay(source),
            2 => VlcPlay(source),
            _ => false
        };
    }

    public bool MpvPlay(VideoSourceModel source, string? executablePath = null)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.Url))
        {
            throw new ArgumentException("Video source or URL is invalid.");
        }

        var mpvCmd = ResolveSelectedExecutable(executablePath) ?? ResolveMpvExecutable();
        if (mpvCmd is null)
        {
            return false;
        }

        var startInfo = CreateStartInfo(mpvCmd);
        startInfo.ArgumentList.Add(source.Url);

        if (source.Headers is not null && source.Headers.Any())
        {
            var headerList = source.Headers.Select(h => $"{h.Name}: {h.Value}");
            startInfo.ArgumentList.Add($"--http-header-fields={string.Join(",", headerList)}");

            var userAgent = source.Headers.FirstOrDefault(h =>
                h.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
            if (userAgent is not null)
            {
                startInfo.ArgumentList.Add($"--user-agent={userAgent.Value}");
            }
        }

        if (!string.IsNullOrWhiteSpace(source.Referer))
        {
            startInfo.ArgumentList.Add($"--referrer={source.Referer}");
        }

        if (source.Subtitles is not null)
        {
            foreach (var sub in source.Subtitles.Where(x => !string.IsNullOrWhiteSpace(x.Url)))
            {
                startInfo.ArgumentList.Add($"--sub-file={sub.Url}");
            }

            startInfo.ArgumentList.Add("--demuxer-max-bytes=100M");
            startInfo.ArgumentList.Add("--sub-auto=all");
        }

        startInfo.ArgumentList.Add("--fullscreen");
        startInfo.ArgumentList.Add("--keep-open=no");

        Debug.WriteLine($"mpv {string.Join(" ", startInfo.ArgumentList)}");
        return TryStart(startInfo);
    }

    public bool VlcPlay(VideoSourceModel source, string? executablePath = null)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.Url))
        {
            throw new ArgumentException("Video source or URL is invalid.");
        }

        var vlcCmd = ResolveSelectedExecutable(executablePath) ?? ResolveVlcExecutable();
        if (vlcCmd is null)
        {
            return false;
        }

        var startInfo = CreateVlcStartInfo(vlcCmd, source);
        Debug.WriteLine($"{startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}");
        return TryStart(startInfo);
    }

    private static ProcessStartInfo CreateVlcStartInfo(string vlcCmd, VideoSourceModel source)
    {
        var startInfo = OperatingSystem.IsMacOS() && string.Equals(vlcCmd, "open", StringComparison.Ordinal)
            ? CreateMacOpenVlcStartInfo()
            : CreateStartInfo(vlcCmd);

        AddVlcArguments(startInfo, source);
        return startInfo;
    }

    private static ProcessStartInfo CreateMacOpenVlcStartInfo()
    {
        var startInfo = CreateStartInfo("open");
        startInfo.ArgumentList.Add("-a");
        startInfo.ArgumentList.Add("VLC");
        startInfo.ArgumentList.Add("--args");
        return startInfo;
    }

    private static void AddVlcArguments(ProcessStartInfo startInfo, VideoSourceModel source)
    {
        startInfo.ArgumentList.Add("--fullscreen");
        // Keep this disabled while diagnosing external-player failures; otherwise VLC can close before its error is visible.
        // startInfo.ArgumentList.Add("--play-and-exit");

        startInfo.ArgumentList.Add(source.Url);

        if (source.Headers is not null)
        {
            foreach (var header in source.Headers)
            {
                if (header.Name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.ArgumentList.Add($":http-user-agent={header.Value}");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(source.Referer))
        {
            startInfo.ArgumentList.Add($":http-referrer={source.Referer}");
        }

        if (source.Subtitles is not null)
        {
            foreach (var sub in source.Subtitles.Where(x => !string.IsNullOrWhiteSpace(x.Url)))
            {
                startInfo.ArgumentList.Add($":input-slave={sub.Url}");
            }
        }
    }

    private static ProcessStartInfo CreateStartInfo(string executable)
    {
        return new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardError = false,
            RedirectStandardOutput = false,
            WorkingDirectory = GetDefaultWorkingDirectory()
        };
    }

    private static bool TryStart(ProcessStartInfo startInfo)
    {
        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"External player launch failed: {ex}");
            return false;
        }
    }

    private static string GetDefaultWorkingDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static string? ResolveMpvExecutable()
    {
        return ResolveConfiguredExecutable("mpv");
    }

    private static string? ResolveSelectedExecutable(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        if (OperatingSystem.IsMacOS() && Directory.Exists(executablePath) && executablePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            var appExecutable = Path.Combine(executablePath, "Contents", "MacOS", Path.GetFileNameWithoutExtension(executablePath));
            if (File.Exists(appExecutable))
            {
                return appExecutable;
            }
        }

        return File.Exists(executablePath) ? executablePath : null;
    }

    private static string? ResolveVlcExecutable()
    {
        return ResolveConfiguredExecutable("vlc");
    }

    private static string? ResolveConfiguredExecutable(string playerName)
    {
        var settings = LoadPlayerSettings();
        var player = settings.GetCurrentPlatform()?.GetPlayer(playerName);
        if (player is null)
        {
            return null;
        }

        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(player.SelectedPath))
        {
            candidates.Add(player.SelectedPath);
        }

        candidates.AddRange(player.DefaultPaths);
        return ResolveExecutable(candidates);
    }

    public void SavePlayerPath(string playerName, string executablePath)
    {
        var resolvedPath = ResolveSelectedExecutable(executablePath);
        if (resolvedPath is null)
        {
            return;
        }

        var settings = LoadPlayerSettings();
        var platform = settings.GetOrCreateCurrentPlatform();
        var player = platform.GetOrCreatePlayer(playerName);
        player.SelectedPath = resolvedPath;
        SavePlayerSettings(settings);
    }

    private static PlayerSettings LoadPlayerSettings()
    {
        var path = GetPlayersJsonPath();
        try
        {
            EnsurePlayerSettingsFile(path);
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PlayerSettings>(json, JsonOptions) ?? PlayerSettings.CreateDefault();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Player settings load failed. Path: {path} Error: {ex}");
            return PlayerSettings.CreateDefault();
        }
    }

    private static void SavePlayerSettings(PlayerSettings settings)
    {
        var path = GetPlayersJsonPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Player settings save failed. Path: {path} Error: {ex}");
        }
    }

    private static void EnsurePlayerSettingsFile(string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(PlayerSettings.CreateDefault(), JsonOptions));
    }

    private static string GetPlayersJsonPath()
    {
        return AppDataPath.GetDataPath("app", "players.json");
    }

    private static string? ResolveExecutable(IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Path.IsPathRooted(candidate))
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                continue;
            }

            var fromPath = FindOnPath(candidate);
            if (fromPath is not null)
            {
                return fromPath;
            }
        }

        return null;
    }

    private static string? FindOnPath(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var candidate = Path.Combine(directory, executable);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private sealed class PlayerSettings
    {
        public PlatformPlayerSettings Windows { get; set; } = new();
        public PlatformPlayerSettings Linux { get; set; } = new();
        public PlatformPlayerSettings Macos { get; set; } = new();

        public static PlayerSettings CreateDefault() => new()
        {
            Windows = new PlatformPlayerSettings
            {
                Vlc = new PlayerPathSettings
                {
                    DefaultPaths =
                    [
                        @"C:\Program Files\VideoLAN\VLC\vlc.exe",
                        @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
                    ]
                },
                Mpv = new PlayerPathSettings
                {
                    DefaultPaths =
                    [
                        @"C:\Program Files\mpv\mpv.exe",
                        @"C:\Program Files\Mpv\mpv.exe",
                        @"C:\mpv\mpv.exe"
                    ]
                }
            },
            Linux = new PlatformPlayerSettings
            {
                Vlc = new PlayerPathSettings { DefaultPaths = ["vlc", "cvlc"] },
                Mpv = new PlayerPathSettings { DefaultPaths = ["mpv"] }
            },
            Macos = new PlatformPlayerSettings
            {
                Vlc = new PlayerPathSettings
                {
                    DefaultPaths =
                    [
                        "open",
                        "/Applications/VLC.app/Contents/MacOS/VLC",
                        "/Applications/VLC media player.app/Contents/MacOS/VLC"
                    ]
                },
                Mpv = new PlayerPathSettings
                {
                    DefaultPaths =
                    [
                        "/Applications/mpv.app/Contents/MacOS/mpv",
                        "/Applications/IINA.app/Contents/MacOS/iina-cli",
                        "mpv"
                    ]
                }
            }
        };

        public PlatformPlayerSettings? GetCurrentPlatform()
        {
            if (OperatingSystem.IsWindows()) return Windows;
            if (OperatingSystem.IsLinux()) return Linux;
            if (OperatingSystem.IsMacOS()) return Macos;
            return null;
        }

        public PlatformPlayerSettings GetOrCreateCurrentPlatform()
        {
            if (OperatingSystem.IsWindows()) return Windows ??= new PlatformPlayerSettings();
            if (OperatingSystem.IsLinux()) return Linux ??= new PlatformPlayerSettings();
            if (OperatingSystem.IsMacOS()) return Macos ??= new PlatformPlayerSettings();
            return Linux ??= new PlatformPlayerSettings();
        }
    }

    private sealed class PlatformPlayerSettings
    {
        public PlayerPathSettings Vlc { get; set; } = new();
        public PlayerPathSettings Mpv { get; set; } = new();

        public PlayerPathSettings? GetPlayer(string playerName)
        {
            return playerName.Equals("vlc", StringComparison.OrdinalIgnoreCase)
                ? Vlc
                : playerName.Equals("mpv", StringComparison.OrdinalIgnoreCase)
                    ? Mpv
                    : null;
        }

        public PlayerPathSettings GetOrCreatePlayer(string playerName)
        {
            return playerName.Equals("vlc", StringComparison.OrdinalIgnoreCase)
                ? Vlc ??= new PlayerPathSettings()
                : Mpv ??= new PlayerPathSettings();
        }
    }

    private sealed class PlayerPathSettings
    {
        public List<string> DefaultPaths { get; set; } = [];
        public string? SelectedPath { get; set; }
    }
}
