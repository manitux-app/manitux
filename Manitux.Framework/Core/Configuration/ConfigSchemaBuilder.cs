using System.Collections;
using System.Reflection;

namespace CodeLogic.Core.Configuration;

/// <summary>
/// Reflects over a configuration type and produces a flat <see cref="ConfigSchema"/>
/// describing every editable field. Handles nested objects, dictionaries of
/// sub-configs, enums, and primitive types. Unknown complex types are represented
/// as opaque "object" fields that UIs can skip or delegate to a custom editor.
/// </summary>
internal static class ConfigSchemaBuilder
{
    /// <summary>
    /// Build a <see cref="ConfigSchema"/> for the given registered type and
    /// currently-loaded instance. Pass <paramref name="instance"/> = null to
    /// produce a defaults-only schema (fresh instance is used for current values).
    /// </summary>
    public static ConfigSchema Build(
        Type sectionType,
        string sectionName,
        object? instance,
        bool includeSecrets)
    {
        var sectionAttr = sectionType.GetCustomAttribute<ConfigSectionAttribute>();
        var current = instance ?? Activator.CreateInstance(sectionType);
        var defaults = Activator.CreateInstance(sectionType);

        var fields = new List<ConfigField>();
        AppendFields(sectionType, current, defaults, path: "", group: null, groupCollapsed: false, includeSecrets, fields);

        return new ConfigSchema
        {
            SectionName = sectionName,
            Title = sectionType.Name,
            Version = sectionAttr?.Version ?? 1,
            Fields = fields
        };
    }

    private static void AppendFields(
        Type type,
        object? currentInstance,
        object? defaultInstance,
        string path,
        string? group,
        bool groupCollapsed,
        bool includeSecrets,
        List<ConfigField> accumulator)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<ConfigFieldAttribute>();
            var fieldPath = string.IsNullOrEmpty(path) ? ToCamelCase(prop.Name) : $"{path}.{ToCamelCase(prop.Name)}";
            var propType = prop.PropertyType;
            var currentValue = currentInstance is null ? null : prop.GetValue(currentInstance);
            var defaultValue = defaultInstance is null ? null : prop.GetValue(defaultInstance);
            var fieldGroup = attr?.Group ?? group;
            var fieldCollapsed = attr?.Collapsed ?? groupCollapsed;

            if (IsPrimitiveLike(propType))
            {
                accumulator.Add(BuildPrimitiveField(prop, attr, fieldPath, propType, currentValue, defaultValue, fieldGroup, fieldCollapsed, includeSecrets));
                continue;
            }

            if (propType.IsEnum)
            {
                accumulator.Add(BuildEnumField(prop, attr, fieldPath, propType, currentValue, defaultValue, fieldGroup, fieldCollapsed));
                continue;
            }

