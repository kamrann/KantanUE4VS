// Copyright 2018 Cameron Angus. All Rights Reserved.

using System.Collections.Generic;

namespace KUE4VS_Core.CodeGeneration.Templates.Preprocessed
{
    public partial class hdr_file
    {
        public string file_title { get; set; }
        public string file_header { get; set; }
        public IEnumerable<string> default_includes{ get; set; }
        public bool reflected{ get; set; }
        public string body{ get; set; }
    }
}

