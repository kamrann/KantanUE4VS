// Copyright 2018 Cameron Angus. All Rights Reserved.

using System.Collections.Generic;

namespace KUE4VS_Core.CodeGeneration.Templates.Preprocessed
{
    public partial class build_cs_file
    {
        public string file_header { get; set; }
        public string module_name { get; set; }
        public bool enforce_iwyu { get; set; }
        public bool suppress_unity { get; set; }
        public IEnumerable<string> public_deps { get; set; }
        public IEnumerable<string> private_deps { get; set; }
        public IEnumerable<string> dynamic_deps { get; set; }
    }
}