            if (IsDictionaryOfConfig(propType, out var valueType) && valueType is not null)
            {
                // Dictionary<string, X> — iterate current entries. For each entry, recurse X with path "{fieldPath}.{key}".
                if (currentValue is IDictionary dict)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (key is null) continue;
                        var keyStr = key.ToString() ?? "";
                        var entryPath = $"{fieldPath}.{keyStr}";
                        AppendFields(valueType, dict[key], Activator.CreateInstance(valueType), entryPath, fieldGroup, fieldCollapsed, includeSecrets, accumulator);
                    }
                }
                continue;
            }

            if (IsListOfPrimitive(propType))
            {
                // List of primitives renders as a single textarea (one value per line) — one field.
                accumulator.Add(new ConfigField
                {
                    Path = fieldPath,
                    Label = attr?.Label ?? Humanize(prop.Name),
                    Description = attr?.Description,
                    InputType = ConfigInputType.Textarea,
                    TypeHint = "list",
                    Required = attr?.Required ?? false,
                    Secret = attr?.Secret ?? false,
                    RequiresRestart = attr?.RequiresRestart ?? false,
                    Placeholder = attr?.Placeholder,
                    MaxLength = attr?.MaxLength > 0 ? attr.MaxLength : null,
                    Group = fieldGroup,
                    GroupCollapsed = fieldCollapsed,
                    Order = attr?.Order ?? 0,
                    DefaultValue = defaultValue,
                    CurrentValue = currentValue
                });
                continue;
            }

            // Nested complex object — recurse with dotted path.
            if (propType.IsClass && propType != typeof(string))
            {
                AppendFields(propType, currentValue, defaultValue ?? Activator.CreateInstance(propType), fieldPath, fieldGroup, fieldCollapsed, includeSecrets, accumulator);
                continue;
            }

            // Fallback: opaque object, skip (UI can't render it).
        }
    }

    private static ConfigField BuildPrimitiveField(
        PropertyInfo prop,
        ConfigFieldAttribute? attr,
        string path,
        Type propType,
        object? currentValue,
        object? defaultValue,
        string? group,
        bool groupCollapsed,
        bool includeSecrets)
    {
        var isSecret = attr?.Secret ?? false;
        var inputType = attr?.InputType ?? ConfigInputType.Auto;
        if (inputType == ConfigInputType.Auto)
        {
            inputType = isSecret
                ? ConfigInputType.Password
                : propType == typeof(bool) ? ConfigInputType.Checkbox
                : IsNumeric(propType) ? ConfigInputType.Number
                : ConfigInputType.Text;
        }

        var typeHint = TypeHintFor(propType);
        var maskedValue = isSecret && !includeSecrets && currentValue is string s && s.Length > 0
            ? ConfigField.SecretMask
            : currentValue;

        return new ConfigField
        {
            Path = path,
            Label = attr?.Label ?? Humanize(prop.Name),
            Description = attr?.Description,
            InputType = inputType,
            TypeHint = typeHint,
            Required = attr?.Required ?? false,
            Secret = isSecret,
            RequiresRestart = attr?.RequiresRestart ?? false,
            Placeholder = attr?.Placeholder,
            AllowedValues = attr?.AllowedValues is { Length: > 0 } av ? av.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : null,
            Min = attr is not null && !double.IsNaN(attr.Min) ? attr.Min : null,
            Max = attr is not null && !double.IsNaN(attr.Max) ? attr.Max : null,
            MaxLength = attr?.MaxLength > 0 ? attr.MaxLength : null,
            Group = group,
            GroupCollapsed = groupCollapsed,
            Order = attr?.Order ?? 0,
            DefaultValue = defaultValue,
            CurrentValue = maskedValue
        };
    }

    private static ConfigField BuildEnumField(
        PropertyInfo prop,
        ConfigFieldAttribute? attr,
        string path,
        Type enumType,
        object? currentValue,
        object? defaultValue,
        string? group,
        bool groupCollapsed)
    {
        var names = Enum.GetNames(enumType);
        return new ConfigField
        {
            Path = path,
            Label = attr?.Label ?? Humanize(prop.Name),
            Description = attr?.Description,
            InputType = ConfigInputType.Select,
            TypeHint = $"enum:{string.Join(',', names)}",
            Required = attr?.Required ?? false,
            RequiresRestart = attr?.RequiresRestart ?? false,
            AllowedValues = names,
            Group = group,
            GroupCollapsed = groupCollapsed,
            Order = attr?.Order ?? 0,
            DefaultValue = defaultValue?.ToString(),
            CurrentValue = currentValue?.ToString()
        };
    }

    private static bool IsPrimitiveLike(Type t)
    {
        var under = Nullable.GetUnderlyingType(t) ?? t;
        return under.IsPrimitive
            || under == typeof(string)
            || under == typeof(decimal)
            || under == typeof(DateTime)
            || under == typeof(Guid);
    }

    private static bool IsNumeric(Type t)
    {
        var under = Nullable.GetUnderlyingType(t) ?? t;
        return under == typeof(int) || under == typeof(long) || under == typeof(short)
            || under == typeof(double) || under == typeof(float) || under == typeof(decimal)
            || under == typeof(uint) || under == typeof(ulong) || under == typeof(ushort)
            || under == typeof(byte) || under == typeof(sbyte);
    }

    private static bool IsDictionaryOfConfig(Type t, out Type? valueType)
    {
        valueType = null;
        if (!t.IsGenericType) return false;
        var def = t.GetGenericTypeDefinition();
        if (def != typeof(Dictionary<,>) && def != typeof(IDictionary<,>)) return false;
        var args = t.GetGenericArguments();
        if (args[0] != typeof(string)) return false;
        valueType = args[1];
        return true;
    }

    private static bool IsListOfPrimitive(Type t)
    {
        if (!t.IsGenericType) return false;
        var def = t.GetGenericTypeDefinition();
        if (def != typeof(List<>) && def != typeof(IList<>) && def != typeof(IReadOnlyList<>)) return false;
        return IsPrimitiveLike(t.GetGenericArguments()[0]);
    }

    private static string TypeHintFor(Type t)
    {
        var under = Nullable.GetUnderlyingType(t) ?? t;
        if (under == typeof(string)) return "string";
        if (under == typeof(bool)) return "bool";
        if (under == typeof(int) || under == typeof(short) || under == typeof(byte)) return "int";
        if (under == typeof(long) || under == typeof(uint)) return "long";
        if (under == typeof(float) || under == typeof(double) || under == typeof(decimal)) return "double";
        if (under == typeof(DateTime)) return "datetime";
        if (under == typeof(Guid)) return "guid";
        return "object";
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>Turn "MinPoolSize" into "Min Pool Size".</summary>
    private static string Humanize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder(name.Length + 4);
        sb.Append(name[0]);
        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
