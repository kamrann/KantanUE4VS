namespace KUE4VS_UI
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using KUE4VS;
    using KUE4VS.CodeGeneration;

    /// <summary>
    /// Interaction logic for AddCodeElementWindowControl.
    /// </summary>
    public partial class AddCodeElementWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddCodeElementWindowControl"/> class.
        /// </summary>
        public AddCodeElementWindowControl()
        {
            this.InitializeComponent();
            InitializeContent(CodeElementType.Type);

            ElementTypeBox.SelectionChanged += ElementTypeSelectionChanged;
        }

        public AddCodeElementTask TaskData { get; private set; }

        public bool CanAddElement
        {
            get
            {
                return true;// TaskData is AddTypeTask;
            }
        }

        void InitializeContent(CodeElementType ElementType)
        {
            TaskData = null;
            switch (ElementType)
            {
                case CodeElementType.Type:
                    TaskData = new AddTypeTask();
                    ((AddTypeTask)TaskData).bPrivateHeader = true;
                    ((AddTypeTask)TaskData).Location.ModuleName = "foo";
                    ((AddTypeTask)TaskData).Location.RelativePath = "bar";
                    break;
                case CodeElementType.Source:
                    TaskData = new AddSourceFileTask();
                    break;
                case CodeElementType.Module:
                    TaskData = new AddModuleTask();
                    break;
                case CodeElementType.Plugin:
                    TaskData = new AddPluginTask();
                    break;
            }

            TaskData.ElementType = ElementType;
            ElementTypeBox.DataContext = TaskData;
            // @NOTE: Setting this also sets the DataContext to the same object
            AddElementPresenter.Content = TaskData;
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
        }
    }
}
