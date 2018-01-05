using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Input;

namespace KUE4VS
{
    public class AddModuleTask : AddCodeElementTask
    {
        public ModuleLocation Location { get; set; }
        public ModuleType Type { get; set; }
        public bool bCustomImplementation { get; set; }
        public string PublicInterfaceName { get; set; }
        public bool bEnforceIWYU { get; set; }
        public bool bSuppressUnity { get; set; }
        // todo: extra dependency modules

        private ICommand _custom_impl_unchecked_cmd;
        public ICommand CustomImplUncheckedCommand
        {
            get
            {
                return _custom_impl_unchecked_cmd ?? (_custom_impl_unchecked_cmd = new CommandHandler(() => OnCustomImplementationUnchecked(), true));
            }
        }

        void OnCustomImplementationUnchecked()
        {
            
        }

        public AddModuleTask()
        {
            Location = new ModuleLocation();
            Type = ModuleType.Runtime;
            bCustomImplementation = false;
            PublicInterfaceName = null;
            bEnforceIWYU = true;
            bSuppressUnity = true;
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string module_name = ElementName;
            string impl_file_title = module_name + "ModuleImpl";
            string file_header = "/** Some copyright stuff. */";
            bool custom_impl = bCustomImplementation;
            string custom_base = custom_impl ? PublicInterfaceName : null;
            bool has_custom_base = !String.IsNullOrEmpty(custom_base);

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
                List<string> dynamic_deps = new List<string>
                {
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

            // Module impl cpp
            {
                // Generate content

                List<string> additional_includes = new List<string>{
                };

                // todo?
                List<string> nspace = new List<string>();

                string contents = CodeGeneration.SourceGenerator.GenerateModuleImplCpp(
                    impl_file_title,
                    file_header,
                    module_name,
                    custom_impl,
                    custom_base,
                    additional_includes,
                    nspace
                    );
                if (contents == null)
                {
                    return null;
                }

                // Now generate the path where we want to add the file
                var folder_path = Utils.GenerateSubfolderPath(
                    proj,
                    module_name,
                    ModuleFileLocationType.Private,
                    ""
                    // todo: plugin
                    );

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = impl_file_title,
                    Extension = ".cpp",
                    FolderPath = folder_path,
                    Contents = contents
                });
            }

            if (custom_impl)
            {
                string interface_file_title = custom_base; // @TODO: separately configurable?

                // Module impl header
                {
                    // Generate content

                    List<string> additional_includes = new List<string>
                    {
                    };

                    // todo?
                    List<string> nspace = new List<string>();

                    string custom_base_header = has_custom_base ? (interface_file_title + ".h") : null;

                    string contents = CodeGeneration.SourceGenerator.GenerateModuleImplHeader(
                        impl_file_title,
                        file_header,
                        module_name,
                        custom_base,
                        custom_base_header,
                        additional_includes,
                        nspace
                        );
                    if (contents == null)
                    {
                        return null;
                    }

                    // Now generate the path where we want to add the file
                    var folder_path = Utils.GenerateSubfolderPath(
                        proj,
                        module_name,
                        ModuleFileLocationType.Private,
                        ""
                        // todo: plugin
                        );

                    additions.Add(new GenericFileAdditionTask
                    {
                        FileTitle = impl_file_title,
                        Extension = ".h",
                        FolderPath = folder_path,
                        Contents = contents
                    });
                }

                if (has_custom_base)
                {
                    // Module impl header
                    {
                        // Generate content

                        List<string> additional_includes = new List<string>
                        {
                        };

                        // todo?
                        List<string> nspace = new List<string>();

                        string contents = CodeGeneration.SourceGenerator.GenerateModuleInterfaceHeader(
                            interface_file_title,
                            file_header,
                            module_name,
                            custom_base,
                            additional_includes,
                            nspace
                            );
                        if (contents == null)
                        {
                            return null;
                        }

                        // Now generate the path where we want to add the file
                        var folder_path = Utils.GenerateSubfolderPath(
                            proj,
                            module_name,
                            ModuleFileLocationType.Public,
                            ""
                            // todo: plugin
                            );

                        additions.Add(new GenericFileAdditionTask
                        {
                            FileTitle = interface_file_title,
                            Extension = ".h",
                            FolderPath = folder_path,
                            Contents = contents
                        });
                    }
                }
            }

            return additions;
        }

        public override void PostFileAdditions(Project proj)
        {
            // @todo: plugin version

            string module_name = ElementName;

            // @todo: is it feasible to use some of the UBT assemblies for stuff like this?
            var uproject_path = Utils.GetUProjectFilePath(proj);
            var uproject_obj = JObject.Parse(File.ReadAllText(uproject_path));
            var module_obj = new JObject();
            module_obj["Name"] = module_name;
            module_obj["Type"] = Constants.ModuleTypeJsonNames[Type];
            module_obj["LoadingPhase"] = "Default"; // @TODO:
                                                    // @TODO: whitelist plats (plugins only? not sure)

            if (uproject_obj["Modules"] == null)
            {
                uproject_obj["Modules"] = new JArray();
            }
            var modules = uproject_obj["Modules"] as JArray;
            modules.Add(module_obj);

            File.WriteAllText(uproject_path, uproject_obj.ToString());
        }
    }
}
