using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using System.Collections.ObjectModel;

namespace KUE4VS
{
    public interface IExtContext
    {
        DTE2 Dte { get; }

        // Visual Studio solution build manager interface.
        IVsSolutionBuildManager2 SolutionBuildManager { get; }

        // Visual Studio solution "manager" interface. This needs to be cleaned up at shutdown.
        IVsSolution2 SolutionManager { get; }

        bool IsUE4Loaded { get; }
        string SolutionFilepath { get; }

        KUE4VSOptions ExtensionOptions { get; }

        IVsOutputWindowPane GetOutputPane();

        ObservableCollection<ModuleRef> AvailableModules { get; }
        void RefreshModules();

        ObservableCollection<ModuleHost> AvailableModuleHosts { get; }
        void RefreshModuleHosts();

        ObservableCollection<UProject> AvailableUProjects { get; }
        void RefreshUProjects();
    }
}
