using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUE4VS
{
    public abstract class AddCodeElementTask
    {
        public CodeElementType ElementType { get; set; }
        public string ElementName { get; set; }

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
            List<string> created_files = new List<string>();

            var results = new Results();
            foreach (var task in tasks)
            {
                // Create the required directories on disk
                try
                {
                    var dir_info = Directory.CreateDirectory(task.FolderPath);

                    // Create the file
                    var file_path = Path.Combine(task.FolderPath, task.FileTitle + task.Extension);
                    {
                        var file = File.CreateText(file_path);
                        created_files.Add(file_path);
                        file.Write(task.Contents);
                        file.Close();
                    }

                    // Add corresponding project items
                    bool add_ok = Utils.AddExistingFileItemToProject(proj, file_path);

                    if (!add_ok)
                    {
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
                }
                catch
                {
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
                foreach (var file_path in created_files)
                {
                    try
                    {
                        File.Delete(file_path);
                    }
                    catch
                    { }
                }
            }

            return results;
        }
    }
}
