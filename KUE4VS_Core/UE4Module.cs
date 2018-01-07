
using System;
using System.Collections.Generic;
using System.IO;

namespace KUE4VS
{
    public class ModuleRef
    {
        public string Name { get; set; }
        public ModuleHost Host { get; set; }
        // Path of the directory containing the build rules file, relative to host's Source directory
        public string RelativePath { get; set; }

        public ModuleRef(string name, ModuleHost host, string rel_path)
        {
            Name = name;
            Host = host;
            RelativePath = rel_path;
        }

        public string RootPath
        {
            get
            {
                return Path.Combine(Host.SourceDirectory, RelativePath);
            }
        }

        public override string ToString()
        {
            return Host.ToString() + "/" + Name;
        }
    }
}
