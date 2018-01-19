// Copyright 2018 Cameron Angus. All Rights Reserved.

namespace KUE4VSPkg
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
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
            var impl = new AddCodeElementWindowControl();
            impl.ContentUpdated += OnContentUpdated;

            this.Content = impl;
        }

        public void OnContentUpdated(object sender, EventArgs args)
        {
            var frame = Frame as IVsWindowFrame;
            if (frame != null ) // @TODO: only if undocked? && )
            {
                Guid pguidRelativeTo;
                int px;
                int py;
                int pcx;
                int pcy;

                frame.GetFramePos(new[] { VSSETFRAMEPOS.SFP_fSize }, out pguidRelativeTo, out px, out py, out pcx, out pcy);
                var control = Content as AddCodeElementWindowControl;
                control.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                var desired_height = /*control.GetIdealHeight(); /*/(int)control.DesiredSize.Height;
                control.InvalidateMeasure();
                if (pcy < desired_height)
                {
                    frame.SetFramePos(VSSETFRAMEPOS.SFP_fSize, pguidRelativeTo, px, py, pcx, desired_height);
                }
            }
        }

        public static void ShowToolWindow()
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = PackageProvider.Pkg.FindToolWindow(typeof(AddCodeElementWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public static AddCodeElementWindowControl GetControlInstance()
        {
            var window = (AddCodeElementWindow)PackageProvider.Pkg.FindToolWindow(typeof(AddCodeElementWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            return (AddCodeElementWindowControl)window.Content;
        }
    }
}
