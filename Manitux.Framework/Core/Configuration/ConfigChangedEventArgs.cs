namespace CodeLogic.Core.Configuration;

/// <summary>
/// Payload for <see cref="IConfigurationManager.ConfigChanged"/>.
/// Services subscribe during initialization and reload internal state when the
/// section they care about fires.
/// </summary>
public sealed class ConfigChangedEventArgs : EventArgs
{
    /// <summary>The section that was modified (empty string = root <c>config.json</c>).</summary>
    public required string SectionName { get; init; }

    /// <summary>The kind of change that occurred.</summary>
    public required ConfigChangeKind Kind { get; init; }

    /// <summary>Optional caller identifier — display name, user id, "system", etc.</summary>
    public string? ChangedBy { get; init; }

    /// <summary>Timestamp of the change in UTC.</summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>Why a config change event fired.</summary>
public enum ConfigChangeKind
{
    /// <summary>Section was saved via <see cref="IConfigurationManager.SaveAsync"/> or <see cref="IConfigurationManager.UpdateSectionAsync"/>.</summary>
    Updated,

    /// <summary>Section was explicitly reloaded from disk.</summary>
    Reloaded,

    /// <summary>Section was reset to its default values.</summary>
    Reset
}
