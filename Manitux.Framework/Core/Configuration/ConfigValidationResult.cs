namespace CodeLogic.Core.Configuration;

/// <summary>Represents the result of validating a configuration model.</summary>
public sealed class ConfigValidationResult
{
    /// <summary>Gets whether the configuration passed validation.</summary>
    public bool IsValid { get; private set; }

    /// <summary>Gets the list of validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>
    /// Optional per-field errors, keyed by the field path that would appear in
    /// a <see cref="ConfigSchema"/> (e.g. <c>"databases.Default.port"</c>).
    /// Empty when validation passed or when the validator only emitted flat
    /// error strings.
    /// </summary>
    public IReadOnlyList<ConfigFieldError> FieldErrors { get; private set; } = [];

    private ConfigValidationResult() { }

    /// <summary>Creates a successful validation result.</summary>
    public static ConfigValidationResult Valid() =>
        new() { IsValid = true };

    /// <summary>Creates a failed validation result with the specified errors.</summary>
    public static ConfigValidationResult Invalid(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };

    /// <summary>Creates a failed validation result with a single error.</summary>
    public static ConfigValidationResult Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };

    /// <summary>
    /// Creates a failed validation result with both top-level error strings and
    /// structured per-field errors. The <see cref="Errors"/> list is populated
    /// from <paramref name="fieldErrors"/> when <paramref name="errors"/> is null.
    /// </summary>
    public static ConfigValidationResult Invalid(
        IEnumerable<ConfigFieldError> fieldErrors,
        IEnumerable<string>? errors = null)
    {
        var fieldList = fieldErrors.ToList();
        var topList = errors?.ToList() ?? fieldList.Select(f => $"{f.Path}: {f.Message}").ToList();
        return new() { IsValid = false, Errors = topList, FieldErrors = fieldList };
    }

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
}

/// <summary>
/// A single field-scoped validation error. <see cref="Path"/> matches the field
/// path used in <see cref="ConfigSchema"/> so UIs can surface errors next to
/// the right input.
/// </summary>
public sealed record ConfigFieldError(string Path, string Message);
