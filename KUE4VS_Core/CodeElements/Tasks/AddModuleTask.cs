using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUE4VS
{
    public class AddModuleTask : AddCodeElementTask
    {
        public ModuleLocation Location { get; set; }
        public ModuleType Type { get; set; }
        public bool bEnforceIWYU { get; set; }
        public bool bSuppressUnity { get; set; }
        // todo: extra dependency modules

        public AddModuleTask()
        {
            Location = new ModuleLocation();
            Type = ModuleType.Runtime;
            bEnforceIWYU = true;
            bSuppressUnity = true;
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string module_name = ElementName;
            string file_header = "/** Some copyright stuff. */";

            // Build.cs file
            {
                // Generate content

                List<string> public_deps = new List<string>{
                    "Core"
                };
                List<string> private_deps = new List<string>{
                    "CoreUObject",
                    "Engine",
                };
                if (Type == ModuleType.Editor)
                {
                    private_deps.AddRange(new List<string>
                    {
                        "Slate",
                        "SlateCore",
                        "InputCore",
                        "UnrealEd",
                        "EditorStyle",
                        "PropertyEditor",
                    });
                }
                List<string> dynamic_deps = new List<string>{
                };

                string contents = CodeGeneration.SourceGenerator.GenerateBuildRulesFile(
                    module_name,
                    file_header,
                    public_deps,
                    private_deps,
                    dynamic_deps,
                    bEnforceIWYU,
                    bSuppressUnity
                    );
                if (contents == null)
                {
                    return null;
                }

                // Now generate the paths where we want to add the files
                // @todo: should be using Location.RelativePath here to allow optionally creating module at deeper folder levels
                // but needs more work, we could add as 4th param but needs to be made consistent use for both adding new module,
                // and adding files to an existing module (which may have a nonstandard relative path)
                var build_cs_folder_path = Utils.GenerateSubfolderPath(
                    proj,
                    module_name,
                    ModuleFileLocationType.TopLevel,
                    ""
                    // todo: plugin
                    );

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = module_name,
                    Extension = ".Build.cs",
                    FolderPath = build_cs_folder_path,
                    Contents = contents
                });
            }

            return additions;
        }

        public override void PostFileAdditions(Project proj)
        {
            // @todo: modify uproject/uplugin
        }
    }
}
