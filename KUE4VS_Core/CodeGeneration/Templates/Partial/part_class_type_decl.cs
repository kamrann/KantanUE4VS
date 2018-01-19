// Copyright 2018 Cameron Angus. All Rights Reserved.

using System.Collections.Generic;

namespace KUE4VS_Core.CodeGeneration.Templates.Preprocessed
{
    public partial class class_type_decl
    {
        public string type_name { get; set; }
        public string base_class { get; set; }
        public string module_name { get; set; }
        public string type_keyword { get; set; }
        public bool export { get; set; }
        public bool reflected { get; set; }
        public string reflection_macro { get; set; }
        public bool constructor { get; set; }
        public IEnumerable<string> declarations { get; set; }
    }
}

