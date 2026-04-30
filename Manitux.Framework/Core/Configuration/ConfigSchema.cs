namespace CodeLogic.Core.Configuration;

/// <summary>
/// Machine-readable description of a configuration section plus the current
/// values loaded in memory. Produced by <see cref="IConfigurationManager.GetSchema"/>
/// and consumed by admin UIs to render a generic editor form.
/// </summary>
public sealed class ConfigSchema
{
    /// <summary>Section identifier, matching the sub-config name used when registering (e.g. "mysql").</summary>
    public required string SectionName { get; init; }

    /// <summary>Human-friendly title. Defaults to the class name if no title is specified.</summary>
    public required string Title { get; init; }

    /// <summary>Optional description of the section.</summary>
    public string? Description { get; init; }

    /// <summary>Schema version from <see cref="ConfigSectionAttribute.Version"/>. Informational.</summary>
    public int Version { get; init; } = 1;

    /// <summary>The individual fields, already flattened with dotted paths for nested objects.</summary>
    public IReadOnlyList<ConfigField> Fields { get; init; } = [];
}

/// <summary>Describes a single editable field in a <see cref="ConfigSchema"/>.</summary>
public sealed class ConfigField
{
    /// <summary>
    /// Dotted path from the section root, e.g. "host", "databases.Default.host",
    /// "smtp.password". Admin UIs use this path when submitting updates.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>Human-friendly label.</summary>
    public required string Label { get; init; }

    /// <summary>Optional help text.</summary>
    public string? Description { get; init; }

    /// <summary>The resolved input type — never <see cref="ConfigInputType.Auto"/>.</summary>
    public required ConfigInputType InputType { get; init; }

    /// <summary>
    /// Stable hint about the underlying CLR shape so UIs can render appropriately:
    /// "string", "int", "long", "double", "bool", "datetime",
    /// "enum:Value1,Value2", "dict", "list", "object" (nested but not expanded).
    /// </summary>
    public required string TypeHint { get; init; }

    /// <summary>Whether the UI should mark this field as required.</summary>
    public bool Required { get; init; }

    /// <summary>Whether this field contains sensitive data that should be masked on read.</summary>
    public bool Secret { get; init; }

    /// <summary>Whether changes to this field only take effect after restart.</summary>
    public bool RequiresRestart { get; init; }

    /// <summary>Placeholder text for the input element.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Allowed values for select/enum fields.</summary>
    public IReadOnlyList<string>? AllowedValues { get; init; }

    /// <summary>Minimum numeric value (null = unset).</summary>
    public double? Min { get; init; }

    /// <summary>Maximum numeric value (null = unset).</summary>
    public double? Max { get; init; }

    /// <summary>Max length for string fields (null = unset).</summary>
    public int? MaxLength { get; init; }

    /// <summary>Collapse-group name. Null means "default group".</summary>
    public string? Group { get; init; }

    /// <summary>Whether the group containing this field should start collapsed.</summary>
    public bool GroupCollapsed { get; init; }

    /// <summary>Explicit display order within the group.</summary>
    public int Order { get; init; }

    /// <summary>The value a freshly-instantiated config class would have.</summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// The currently-loaded value. For fields marked <see cref="ConfigFieldAttribute.Secret"/>
    /// and queried with <c>includeSecrets=false</c>, this is the mask string
    /// <see cref="SecretMask"/> instead of the real value.
    /// </summary>
    public object? CurrentValue { get; init; }

    /// <summary>The placeholder string used when masking secret values on read.</summary>
    public const string SecretMask = "••••••••";
}
