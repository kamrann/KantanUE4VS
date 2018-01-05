using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUE4VS
{
    public class CodeElementLocation
    {
    }

    public class SourceRelativeLocation
    {
        // Module
        public string ModuleName { get; set; }

        // Path, relative to Private/Public folder structure
        public string RelativePath { get; set; }
    }

    public class ModuleLocation
    {
        // todo: proj/plug reference

        // From Project/Plugin Source directory [Optional]
        public string RelativePath { get; set; }
    }

    public class PluginLocation
    {
        // todo: proj reference

        // From Plugins directory [Optional]
        public string RelativePath { get; set; }
    }
}
