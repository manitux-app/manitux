using System;
using System.Collections.Generic;
using System.Text;
using Manitux.Core.Plugins;

namespace Manitux.Services.Plugins
{
    public interface IPluginService
    {
        PluginBase? CurrentPlugin { get; set; }
    }

    public class PluginService : IPluginService
    {
        public PluginBase? CurrentPlugin { get; set; }
    }
}
