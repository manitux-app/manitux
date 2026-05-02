using System;
using System.ComponentModel.DataAnnotations;
using CodeLogic.Core.Configuration;

namespace Manitux.Core.Application;

public class AppConfig: ConfigModelBase
{
    [Required]
    public string AppTitle { get; set; } = "Manitux App";
}
