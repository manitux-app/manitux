using System;
using System.ComponentModel.DataAnnotations;
using CodeLogic.Core.Configuration;

namespace Manitux.Core.Plugins;

public class PluginConfig: ConfigModelBase
{
    //[Required]
    public string MainUrl { get; set; } = string.Empty;
    public string Favicon { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool UseProxy { get; set; }
    public bool IsAdult { get; set; }
}
