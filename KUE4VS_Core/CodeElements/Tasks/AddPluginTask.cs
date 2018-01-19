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
    public class AddPluginTask : AddCodeElementTask
    {
        public PluginLocation Location { get; set; }
        public string Category { get; set; }
//        public 
        public bool WithContent { get; set; }

        public AddPluginTask()
        {
            Location = new PluginLocation();
            ExtContext.Instance.RefreshModules();
            Location.Project = ExtContext.Instance.AvailableUProjects.FirstOrDefault();//Utils.GetDefaultUProject();

            WithContent = false;
        }

        public override bool DetermineIsNameValid()
        {
            if (!base.DetermineIsNameValid())
            {
                return false;
            }

            if (ExtContext.Instance.AvailableModuleHosts.Where(x => x is UPlugin && string.Compare(x.Name, ElementName, true) == 0).Count() > 0)
            {
                return false;
            }

            return true;
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string plugin_name = ElementName;
            string category = Category;
            bool with_content = WithContent;
            // Prepend the plugin name to the specified relative path - For plugins we generate, we always put the .uplugin file into
            // a directory named after the plugin, on top of any relative path specified in the UI.
            // The UPlugin type, however, doesn't make that assumption since we want to be able to work with preexisting plugins that
            // don't follow that requirement.
            string plugin_rel_path = Path.Combine(plugin_name, Location.RelativePath);

            UPlugin new_plugin = new UPlugin(plugin_name, Location.Project, plugin_rel_path);

            // .uplugin file
            {
                string contents = CodeGeneration.SourceGenerator.GenerateUPluginFile(
                    plugin_name,
                    category,
                    with_content
                    );
                if (contents == null)
                {
                    return null;
                }

                // Now generate the paths where we want to add the files
                var uplugin_folder_path = new_plugin.RootDirectory;

                additions.Add(new GenericFileAdditionTask
                {
                    FileTitle = plugin_name,
                    Extension = ".uplugin",
                    FolderPath = uplugin_folder_path,
                    Contents = contents
                });
            }

            return additions;
        }
    }
}
