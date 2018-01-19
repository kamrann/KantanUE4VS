// Copyright 2018 Cameron Angus. All Rights Reserved.

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
        public ModuleRef Module { get; set; }

        // Path, relative to Private/Public folder structure
        public string RelativePath { get; set; }

        public SourceRelativeLocation()
        {
            RelativePath = "";
        }
    }

    public class ModuleLocation
    {
        public ModuleHost Host { get; set; }

        // From Project/Plugin Source directory [Optional]
        public string RelativePath { get; set; }

        public ModuleLocation()
        {
            RelativePath = "";
        }
    }

    public class PluginLocation
    {
        public UProject Project { get; set; }

        // From Plugins directory [Optional]
        public string RelativePath { get; set; }

        public PluginLocation()
        {
            RelativePath = "";
        }
    }
}
