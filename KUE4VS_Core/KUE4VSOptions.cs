// Copyright 2018 Cameron Angus. All Rights Reserved.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace KUE4VS
{
    [Guid(GuidList.KUE4VSOptionsString)]
    public class KUE4VSOptions : DialogPage
    {
        public event EventHandler OnOptionsChanged;

        private string _source_header_text;

        [Category("Code Generation")]
        [DisplayName("Source File Header")]
        [Description("Text prepended at the top of all generated source files, intended for author/copyright information.")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string SourceFileHeaderText
        {
            get { return _source_header_text; }
            set { _source_header_text = value; }
        }

        public KUE4VSOptions()
        {
            SourceFileHeaderText = @"// [Set a copyright notice via Tools|Options|Kantan UE4VS]";
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (e.ApplyBehavior == ApplyKind.Apply && OnOptionsChanged != null)
            {
                OnOptionsChanged(this, EventArgs.Empty);
            }
        }
    }
}
