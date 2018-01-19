// Copyright 2018 Cameron Angus. All Rights Reserved.

using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;

namespace KUE4VS
{
    public abstract class AddCodeElementTask : PropertyChangeNotifyBase
    {
        public CodeElementType ElementType { get; set; }

        string _elem_name = "";
        public string ElementName
        {
            get { return _elem_name; }
            set
            {
                SetProperty(ref _elem_name, value);
            }
        }

        bool _is_valid = false;
        public bool IsValid
        {
            get { return _is_valid; }
            set
            {
                SetProperty(ref _is_valid, value);
            }
        }

        public AddCodeElementTask()
        {}

        public virtual bool DetermineIsNameValid()
        {
            return !String.IsNullOrWhiteSpace(ElementName);
        }

        public virtual bool DetermineIsValid()
        {
            return DetermineIsNameValid();
        }

        public abstract IEnumerable<GenericFileAdditionTask> GenerateAdditions(Project proj);
        public virtual void PostFileAdditions(Project proj) { }

        public virtual bool Execute()
        {
            // @TODO: Should maybe have a dropdown for project selection in the UI and store the selection on the task.
            var proj = Utils.CurrentProjectContext;
            if (proj == null)
            {
                return false;
            }

            var additions = GenerateAdditions(proj);
            if (additions == null)
            {
                return false;
            }

            var result = GenericFileAdditionTask.ProcessFileAdditions(additions, proj, true);

            if (result.AnyFailure)
            {
                return false;
            }

            PostFileAdditions(proj);
            return true;
        }
    }

    public class GenericFileAdditionTask
    {
        public string FileTitle { get; set; }
        public string Extension { get; set; }
        public string FolderPath { get; set; }
        public string Contents { get; set; }

        public class Results
        {
            public bool bFileCreationFailure = false;
            public bool bItemAdditionFailure = false;

            public bool AnyFailure
            {
                get { return bFileCreationFailure || bItemAdditionFailure; }
            }
        }

        public static Results ProcessFileAdditions(IEnumerable< GenericFileAdditionTask> tasks, Project proj, bool all_or_nothing)
        {
            var created = new List<(string path, ProjectItem item)>();

            var results = new Results();
            foreach (var task in tasks)
            {
                var file_path = Path.Combine(task.FolderPath, task.FileTitle + task.Extension);

                try
                {
                    if (File.Exists(file_path))
                    {
                        throw new IOException();
                    }

                    // Create the required directories on disk
                    Directory.CreateDirectory(task.FolderPath);

                    // Create the file
                    {
                        var file = File.CreateText(file_path);
                        created.Add((file_path, null));
                        file.Write(task.Contents);
                        file.Close();
                    }

                    // Add corresponding project items
                    var item = Utils.AddExistingFileItemToProject(proj, file_path, false);

                    if (item == null)
                    {
                        ExtContext.Instance.GetOutputPane().OutputStringThreadSafe(
                            "Failed to add item to project: " + file_path + "\n"
                            );

                        results.bItemAdditionFailure = true;
                        if (all_or_nothing)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        created.RemoveAt(created.Count - 1);
                        created.Add((file_path, item));
                    }
                }
                catch(Exception e)
                {
                    ExtContext.Instance.GetOutputPane().OutputStringThreadSafe(
                        "Exception processing file addition: " + file_path + " [" + e.Message  + "]" + "\n"
                        );

                    results.bFileCreationFailure = true;
                    if (all_or_nothing)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            if (results.AnyFailure && all_or_nothing)
            {
                // Cleanup

                ExtContext.Instance.GetOutputPane().OutputStringThreadSafe(
                    "Cancelling entire generation task..."
                    );

                foreach (var addition in created)
                {
                    if (addition.path != null)
                    {
                        try
                        {
                            File.Delete(addition.path);
                        }
                        catch
                        { }
                    }

                    if (addition.item != null)
                    {
                        try
                        {
                            addition.item.Delete();
                            // @TODO: Ideally also remove any added filters. Meh.
                        }
                        catch
                        { }
                    }
                }
            }
            else
            {
                // We're keeping what we've added, so open them

                foreach (var addition in created)
                {
                    if (addition.item != null)
                    {
                        addition.item.ExpandView();
                        var wnd = addition.item.Open(EnvDTE.Constants.vsViewKindCode);
                        wnd.Activate();
                    }
                }
            }

            return results;
        }
    }
}
