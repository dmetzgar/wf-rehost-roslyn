using Microsoft.CSharp.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Activities.Statements;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;

namespace HostingApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WorkflowDesigner workflowDesigner;
        private RoslynExpressionEditorService expressionEditorService;

        public MainWindow()
        {
            InitializeComponent();
            RegisterMetadata();
            AddDesigner();
            AddToolBox();
            AddPropertyInspector();
        }

        private void AddDesigner()
        {
            //Create an instance of WorkflowDesigner class.
            this.workflowDesigner = new WorkflowDesigner();

            //Place the designer canvas in the middle column of the grid.
            Grid.SetColumn(this.workflowDesigner.View, 1);

            this.expressionEditorService = new RoslynExpressionEditorService();
            ExpressionTextBox.RegisterExpressionActivityEditor(new CSharpValue<string>().Language, typeof(RoslynExpressionEditor), CSharpExpressionHelper.CreateExpressionFromString);
            this.workflowDesigner.Context.Services.Publish<IExpressionEditorService>(this.expressionEditorService);

            //To avoid loading the default VB expression editor
            DesignerConfigurationService configurationService = this.workflowDesigner.Context.Services.GetService<DesignerConfigurationService>();
            configurationService.TargetFrameworkName = new FrameworkName(".NETFramework", new System.Version(4, 5));
            configurationService.LoadingFromUntrustedSourceEnabled = true;

            //Load a new Sequence as default.
            this.workflowDesigner.Load("StartingWorkflow.xml");

            //Add the designer canvas to the grid.
            grid1.Children.Add(this.workflowDesigner.View);
        }

        private void RegisterMetadata()
        {
            DesignerMetadata dm = new DesignerMetadata();
            dm.Register();
        }

        private ToolboxControl GetToolboxControl()
        {
            // Create the ToolBoxControl.
            ToolboxControl ctrl = new ToolboxControl();

            // Create a category.
            ToolboxCategory category = new ToolboxCategory("category1");

            // Create Toolbox items.
            ToolboxItemWrapper tool1 =
                new ToolboxItemWrapper("System.Activities.Statements.Assign",
                typeof(Assign).Assembly.FullName, null, "Assign");

            ToolboxItemWrapper tool2 = new ToolboxItemWrapper("System.Activities.Statements.Sequence",
                typeof(Sequence).Assembly.FullName, null, "Sequence");

            // Add the Toolbox items to the category.
            category.Add(tool1);
            category.Add(tool2);

            // Add the category to the ToolBox control.
            ctrl.Categories.Add(category);
            return ctrl;
        }

        private void AddToolBox()
        {
            ToolboxControl tc = GetToolboxControl();
            Grid.SetColumn(tc, 0);
            grid1.Children.Add(tc);
        }

        private void AddPropertyInspector()
        {
            Grid.SetColumn(workflowDesigner.PropertyInspectorView, 2);
            grid1.Children.Add(workflowDesigner.PropertyInspectorView);
        }
    }
}
