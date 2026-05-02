namespace CodeLogic.Core.Configuration;

/// <summary>
/// Hints the UI about which input element to render for a configuration field.
/// When <see cref="Auto"/> (the default) the schema builder infers the type from
/// the property's CLR type and any <see cref="ConfigFieldAttribute"/> hints
/// (e.g. <c>Secret=true</c> → <see cref="Password"/>).
/// </summary>
public enum ConfigInputType
{
    /// <summary>Infer from property type and attribute hints.</summary>
    Auto = 0,

    /// <summary>Single-line text input.</summary>
    Text,

    /// <summary>Masked input for secrets (passwords, API keys).</summary>
    Password,

    /// <summary>Number input for numeric properties.</summary>
    Number,

    /// <summary>Checkbox for boolean properties.</summary>
    Checkbox,

    /// <summary>Dropdown list — pair with <c>AllowedValues</c> or an enum property.</summary>
    Select,

    /// <summary>Multi-line text area.</summary>
    Textarea,

    /// <summary>Email-style validated text input.</summary>
    Email,

    /// <summary>URL-style validated text input.</summary>
    Url
}
