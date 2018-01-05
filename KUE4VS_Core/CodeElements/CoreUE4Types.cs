
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
}
