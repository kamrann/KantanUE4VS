// Copyright 2018 Cameron Angus. All Rights Reserved.

using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Input;
using System.ComponentModel;

namespace KUE4VS
{
    public class AddModuleTask : AddCodeElementTask
    {
        public ModuleLocation Location { get; set; }
        public ModuleType Type { get; set; }

        bool _custom_implementation = false;
        public bool bCustomImplementation
        {
            get { return _custom_implementation; }
            set
            {
                SetProperty(ref _custom_implementation, value);
            }
        }

        string _public_interface_name = null;
        public string PublicInterfaceName
        {
            get { return _public_interface_name; }
            set
            {
                SetProperty(ref _public_interface_name, value);
            }
        }

        public bool bEnforceIWYU { get; set; }
        public bool bSuppressUnity { get; set; }
        // todo: extra dependency modules

        public string DetermineDefaultInterfaceName()
        {
            return String.IsNullOrEmpty(ElementName) ?
                "" : ("I" + ElementName + "Module");
        }

        public AddModuleTask()
        {
            Type = ModuleType.Runtime;
            bCustomImplementation = false;
            PublicInterfaceName = null;
            bEnforceIWYU = true;
            bSuppressUnity = true;

            Location = new ModuleLocation();
            ExtContext.Instance.RefreshModuleHosts();
            Location.Host = ExtContext.Instance.AvailableModuleHosts.FirstOrDefault(); //Utils.GetDefaultModuleHost();
        }

        public override bool DetermineIsNameValid()
        {
            if (!base.DetermineIsNameValid())
            {
                return false;
            }

            if (ExtContext.Instance.AvailableModules.Where(x => string.Compare(x.Name, ElementName, true) == 0).Count() > 0)
            {
                return false;
            }

            return true;
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string module_name = ElementName;
            string impl_file_title = module_name + "ModuleImpl";
            string file_header = ExtContext.Instance.ExtensionOptions.SourceFileHeaderText;
            bool custom_impl = bCustomImplementation;
            string custom_base = custom_impl ? PublicInterfaceName : null;
            bool has_custom_base = !String.IsNullOrEmpty(custom_base);
            // Prepend the module name to the specified relative path - For modules we generate, we always put the module build file into
            // a directory named after the module, on top of any relative path specified in the UI.
            // The ModuleRef type, however, doesn't make that assumption since we want to be able to work with preexisting modules that
            // don't follow that requirement.
            string module_rel_path = Path.Combine(module_name, Location.RelativePath);

            ModuleRef new_module = new ModuleRef(module_name, Location.Host, module_rel_path);

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
                var build_cs_folder_path = Utils.GenerateSourceSubfolderPath(
                    new_module,
                    ModuleFileLocationType.TopLevel,
                    ""
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
                var folder_path = Utils.GenerateSourceSubfolderPath(
                    new_module,
                    ModuleFileLocationType.Private,
                    ""
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
                    var folder_path = Utils.GenerateSourceSubfolderPath(
                        new_module,
                        ModuleFileLocationType.Private,
                        ""
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
                        var folder_path = Utils.GenerateSourceSubfolderPath(
                            new_module,
                            ModuleFileLocationType.Public,
                            ""
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
            string module_name = ElementName;

            string descriptor_path;
            if (Location.Host is UProject)
            {
                descriptor_path = (Location.Host as UProject).UProjectFilePath;
            }
            else if (Location.Host is UPlugin)
            {
                descriptor_path = (Location.Host as UPlugin).UPluginFilePath;
            }
            else
            {
                throw new Exception();
            }

            var descriptor_obj = JObject.Parse(File.ReadAllText(descriptor_path));

            var module_obj = new JObject();
            module_obj["Name"] = module_name;
            module_obj["Type"] = Constants.ModuleTypeJsonNames[Type];
            module_obj["LoadingPhase"] = "Default"; // @TODO:
                                                    // @TODO: whitelist plats (plugins only? not sure)

            if (descriptor_obj["Modules"] == null)
            {
                descriptor_obj["Modules"] = new JArray();
            }
            var modules = descriptor_obj["Modules"] as JArray;
            modules.Add(module_obj);

            File.WriteAllText(descriptor_path, descriptor_obj.ToString());
        }
    }
}
