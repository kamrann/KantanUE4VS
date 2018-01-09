
using System;
using System.Collections.Generic;
using System.IO;

namespace KUE4VS
{
    public class ModuleRef: IEquatable<ModuleRef>
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

        public bool Equals(ModuleRef other)
        {
            if (other == null)
                return false;

            return this.ToString().CompareTo(other.ToString()) == 0;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(Object obj)
        {
            var other = obj as ModuleHost;
            return !ReferenceEquals(other, null) && Equals(other);
        }

        public static bool operator ==(ModuleRef m1, ModuleRef m2)
        {
            if (((object)m1) == null || ((object)m2) == null)
                return Object.Equals(m1, m2);

            return m1.Equals(m2);
        }

        public static bool operator !=(ModuleRef m1, ModuleRef m2)
        {
            if (((object)m1) == null || ((object)m2) == null)
                return !Object.Equals(m1, m2);

            return !(m1.Equals(m2));
        }
    }
}
