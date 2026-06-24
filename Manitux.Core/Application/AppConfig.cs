using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CodeLogic.Core.Configuration;

namespace Manitux.Core.Application;

public class AppConfig: ConfigModelBase
{
    [Required]
    public string AppTitle { get; set; } = "Manitux App";

    public string Language { get; set; } = "en-US";

    public string Theme { get; set; } = "Dark";

    public string? CurrentPluginId { get; set; }

    public List<AppSelectedPluginConfig> SelectedPlugins { get; set; } = [];
}

public class AppSelectedPluginConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PackageInternalName { get; set; }
    public string? Version { get; set; }
    public string? Language { get; set; }
    public string? Favicon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsCurrent { get; set; }
}
