using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Manitux.Core.Models;

namespace Manitux.Player;

public class ExternalPlayerManager
{
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

        var startInfo = CreateStartInfo(vlcCmd);
        startInfo.ArgumentList.Add(source.Url);

        if (source.Headers is not null)
        {
            foreach (var header in source.Headers)
            {
                startInfo.ArgumentList.Add($":http-header-fields={header.Name}: {header.Value}");
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

        startInfo.ArgumentList.Add("--fullscreen");
        startInfo.ArgumentList.Add("--play-and-exit");

        Debug.WriteLine($"vlc {string.Join(" ", startInfo.ArgumentList)}");
        return TryStart(startInfo);
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
        var candidates = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            candidates.AddRange(GetWindowsProgramFilesCandidates("mpv", "mpv.exe"));
            candidates.AddRange(GetWindowsProgramFilesCandidates("Mpv", "mpv.exe"));
            candidates.Add("mpv.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.Add("/Applications/mpv.app/Contents/MacOS/mpv");
            candidates.Add("/Applications/IINA.app/Contents/MacOS/iina-cli");
            candidates.Add("mpv");
        }
        else
        {
            candidates.Add("mpv");
        }

        return ResolveExecutable(candidates);
    }

    private static string? ResolveSelectedExecutable(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        return File.Exists(executablePath) ? executablePath : null;
    }

    private static string? ResolveVlcExecutable()
    {
        var candidates = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            candidates.AddRange(GetWindowsProgramFilesCandidates("VideoLAN\\VLC", "vlc.exe"));
            candidates.Add("vlc.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.Add("/Applications/VLC.app/Contents/MacOS/VLC");
            candidates.Add("vlc");
        }
        else
        {
            candidates.Add("vlc");
            candidates.Add("cvlc");
        }

        return ResolveExecutable(candidates);
    }

    private static IEnumerable<string> GetWindowsProgramFilesCandidates(string relativeDirectory, string executable)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetEnvironmentVariable("LOCALAPPDATA")
        };

        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.Combine(root!, relativeDirectory, executable));
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
}
