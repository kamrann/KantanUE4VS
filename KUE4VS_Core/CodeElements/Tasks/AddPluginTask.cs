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
            throw new NotImplementedException();
        }
    }
}
