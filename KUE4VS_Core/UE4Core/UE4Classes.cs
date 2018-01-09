
using System;
using System.Collections.Generic;

namespace KUE4VS
{
    // UCLASS or USTRUCT
    public class UE4ClassDefnBase
    {
        public string Name;
        public string IncludePath;
        // todo: Ideally also want to know module

        public UE4ClassDefnBase(string name, string incl_path = null)
        {
            Name = name;
            IncludePath = incl_path;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class UClassDefn : UE4ClassDefnBase
    {
        public UClassDefn(string name, string incl_path = null) : base(name, incl_path)
        { }
    }

    public class UStructDefn : UE4ClassDefnBase
    {
        public UStructDefn(string name, string incl_path = null) : base(name, incl_path)
        { }
    }
}
