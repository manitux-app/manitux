using System;
using System.ComponentModel.DataAnnotations;
using CodeLogic.Core.Configuration;

namespace Manitux.Core.Plugins;

public class PluginConfig: ConfigModelBase
{
    //[Required]
    public string MainUrl { get; set; }
    public string Favicon { get; set; }
    public string Language { get; set; }
    public bool UseProxy {get; set;} = false;
    public bool IsAdult {get; set;} = false;
}
