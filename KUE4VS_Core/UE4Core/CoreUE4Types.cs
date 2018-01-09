
using System;
using System.Collections.Generic;

namespace KUE4VS
{
    public enum ModuleFileLocationType
    {
        TopLevel,       // The level of the build rules file.
        Public,
        Private,
    };

    public enum ModuleType
    {
        Runtime,
        Development,
        Editor,
    };

    public static partial class Constants
    {
        public static readonly Dictionary<ModuleType, string> ModuleTypeJsonNames
            = new Dictionary<ModuleType, string>
            {
                { ModuleType.Runtime, "Runtime" },
                { ModuleType.Development, "Developer" },
                { ModuleType.Editor, "Editor" },
            };
    }
}
