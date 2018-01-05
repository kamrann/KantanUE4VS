using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using KUE4VS;

namespace KUE4VS_UI
{
    public class CodeElementTypeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string resName = null;
            if (item is AddSourceFileTask)
            {
                resName = "AddSourceTemplate";
            }
            else if (item is AddTypeTask)
            {
                resName = "AddTypeTemplate";
            }
            else if (item is AddModuleTask)
            {
                resName = "AddModuleTemplate";
            }
            else if (item is AddPluginTask)
            {
                resName = "AddPluginTemplate";
            }

            if (resName != null)
            {
                var obj = container as FrameworkElement;
                while (obj != null)
                {
                    var control = obj as UserControl;
                    if (control != null)
                    {
                        var res = control.FindResource(resName) as DataTemplate;
                        if (res != null)
                        {
                            return res;
                        }
                    }

                    obj = obj.Parent as FrameworkElement;
                }
            }

            return null;
        }
    }
}
