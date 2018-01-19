// Copyright 2018 Cameron Angus. All Rights Reserved.

namespace KUE4VS_UI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using KUE4VS;
    using KUE4VS.CodeGeneration;
    using System.Windows.Input;
    using System.Windows.Data;
    using System.ComponentModel;

    /// <summary>
    /// Interaction logic for AddCodeElementWindowControl.
    /// </summary>
    public partial class AddCodeElementWindowControl : UserControl
    {
        public event EventHandler ContentUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddCodeElementWindowControl"/> class.
        /// </summary>
        public AddCodeElementWindowControl()
        {
            this.InitializeComponent();
            InitializeContent(CodeElementType.Type);

            this.Loaded += OnLoaded;

            ElementTypeBox.SelectionChanged += ElementTypeSelectionChanged;
        }

        public AddCodeElementTask TaskData { get; private set; }

        public void InitializeContent(CodeElementType ElementType)
        {
            RefreshModules();
            RefreshModuleHosts();
            RefreshUProjects();

            TaskData = null;
            AddCodeElement_ViewModel view_model = null;
            switch (ElementType)
            {
                case CodeElementType.Type:
                    TaskData = new AddTypeTask();
                    view_model = new AddType_ViewModel(TaskData as AddTypeTask);
                    break;
                case CodeElementType.Source:
                    TaskData = new AddSourceFileTask();
                    view_model = new AddCodeElement_ViewModel(TaskData);
                    break;
                case CodeElementType.Module:
                    TaskData = new AddModuleTask();
                    view_model = new AddModule_ViewModel(TaskData as AddModuleTask);
                    break;
                case CodeElementType.Plugin:
                    TaskData = new AddPluginTask();
                    view_model = new AddCodeElement_ViewModel(TaskData);
                    break;
            }

            TaskData.ElementType = ElementType;
            TaskData.PropertyChanged += OnModelChanged;

            ElementTypeBox.DataContext = view_model;
            // @NOTE: Setting this also sets the DataContext to the same object
            AddElementPresenter.Content = view_model;

            // @TODO: Don't understand why, but if leaving the template selector applied in the xaml to choose the template,
            // then we can't access the instantiated template here (nor by explicitly calling ApplyTemplate, or delaying until later).
            var selector = new CodeElementTypeTemplateSelector();
            AddElementPresenter.ContentTemplate = selector.SelectTemplate(view_model, AddElementPresenter);
            AddElementPresenter.ApplyTemplate();

            var name_text_box = AddElementPresenter.ContentTemplate.FindName("ElementNameBox", AddElementPresenter) as FrameworkElement;
            if (name_text_box != null)
            {
                name_text_box.Focus();
            }
            //

            AddBtn.DataContext = TaskData;
            AddFinishBtn.DataContext = TaskData;

            this.ContentUpdated?.Invoke(this, new EventArgs());
        }

        public int GetIdealHeight()
        {
            return (int)ContentStack.DesiredSize.Height;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
        }

        private void OnModelChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.Compare(args.PropertyName, "IsValid") != 0)
            {
                TaskData.IsValid = TaskData.DetermineIsValid();
            }
        }

        private void ElementTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ComboBox;
            InitializeContent((CodeElementType)box.SelectedItem);
        }

        private void OnAddElement(object sender, RoutedEventArgs e)
        {
            PerformAdditionTask();
        }

        private void OnAddElementAndFinish(object sender, RoutedEventArgs e)
        {
            PerformAdditionTask();
        }

        private void PerformAdditionTask()
        {
            if (!TaskData.Execute())
            {
                // @todo: log output
            }
            else
            {
                TaskData.OnPropertyChanged("IsValid");
            }
        }


        private ICommand _refresh_modules_cmd;
        public ICommand RefreshModulesCommand
        {
            get
            {
                return _refresh_modules_cmd ?? (_refresh_modules_cmd = new ViewModelBase.CommandHandler(() => RefreshModules(), true));
            }
        }

        protected virtual void RefreshModules()
        {
            ExtContext.Instance.RefreshModules();

            //var provider_res = FindResource("AvailableModulesSource") as ObjectDataProvider;
            //provider_res.Refresh();
        }


        private ICommand _refresh_hosts_cmd;
        public ICommand RefreshModuleHostsCommand
        {
            get
            {
                return _refresh_hosts_cmd ?? (_refresh_hosts_cmd = new ViewModelBase.CommandHandler(() => RefreshModuleHosts(), true));
            }
        }

        protected virtual void RefreshModuleHosts()
        {
            ExtContext.Instance.RefreshModuleHosts();

            //var provider_res = FindResource("AvailableModuleHostsSource") as ObjectDataProvider;
            //provider_res.Refresh();
        }


        private ICommand _refresh_projects_cmd;
        public ICommand RefreshUProjectsCommand
        {
            get
            {
                return _refresh_projects_cmd ?? (_refresh_projects_cmd = new ViewModelBase.CommandHandler(() => RefreshUProjects(), true));
            }
        }

        protected virtual void RefreshUProjects()
        {
            ExtContext.Instance.RefreshUProjects();

            //var provider_res = FindResource("AvailableProjectsSource") as ObjectDataProvider;
            //provider_res.Refresh();
        }
    }
}
