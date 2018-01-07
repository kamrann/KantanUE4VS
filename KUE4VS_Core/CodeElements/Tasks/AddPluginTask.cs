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
        // todo: various plugin elements
        public PluginLocation Location { get; set; }

        public AddPluginTask()
        {
            Location = new PluginLocation();
        }

        public override IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj)
        {
            List<GenericFileAdditionTask> additions = new List<GenericFileAdditionTask>();

            string plugin_name = ElementName;
            string category = "";
            bool with_content = false;

            UPlugin new_plugin = new UPlugin(plugin_name, Location.Project, Location.RelativePath);

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
