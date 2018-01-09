
using System;
using System.Collections.Generic;
using System.IO;

namespace KUE4VS
{
    public class UPlugin : ModuleHost
    {
        public UProject Project { get; set; }
        // Path of the directory containing the uplugin file, relative to project's Plugins directory
        public string RelativePath { get; set; }

        public UPlugin(string name, UProject proj, string rel_path) : base(name, Path.Combine(proj.PluginsDirectory, rel_path))
        {
            Project = proj;
            RelativePath = rel_path;
        }

        public string UPluginFilePath
        {
            get
            {
                return Path.Combine(RootDirectory, Name) + "." + Utils.UPluginExtension;
            }
        }

        public override string ToString()
        {
            return Project.Name + "/" + this.Name;
        }
    }
}
