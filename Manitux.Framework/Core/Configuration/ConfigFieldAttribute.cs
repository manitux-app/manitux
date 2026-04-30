namespace CodeLogic.Core.Configuration;

/// <summary>
/// Describes a configuration field for admin UIs, validation reports, CLI wizards
/// and any other tooling that needs machine-readable metadata about the setting.
/// </summary>
/// <remarks>
/// Purely additive: configs without this attribute still work — the schema builder
/// falls back to property names and auto-inferred input types.
///
/// Typical usage on a property of a class deriving from <see cref="ConfigModelBase"/>:
/// <code>
/// [ConfigField(
///     Label = "MySQL Host",
///     Description = "Hostname or IP of the MySQL server.",
///     Required = true,
///     Placeholder = "localhost")]
/// public string Host { get; set; } = "localhost";
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ConfigFieldAttribute : Attribute
{
    /// <summary>Human-friendly label. Defaults to the property name if unset.</summary>
    public string? Label { get; set; }

    /// <summary>Help text rendered below the input.</summary>
    public string? Description { get; set; }

    /// <summary>Placeholder text for the input element.</summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Preferred input type. When <see cref="ConfigInputType.Auto"/> (default),
    /// the schema builder infers from the CLR type and other attribute hints.
    /// </summary>
    public ConfigInputType InputType { get; set; } = ConfigInputType.Auto;

    /// <summary>When true, the UI should mark this field as required.</summary>
    public bool Required { get; set; }

    /// <summary>
    /// When true, the field value is masked on read and — when the client echoes
    /// back the mask — the existing value is preserved on save. Typical for
    /// passwords, API keys, tokens.
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>
    /// When true, the UI indicates that changes take effect only after an
    /// application restart. Useful for connection strings, pool sizes, etc.
    /// </summary>
    public bool RequiresRestart { get; set; }

    /// <summary>
    /// Comma-separated list of allowed values for <see cref="ConfigInputType.Select"/>
    /// fields. For enum properties, the enum's values are used automatically if
    /// this is not set.
    /// </summary>
    public string? AllowedValues { get; set; }

    /// <summary>Minimum numeric value. NaN means unset.</summary>
    public double Min { get; set; } = double.NaN;

    /// <summary>Maximum numeric value. NaN means unset.</summary>
    public double Max { get; set; } = double.NaN;

    /// <summary>Max length for string fields. 0 means unset.</summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Group name for the UI to visually bucket related fields
    /// (e.g. "Connection", "Advanced"). Null = default group.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>When true, the UI may collapse this field's group by default.</summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// Explicit display order within a group. Lower values show first.
    /// Unspecified fields are ordered by declaration order.
    /// </summary>
    public int Order { get; set; }
}
