
using System.Collections.Generic;

namespace KUE4VS_Core.CodeGeneration.Templates.Preprocessed
{
    public partial class cpp_file
    {
        public string file_title { get; set; }
        public string file_header { get; set; }
        public IEnumerable<string> default_includes { get; set; }
        public bool matching_header { get; set; }
        public string loctext_ns { get; set; }
        public string body { get; set; }
        public string footer_content { get; set; }
    }
}

