using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Models;
using Manitux.Core.Plugins;

namespace Manitux.Models
{
    public class PluginMenuModel
    {
        public required PluginBase Plugin { get; set; }
        public List<CategoryModel>? Categories { get; set; }
    }
}
