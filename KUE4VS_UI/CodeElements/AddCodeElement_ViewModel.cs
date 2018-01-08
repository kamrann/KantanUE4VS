
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
using KUE4VS;
using System.Collections.ObjectModel;

namespace KUE4VS_UI
{
    public class AddCodeElement_ViewModel : ViewModelBase
    {
        AddCodeElementTask _task;

        public AddCodeElement_ViewModel(AddCodeElementTask model)
        {
            _task = model;
            Model.PropertyChanged += OnModelPropertyChanged;
        }

        public AddCodeElementTask Model
        {
            get
            {
                return _task;
            }
        }

        protected virtual void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args) { }

        public ObservableCollection<ModuleRef> AvailableModules
        {
            get
            {
                return ExtContext.Instance.AvailableModules;
            }
        }

        public ObservableCollection<ModuleHost> AvailableModuleHosts
        {
            get
            {
                return ExtContext.Instance.AvailableModuleHosts;
            }
        }

        public ObservableCollection<UProject> AvailableUProjects
        {
            get
            {
                return ExtContext.Instance.AvailableUProjects;
            }
        }

        /*        private ICommand _elem_name_changed_cmd;
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
                        */
    }
}
