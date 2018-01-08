
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

namespace KUE4VS_UI
{
    public class AddModule_ViewModel : AddCodeElement_ViewModel
    {
        public AddModule_ViewModel(AddModuleTask model) : base(model)
        { }

        public AddModuleTask AddModuleModel
        {
            get
            {
                return Model as AddModuleTask;
            }
        }

        bool _interface_name_manually_set = false;
        bool _indirectly_updating_interface_name = false;

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case "ElementName":
                    UpdateAutoInterfaceName();
                    break;

                case "PublicInterfaceName":
                    OnInterfaceNameChanged();
                    break;
            }
        }

        /*        private ICommand _custom_impl_unchecked_cmd;
                public ICommand CustomImplUncheckedCommand
                {
                    get
                    {
                        return _custom_impl_unchecked_cmd ?? (_custom_impl_unchecked_cmd = new CommandHandler(() => OnCustomImplementationUnchecked(), true));
                    }
                }

                private ICommand _interface_name_changed_cmd;
                public ICommand InterfaceNameChangedCommand
                {
                    get
                    {
                        return _interface_name_changed_cmd ?? (_interface_name_changed_cmd = new CommandHandler(() => OnInterfaceNameChanged(), true));
                    }
                }
        */

        private ICommand _update_auto_interface_name_cmd;
        public ICommand UpdateAutoInterfaceNameCommand
        {
            get
            {
                return _update_auto_interface_name_cmd ?? (_update_auto_interface_name_cmd = new CommandHandler(() => UpdateAutoInterfaceName(), true));
            }
        }

        void OnCustomImplementationUnchecked()
        {
            AddModuleModel.PublicInterfaceName = null;
        }

        void OnInterfaceNameChanged()
        {
            if (!_indirectly_updating_interface_name)
            {
                _interface_name_manually_set = !String.IsNullOrEmpty(AddModuleModel.PublicInterfaceName);
            }
        }

        void UpdateAutoInterfaceName()
        {
            if (!_interface_name_manually_set)
            {
                _indirectly_updating_interface_name = true;
                AddModuleModel.PublicInterfaceName = AddModuleModel.DetermineDefaultInterfaceName();
                _indirectly_updating_interface_name = false;
            }
        }

        /*        private ICommand _refresh_modules_cmd;
                public ICommand RefreshModulesCommand
                {
                    get
                    {
                        return _refresh_modules_cmd ?? (_refresh_modules_cmd = new CommandHandler(() => OnElementNameChanged(), true));
                    }
                }
                */
    }
}
