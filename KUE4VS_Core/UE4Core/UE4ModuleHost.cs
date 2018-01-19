// Copyright 2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

namespace KUE4VS
{
    public class ModuleHost
    {
        string _name;
        string _root_dir;
        List<ModuleRef> _modules;

        public ModuleHost(string name, string root)
        {
            _name = name;
            _root_dir = root;
            _modules = new List<ModuleRef>(Utils.FindModulesInSourceFolder(SourceDirectory, this));
        }

        public string Name { get { return _name; } }

        public string RootDirectory
        {
            get
            {
                return _root_dir;
            }
        }

        public IEnumerable<ModuleRef> Modules
        {
            get
            {
                return _modules;
            }
        }

        public string SourceDirectory
        {
            get
            {
                return Path.Combine(RootDirectory, "Source");
            }
        }

        public override string ToString()
        {
            return _name;
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as ModuleHost;
            return !ReferenceEquals(other, null) && other.ToString().CompareTo(this.ToString()) == 0;
        }
    }
}
