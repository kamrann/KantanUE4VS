// Copyright 2018 Cameron Angus. All Rights Reserved.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using EnvDTE80;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[assembly: AssemblyInformationalVersion("KUE4VS 0.6.0")]

namespace KUE4VSPkg
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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(KantanUE4VSPkg.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(AddCodeElementWindow), Width = 400, Height = 300, Style = VsDockStyle.Float)]
    [ProvideOptionPage(typeof(KUE4VS.KUE4VSOptions), ExtensionName, "General", 0, 0, true)]
    [ProvideOptionPage(typeof(UE4PropVis.Config), ExtensionName, "Property Visualization", 0, 0, true)]
    // Force the package to load whenever a solution exists
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class KantanUE4VSPkg :
        AsyncPackage,
        KUE4VS.IExtContext,
        IVsSolutionEvents,
        IDisposable
    {
        /// <summary>
        /// KantanUE4VSPkg GUID string.
        /// </summary>
        public const string PackageGuidString = "58fe42de-aa3c-45b2-a2dd-4ffe1583db46";

        private const string VersionString = "v0.6.0";
        private const string ExtensionName = "KantanUE4VS";
        private const string UnrealSolutionFileNamePrefix = "UE4";

        private const string CodeGenerationOptionKey = ExtensionName + "CodeGeneration";

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

        KUE4VS.KUE4VSOptions KUE4VS.IExtContext.ExtensionOptions
        {
            get
            {
                return (KUE4VS.KUE4VSOptions)GetDialogPage(typeof(KUE4VS.KUE4VSOptions));
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

        void KUE4VS.IExtContext.RefreshModules()
        {
            var old = new List<KUE4VS.ModuleRef>(_cached_modules);
            var updated = new List<KUE4VS.ModuleRef>();
            //_cached_modules.Clear();
            var modules = KUE4VS.Utils.GetAllModules();
            foreach (var m in modules)
            {
                var existing = old.Find(x => x.Equals(m));
                updated.Add(ReferenceEquals(existing, null) ? m : existing);
            }

            _cached_modules = new ObservableCollection<KUE4VS.ModuleRef>(updated);
        }

        void KUE4VS.IExtContext.RefreshModuleHosts()
        {
            var old = new List<KUE4VS.ModuleHost>(_cached_module_hosts);
            var updated = new List<KUE4VS.ModuleHost>();
            //_cached_module_hosts.Clear();
            var hosts = KUE4VS.Utils.GetAllModuleHosts();
            foreach (var h in hosts)
            {
                var existing = old.Find(x => x.Equals(h));
                updated.Add(ReferenceEquals(existing, null) ? h : existing);
            }

            _cached_module_hosts = new ObservableCollection<KUE4VS.ModuleHost>(updated);
        }

        void KUE4VS.IExtContext.RefreshUProjects()
        {
            var old = new List<KUE4VS.UProject>(_cached_uprojects);
            var updated = new List<KUE4VS.UProject>();
            //_cached_uprojects.Clear();
            var projects = KUE4VS.Utils.GetSolutionUProjects();
            foreach (var p in projects)
            {
                var existing = old.Find(x => x.Equals(p));
                updated.Add(ReferenceEquals(existing, null) ? p : existing);
            }

            _cached_uprojects = new ObservableCollection<KUE4VS.UProject>(updated);
        }

        ObservableCollection<KUE4VS.ModuleRef> _cached_modules = new ObservableCollection<KUE4VS.ModuleRef>();
        ObservableCollection<KUE4VS.ModuleRef> KUE4VS.IExtContext.AvailableModules
        {
            get
            {
                return _cached_modules;
            }
        }

        ObservableCollection<KUE4VS.ModuleHost> _cached_module_hosts = new ObservableCollection<KUE4VS.ModuleHost>();
        ObservableCollection<KUE4VS.ModuleHost> KUE4VS.IExtContext.AvailableModuleHosts
        {
            get
            {
                return _cached_module_hosts;
            }
        }

        ObservableCollection<KUE4VS.UProject> _cached_uprojects = new ObservableCollection<KUE4VS.UProject>();
        ObservableCollection<KUE4VS.UProject> KUE4VS.IExtContext.AvailableUProjects
        {
            get
            {
                return _cached_uprojects;
            }
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

            AddOptionKey(CodeGenerationOptionKey);

            KUE4VS.Logging.Initialize(ExtensionName, VersionString);
            KUE4VS.Logging.WriteLine("Loading UnrealVS extension package...");
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            //await base.InitializeAsync(cancellationToken, progress);

            dte = await GetServiceAsync(typeof(DTE)) as DTE2;
                //GetGlobalService(typeof(DTE)) as DTE2;

            sln_mgr = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution2;
            UpdateUnrealLoadedStatus();
            sln_mgr.AdviseSolutionEvents(this, out SolutionEventsHandle);

            build_mgr = await GetServiceAsync(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;

            KUE4VS.ExtContext.Instance = this;
            PackageProvider.Pkg = this;
            UE4PropVis.Config.Instance = (UE4PropVis.Config)GetDialogPage(typeof(UE4PropVis.Config));


            AddNewSourceFileCmd.Initialize(this);
            AddNewClassCmd.Initialize(this);
            AddNewModuleCmd.Initialize(this);
            AddNewPluginCmd.Initialize(this);
            AddCodeElementWindowCommand.Initialize(this);

            base.Initialize();
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
                        Path.GetFileName(solution_filepath).StartsWith(UnrealSolutionFileNamePrefix, StringComparison.OrdinalIgnoreCase)
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

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            UpdateUnrealLoadedStatus();

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
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

        protected override void OnSaveOptions(string key, Stream stream)
        {
            base.OnSaveOptions(key, stream);
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            base.OnLoadOptions(key, stream);
        }

        /// IDispose pattern lets us clean up our stuff!
		protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // No longer want solution events
            if (SolutionEventsHandle != 0)
            {
                sln_mgr.UnadviseSolutionEvents(SolutionEventsHandle);
                SolutionEventsHandle = 0;
            }
            sln_mgr = null;

            KUE4VS.Logging.WriteLine("Closing KantanUE4VS extension");
            KUE4VS.Logging.Close();
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
    }
}
