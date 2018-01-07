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
    public abstract class AddCodeElementTask : INotifyPropertyChanged
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

        // @todo: should put this into separate view model
        public class CommandHandler : ICommand
        {
            private Action _action;
            private bool _canExecute;
            public CommandHandler(Action action, bool canExecute)
            {
                _action = action;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                _action();
            }
        }

        private ICommand _elem_name_changed_cmd;
        public ICommand ElementNameChangedCommand
        {
            get
            {
                return _elem_name_changed_cmd ?? (_elem_name_changed_cmd = new CommandHandler(() => OnElementNameChanged(), true));
            }
        }

        protected virtual void OnElementNameChanged()
        {}

        private ICommand _refresh_modules_cmd;
        public ICommand RefreshModulesCommand
        {
            get
            {
                return _refresh_modules_cmd ?? (_refresh_modules_cmd = new CommandHandler(() => OnRefreshModules(), true));
            }
        }

        protected virtual void OnRefreshModules()
        {
            //(FindResource("AvailableModulesSource") as ObjectDataProvider).Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                // Create the required directories on disk
                try
                {
                    var file_path = Path.Combine(task.FolderPath, task.FileTitle + task.Extension);

                    if (File.Exists(file_path))
                    {
                        throw new IOException();
                    }

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
                // Cleanup

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
