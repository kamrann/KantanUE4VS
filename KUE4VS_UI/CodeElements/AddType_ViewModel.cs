
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
    public class AddType_ViewModel : AddCodeElement_ViewModel
    {
        public AddType_ViewModel(AddTypeTask model) : base(model)
        { }

        public AddTypeTask AddTypeModel
        {
            get
            {
                return Model as AddTypeTask;
            }
        }

        public bool EnableBaseClassSelection
        {
            get
            {
                return AddTypeModel.Variant == AddableTypeVariant.UClass;
            }
        }

        public IEnumerable<UE4ClassDefnBase> AvailableBaseClasses
        {
            get
            {
                switch (AddTypeModel.Variant)
                {
                    case AddableTypeVariant.UClass:
                        return EngineTypes.UClasses;

                    default:
                        return null;
                }
            }
        }

        protected override void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case "CustomBaseClassName":
                    OnCustomBaseClassNameChanged();
                    break;
            }
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case "Variant":
                    UpdateBaseClassBox();
                    break;
            }
        }

        // @TODO: Think still need to propagate this to the model.
        // Disabled editable property in the xaml for now.
        string _custom_base_class_name = null;
        public string CustomBaseClassName
        {
            get
            {
                return _custom_base_class_name;
            }

            set
            {
                SetProperty(ref _custom_base_class_name, value);
            }
        }

        void UpdateBaseClassBox()
        {
            OnPropertyChanged("AvailableBaseClasses");

            if (AddTypeModel.Variant == AddableTypeVariant.UClass && ReferenceEquals(AddTypeModel.Base, null))
            {
                AddTypeModel.Base = AvailableBaseClasses.FirstOrDefault();
            }

            OnPropertyChanged("EnableBaseClassSelection");
        }

        void OnCustomBaseClassNameChanged()
        {
            int foo = 10;
            int moo = foo;
        }
    }
}
