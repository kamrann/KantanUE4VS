using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using EnvDTE80;

namespace UE4PropVis_VSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(KantanUE4VSPkg.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(AddCodeElementWindow))]
    public sealed class KantanUE4VSPkg :
        Package,
        KUE4VS.IExtContext,
        IVsSolutionEvents,
        IDisposable
    {
        /// <summary>
        /// KantanUE4VSPkg GUID string.
        /// </summary>
        public const string PackageGuidString = "58fe42de-aa3c-45b2-a2dd-4ffe1583db46";

        private const string VersionString = "v0.1";
        private const string ExtensionName = "KantanUE4VS";
        private const string UnrealSolutionFileNamePrefix = "UE4";

        private DTE2 dte;
        private IVsSolutionBuildManager2 build_mgr;
        private IVsSolution2 sln_mgr;
        // Variables to keep track of the loaded solution
        private bool is_ue4_loaded = false;
        private string solution_filepath;

        private string _UBTVersion = string.Empty;

        /// Handle that we use at shutdown to unregister for events about solution activity
        private UInt32 SolutionEventsHandle;
 

        DTE2 KUE4VS.IExtContext.Dte
        {
            get
            {
                return dte;
            }
        }

        IVsSolutionBuildManager2 KUE4VS.IExtContext.SolutionBuildManager
        {
            get
            {
                return build_mgr;
            }
        }

        IVsSolution2 KUE4VS.IExtContext.SolutionManager
        {
            get
            {
                return sln_mgr;
            }
        }

        bool KUE4VS.IExtContext.IsUE4Loaded
        {
            get
            {
                return is_ue4_loaded;
            }
        }

        string KUE4VS.IExtContext.SolutionFilepath
        {
            get
            {
                return solution_filepath;
            }
        }

        /// <summary>
		/// Gets a Visual Studio pane to output text to, or creates one if not visible.  Does not bring the pane to front (you can call Activate() to do that.)
		/// </summary>
		/// <returns>The pane to output to, or null on error</returns>
		IVsOutputWindowPane KUE4VS.IExtContext.GetOutputPane()
        {
            return GetOutputPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, "Build");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KantanUE4VSPkg"/> class.
        /// </summary>
        public KantanUE4VSPkg()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            KUE4VS.Logging.Initialize(ExtensionName, VersionString);
            KUE4VS.Logging.WriteLine("Loading UnrealVS extension package...");

            dte = GetGlobalService(typeof(DTE)) as DTE2;

            sln_mgr = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution2;
            sln_mgr.AdviseSolutionEvents(this, out SolutionEventsHandle);

            build_mgr = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;

            KUE4VS.ExtContext.Instance = this;
            PackageProvider.Pkg = this;

            UpdateUnrealLoadedStatus();

            AddNewSourceFileCmd.Initialize(this);
            AddNewClassCmd.Initialize(this);
            AddNewModuleCmd.Initialize(this);
            AddNewPluginCmd.Initialize(this);
            AddCodeElementWindowCommand.Initialize(this);
        }

        private void UpdateUnrealLoadedStatus()
        {
            if (!dte.Solution.IsOpen)
            {
                is_ue4_loaded = false;
                return;
            }

            string SolutionDirectory, UserOptsFile;
            sln_mgr.GetSolutionInfo(out SolutionDirectory, out solution_filepath, out UserOptsFile);

            var SolutionLines = new string[0];
            try
            {
                SolutionLines = System.IO.File.ReadAllLines(solution_filepath);
            }
            catch
            {
            }

            const string UBTTag = "# UnrealEngineGeneratedSolutionVersion=";
            var UBTLine = SolutionLines.FirstOrDefault(TextLine => TextLine.Trim().StartsWith(UBTTag));
            if (UBTLine != null)
            {
                _UBTVersion = UBTLine.Trim().Substring(UBTTag.Length);
                is_ue4_loaded = true;
            }
            else
            {
                _UBTVersion = string.Empty;
                is_ue4_loaded =
                    (
                        solution_filepath != null &&
                        System.IO.Path.GetFileName(solution_filepath).StartsWith(UnrealSolutionFileNamePrefix, StringComparison.OrdinalIgnoreCase)
                    );
            }
        }

        #endregion

        #region IVsSolutionEvents implementation

        ///
        /// IVsSolutionEvents implementation
        ///

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            UpdateUnrealLoadedStatus();

/*            if (OnSolutionClosed != null)
            {
                OnSolutionClosed();
            }
*/
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            // This function is called after a Visual Studio project is opened (or a new project is created.)

            // Get the actual Project object from the IVsHierarchy object that was supplied
/*            var OpenedProject = Utils.HierarchyObjectToProject(pHierarchy);
            Utils.OnProjectListChanged();
            if (OpenedProject != null && OnProjectOpened != null)
            {
                LoadedProjectPaths.Add(OpenedProject.FullName);
                OnProjectOpened(OpenedProject);
            }
*/
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            UpdateUnrealLoadedStatus();

/*            StartTicker();

            if (OnSolutionOpened != null)
            {
                OnSolutionOpened();
            }
*/
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            // This function is called after a Visual Studio project is closed

            // Get the actual Project object from the IVsHierarchy object that was supplied
/*            var ClosedProject = Utils.HierarchyObjectToProject(pHierarchy);
            if (ClosedProject != null && OnProjectClosed != null)
            {
                LoadedProjectPaths.Remove(ClosedProject.FullName);
                OnProjectClosed(ClosedProject);
            }
*/
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
/*            StopTicker();

            if (OnSolutionClosing != null)
            {
                OnSolutionClosing();
            }
*/
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion

        /// IDispose pattern lets us clean up our stuff!
		protected override void Dispose(bool disposing)
        {
/*            if (Ticker != null && Ticker.IsAlive)
            {
                Thread.Sleep(TickPeriod + TickPeriod);
                if (Ticker.IsAlive)
                {
                    Logging.WriteLine("WARNING: Force aborting Ticker thread");
                    Ticker.Abort();
                }
            }
*/
            base.Dispose(disposing);

            // Clean up singleton instance
            //PrivateInstance = null;

/*            CommandLineEditor = null;
            StartupProjectSelector = null;
            BatchBuilder = null;
            QuickBuilder = null;

            if (CompileSingleFile != null)
            {
                CompileSingleFile.Dispose();
                CompileSingleFile = null;
            }
*/
            // No longer want solution events
            if (SolutionEventsHandle != 0)
            {
                sln_mgr.UnadviseSolutionEvents(SolutionEventsHandle);
                SolutionEventsHandle = 0;
            }
            sln_mgr = null;

/*            // No longer want selection events
            if (SelectionEventsHandle != 0)
            {
                SelectionManager.UnadviseSelectionEvents(SelectionEventsHandle);
                SelectionEventsHandle = 0;
            }
            SelectionManager = null;

            // No longer want update solution events
            if (UpdateSolutionEventsHandle != 0)
            {
                SolutionBuildManager.UnadviseUpdateSolutionEvents(UpdateSolutionEventsHandle);
                UpdateSolutionEventsHandle = 0;
            }
            SolutionBuildManager = null;
*/
            KUE4VS.Logging.WriteLine("Closing KantanUE4VS extension");
            KUE4VS.Logging.Close();
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
    }
}
