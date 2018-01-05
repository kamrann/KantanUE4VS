using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUE4VS
{
    public class AddSourceFileTask : AddCodeElementTask
    {
        public SourceFileAdditionMode Mode { get; set; }
        public SourceRelativeLocation Location { get; set; }
        public bool bPrivateHeader { get; set; }

        // Valid only for non-reflected types
        public string Namespace { get; set; }

        public AddSourceFileTask()
        {
            Location = new SourceRelativeLocation();
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string file_title = ElementName;
            string file_header = "/** Some copyright stuff. */";
            bool is_reflected = false;  // @todo

            List<string> nspace = Utils.SplitNamespaceDefinition(Namespace);

            // Cpp file
            if (Mode.In(SourceFileAdditionMode.HeaderAndCpp, SourceFileAdditionMode.CppOnly))
            {
                // Generate content

                List<string> default_includes = new List<string>
                {
                };

                string cpp_contents = CodeGeneration.SourceGenerator.GenerateCpp(
                    file_title,
                    file_header,
                    default_includes,
                    false,
                    nspace,
                    ""
                    );
                if (cpp_contents == null)
                {
                    return null;
                }

                var cpp_folder_path = Utils.GenerateSubfolderPath(
                    proj,
                    Location.ModuleName,
                    ModuleFileLocationType.Private,
                    Location.RelativePath
                    // todo: plugin
                    );

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = file_title,
                    Extension = ".cpp",
                    FolderPath = cpp_folder_path,
                    Contents = cpp_contents
                });
            }

            // Header file
            if (Mode.In(SourceFileAdditionMode.HeaderAndCpp, SourceFileAdditionMode.HeaderOnly))
            {
                // Generate content

                List<string> default_includes = new List<string>{
                    "CoreMinimal.h"
                };

                string hdr_contents = CodeGeneration.SourceGenerator.GenerateHeader(
                    file_title,
                    file_header,
                    default_includes,
                    is_reflected,
                    nspace,
                    ""
                    );
                if (hdr_contents == null)
                {
                    return null;
                }

                // Now generate the path where we want to add the file
                var hdr_folder_path = Utils.GenerateSubfolderPath(
                    proj,
                    Location.ModuleName,
                    bPrivateHeader ? ModuleFileLocationType.Private : ModuleFileLocationType.Public,
                    Location.RelativePath
                    // todo: plugin
                    );

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = file_title,
                    Extension = ".h",
                    FolderPath = hdr_folder_path,
                    Contents = hdr_contents
                });
            }

            return additions;
        }
    }
}
