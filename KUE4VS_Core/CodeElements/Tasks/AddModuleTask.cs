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

        void PropChangedHandler(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "ElementName")
            {
                if (!bInterfaceNameManuallySet)
                {
                    PublicInterfaceName = DetermineDefaultInterfaceName();
                }
            }
        }

        string DetermineDefaultInterfaceName()
        {
            return String.IsNullOrEmpty(ElementName) ?
                "" : ("I" + ElementName + "Module");
        }

        private ICommand _custom_impl_unchecked_cmd;
        public ICommand CustomImplUncheckedCommand
        {
            get
            {
                return _custom_impl_unchecked_cmd ?? (_custom_impl_unchecked_cmd = new CommandHandler(() => OnCustomImplementationUnchecked(), true));
            }
        }

        private ICommand _interface_name_changed_cmd;
        public ICommand InterfaceNameChangedCommand
        {
            get
            {
                return _interface_name_changed_cmd ?? (_interface_name_changed_cmd = new CommandHandler(() => OnInterfaceNameChanged(), true));
            }
        }

        void OnCustomImplementationUnchecked()
        {
            PublicInterfaceName = null;
            bInterfaceNameManuallySet = false;
        }

        protected override void OnElementNameChanged()
        {
            /* Doesn't work as this is fired before the binding updates the property. Handling from prop changed instead. 
            if (!bInterfaceNameManuallySet)
            {
                PublicInterfaceName = DetermineDefaultInterfaceName();
            }
            */
        }

        bool bInterfaceNameManuallySet = false;

        void OnInterfaceNameChanged()
        {
            bInterfaceNameManuallySet = !String.IsNullOrEmpty(PublicInterfaceName);
        }

        public AddModuleTask()
        {
            Location = new ModuleLocation();
            Type = ModuleType.Runtime;
            bCustomImplementation = false;
            PublicInterfaceName = null;
            bEnforceIWYU = true;
            bSuppressUnity = true;

            PropertyChanged += PropChangedHandler;
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

            ModuleRef new_module = new ModuleRef(module_name, Location.Host, Location.RelativePath);

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
