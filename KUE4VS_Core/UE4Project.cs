
using System;
using System.Collections.Generic;
using System.IO;

namespace KUE4VS
{
    public class UProject : ModuleHost
    {
        List<UPlugin> _plugins;
        public IEnumerable<UPlugin> Plugins { get { return _plugins; } }

        public UProject(string name, string root): base(name, root)
        {
            _plugins = new List<UPlugin>(Utils.FindPluginsInProject(this));
        }

        public string UProjectFilePath
        {
            get
            {
                return Path.Combine(RootDirectory, Name) + "." + Utils.UProjectExtension;
            }
        }

        public string PluginsDirectory
        {
            get
            {
                return Path.Combine(RootDirectory, "Plugins");
            }
        }
    }
}
