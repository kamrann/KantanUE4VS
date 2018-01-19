// Copyright 2018 Cameron Angus. All Rights Reserved.

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
        public bool Reflected { get; set; }

        // Valid only for non-reflected types
        public string Namespace { get; set; }

        public AddSourceFileTask()
        {
            Location = new SourceRelativeLocation();
            ExtContext.Instance.RefreshModules();
            Location.Module = ExtContext.Instance.AvailableModules.FirstOrDefault();//Utils.GetDefaultModule();
            Reflected = false;
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string file_title = ElementName;
            string file_header = ExtContext.Instance.ExtensionOptions.SourceFileHeaderText;
            bool is_reflected = Reflected;

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
                    ""
                    );
                if (cpp_contents == null)
                {
                    return null;
                }

                string namespaced_contents = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
                {
                    nspace = nspace,
                    content = cpp_contents
                }.TransformText();

                var cpp_folder_path = Utils.GenerateSourceSubfolderPath(
                    Location.Module,
                    ModuleFileLocationType.Private,
                    String.IsNullOrWhiteSpace(Location.RelativePath) ? "" : Location.RelativePath
                    );

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = file_title,
                    Extension = ".cpp",
                    FolderPath = cpp_folder_path,
                    Contents = namespaced_contents
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
                    ""
                    );
                if (hdr_contents == null)
                {
                    return null;
                }

                string namespaced_contents = new KUE4VS_Core.CodeGeneration.Templates.Preprocessed.namespaced_content
                {
                    nspace = nspace,
                    content = hdr_contents
                }.TransformText();

                // Now generate the path where we want to add the file
                var hdr_folder_path = Utils.GenerateSourceSubfolderPath(
                    Location.Module,
                    bPrivateHeader ? ModuleFileLocationType.Private : ModuleFileLocationType.Public,
                    String.IsNullOrWhiteSpace(Location.RelativePath) ? "" : Location.RelativePath
                    );

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = file_title,
                    Extension = ".h",
                    FolderPath = hdr_folder_path,
                    Contents = namespaced_contents
                });
            }

            return additions;
        }
    }
}
