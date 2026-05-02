namespace CodeLogic.Core.Utilities;

/// <summary>
/// Validates the runtime environment and directory structure before the framework starts.
/// </summary>
public sealed class StartupValidator
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>Validates the framework root path, directory structure, and configuration.</summary>
    public ValidationResult Validate(string frameworkRootPath)
    {
        _errors.Clear();
        _warnings.Clear();

        ValidateDirectories(frameworkRootPath);
        ValidateSystemRequirements();
        ValidateCodeLogicJson(frameworkRootPath);

        return new ValidationResult
        {
            IsSuccess = _errors.Count == 0,
            ErrorMessage = _errors.Count > 0 ? string.Join(Environment.NewLine, _errors) : null
        };
    }

    /// <summary>Returns the list of non-fatal warnings from the last validation run.</summary>
    public IReadOnlyList<string> GetWarnings() => _warnings.AsReadOnly();
    /// <summary>Returns the list of fatal errors from the last validation run.</summary>
    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();

    private void ValidateDirectories(string rootPath)
    {
        var dirs = new[]
        {
            rootPath,
            Path.Combine(rootPath, "Framework"),
            Path.Combine(rootPath, "Framework", "logs"),
            Path.Combine(rootPath, "Libraries"),
        };

        foreach (var dir in dirs)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    _warnings.Add($"Created missing directory: {dir}");
                }

                // Test write access
                var testFile = Path.Combine(dir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                _errors.Add($"Directory '{dir}' is not writable: {ex.Message}");
            }
            catch (IOException ex)
            {
                _errors.Add($"I/O error accessing directory '{dir}': {ex.Message}");
            }
        }
    }

    private void ValidateSystemRequirements()
    {
        var version = Environment.Version;
        if (version.Major < 10)
            _warnings.Add($"Running on .NET {version}. Framework is optimized for .NET 10+");

        try
        {
            var gcMemory = GC.GetTotalMemory(false);
            if (gcMemory > 1_000_000_000)
                _warnings.Add($"High memory usage detected: {gcMemory / 1_000_000}MB");
        }
        catch { /* ignore */ }

        try
        {
            var root = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
            var drive = new DriveInfo(root);
            if (drive.IsReady && drive.AvailableFreeSpace < 100_000_000)
                _warnings.Add($"Low disk space: {drive.AvailableFreeSpace / 1_000_000}MB available");
        }
        catch { /* ignore */ }
    }

    private void ValidateCodeLogicJson(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "Framework", "CodeLogic.json");
        if (!File.Exists(configPath))
        {
            _errors.Add($"CodeLogic.json not found at: {configPath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _errors.Add($"CodeLogic.json contains invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to read CodeLogic.json: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents the result of a startup validation run.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>Whether all validation checks passed.</summary>
    public bool IsSuccess { get; init; }
    /// <summary>Combined error messages if validation failed, otherwise null.</summary>
    public string? ErrorMessage { get; init; }
}
