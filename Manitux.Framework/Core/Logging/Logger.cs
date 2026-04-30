using System.Diagnostics;

namespace CodeLogic.Core.Logging;

/// <summary>File-based logger that writes entries to disk with optional console output.</summary>
public sealed class Logger : ILogger
{
    private readonly string _componentName;
    private readonly string _componentLogsPath;
    private readonly LogLevel _minimumLevel;
    private readonly LoggingOptions _options;
    private readonly object _lock = new();           // per-instance, not static

    /// <summary>Initializes a new logger for the specified component.</summary>
    public Logger(
        string componentName,
        string componentLogsPath,
        LogLevel minimumLevel,
        LoggingOptions options)
    {
        _componentName = componentName;
        _componentLogsPath = componentLogsPath;
        _minimumLevel = minimumLevel;
        _options = options;
        Directory.CreateDirectory(_componentLogsPath);
    }

    /// <inheritdoc />
    public void Trace(string message)    => Log(LogLevel.Trace, message);
    /// <inheritdoc />
    public void Debug(string message)    => Log(LogLevel.Debug, message);
    /// <inheritdoc />
    public void Info(string message)     => Log(LogLevel.Info, message);
    /// <inheritdoc />
    public void Warning(string message)  => Log(LogLevel.Warning, message);
    /// <inheritdoc />
    public void Error(string message, Exception? exception = null)    => Log(LogLevel.Error, message, exception);
    /// <inheritdoc />
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);

    private void Log(LogLevel level, string message, Exception? exception = null)
    {
        var timestamp = DateTime.Now.ToString(_options.TimestampFormat);
        var prefix = BuildPrefix(timestamp, level);
        var entry = BuildEntry(prefix, message, exception);

        // Component log file
        if (level >= _minimumLevel)
        {
            var filePath = ResolveFilePath(level);
            WriteToFile(filePath, entry);
        }

        // Centralized debug log
        if (_options.EnableDebugMode && _options.CentralizedDebugLog
            && !string.IsNullOrWhiteSpace(_options.CentralizedLogsPath))
        {
            var centralFile = ResolveCentralizedFilePath();
            // Centralized log always includes component name
            var centralEntry = BuildEntry($"[{_componentName}] {prefix}", message, exception);
            WriteToFile(centralFile, centralEntry);
        }

        // Console output
        if (_options.EnableConsoleOutput && level >= _options.ConsoleMinimumLevel)
        {
            WriteToConsole(level, prefix, message, exception);
        }
    }

    private string BuildPrefix(string timestamp, LogLevel level)
    {
        var levelStr = level.ToString().ToUpper();
        return _options.IncludeMachineName
            ? $"[{_componentName}][{Environment.MachineName}] {timestamp} [{levelStr}]"
            : $"[{_componentName}] {timestamp} [{levelStr}]";
    }

    private static string BuildEntry(string prefix, string message, Exception? exception)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(prefix).Append(' ').Append(message);
        if (exception != null)
            sb.AppendLine().Append(exception);
        return sb.ToString();
    }

    private string ResolveFilePath(LogLevel level)
    {
        if (_options.Mode == LoggingMode.SingleFile)
        {
            // component.log — rolling handled in WriteToFile
            return Path.Combine(_componentLogsPath, $"{_componentName.ToLower()}.log");
        }
        else
        {
            // Date folder pattern
            var now = DateTime.Now;
            var pattern = _options.FileNamePattern;
            var relative = pattern
                .Replace("{date:yyyy}", now.ToString("yyyy"))
                .Replace("{date:MM}",   now.ToString("MM"))
                .Replace("{date:dd}",   now.ToString("dd"))
                .Replace("{level}",     level.ToString().ToLower());

            var fileName  = Path.GetFileName(relative);
            var directory = Path.GetDirectoryName(relative) ?? string.Empty;
            var fullDir   = Path.Combine(_componentLogsPath, directory);
            Directory.CreateDirectory(fullDir);
            return Path.Combine(fullDir, fileName);
        }
    }

    private string ResolveCentralizedFilePath()
    {
        var dir = Path.Combine(_options.CentralizedLogsPath!,
            DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), DateTime.Now.ToString("dd"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "debug_all.log");
    }

    private void WriteToFile(string filePath, string content)
    {
        lock (_lock)
        {
            try
            {
                // SingleFile: check size and roll if needed
                if (_options.Mode == LoggingMode.SingleFile && File.Exists(filePath))
                {
                    var info = new FileInfo(filePath);
                    if (info.Length >= _options.MaxFileSizeMb * 1024L * 1024L)
                    {
                        RollFiles(filePath);
                    }
                }

                File.AppendAllText(filePath, content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Logging must never crash the app — fallback to Console.Error
                try
                {
                    Console.Error.WriteLine($"[CodeLogic.Logger] Failed to write to {filePath}: {ex.Message}");
                    Console.Error.WriteLine(content);
                }
                catch { /* if Console.Error also fails, give up silently */ }
            }
        }
    }

    private void RollFiles(string activeFilePath)
    {
        // Delete the oldest file if at limit
        var oldest = GetRolledFilePath(activeFilePath, _options.MaxRolledFiles);
        if (File.Exists(oldest))
            File.Delete(oldest);

        // Shift: component.N.log → component.(N+1).log
        for (int i = _options.MaxRolledFiles - 1; i >= 1; i--)
        {
            var src  = GetRolledFilePath(activeFilePath, i);
            var dest = GetRolledFilePath(activeFilePath, i + 1);
            if (File.Exists(src))
                File.Move(src, dest, overwrite: true);
        }

        // component.log → component.1.log
        var rolled = GetRolledFilePath(activeFilePath, 1);
        File.Move(activeFilePath, rolled, overwrite: true);
    }

    private static string GetRolledFilePath(string activeFilePath, int index)
    {
        // component.log → component.1.log
        var dir  = Path.GetDirectoryName(activeFilePath)!;
        var name = Path.GetFileNameWithoutExtension(activeFilePath);
        var ext  = Path.GetExtension(activeFilePath);
        return Path.Combine(dir, $"{name}.{index}{ext}");
    }

    private void WriteToConsole(LogLevel level, string prefix, string message, Exception? exception)
    {
        lock (Console.Out)
        {
            try
            {
                Console.ForegroundColor = GetConsoleColor(level);
                Console.WriteLine($"{prefix} {message}");
                if (exception != null)
                    Console.WriteLine(exception);
                Console.ResetColor();
            }
            catch { /* console write failures are silently ignored */ }
        }
    }

    private static ConsoleColor GetConsoleColor(LogLevel level) => level switch
    {
        LogLevel.Trace    => ConsoleColor.DarkGray,
        LogLevel.Debug    => ConsoleColor.Cyan,
        LogLevel.Info     => ConsoleColor.White,
        LogLevel.Warning  => ConsoleColor.Yellow,
        LogLevel.Error    => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.DarkRed,
        _                 => ConsoleColor.White
    };
}
