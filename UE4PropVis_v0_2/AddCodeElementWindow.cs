namespace UE4PropVis_VSIX
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using KUE4VS_UI;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("f48c2d46-20fc-4ea5-af86-de0880d89754")]
    public class AddCodeElementWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddCodeElementWindow"/> class.
        /// </summary>
        public AddCodeElementWindow() : base(null)
        {
            this.Caption = "Add UE4 Code Elements";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new AddCodeElementWindowControl();
        }

        public static AddCodeElementWindowControl GetControlInstance()
        {
            var window = (AddCodeElementWindow)PackageProvider.Pkg.FindToolWindow(typeof(AddCodeElementWindow), 0, true);
            return (AddCodeElementWindowControl)window.Content;
        }
    }
}
