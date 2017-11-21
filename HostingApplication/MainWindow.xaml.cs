using Microsoft.CSharp.Activities;
using Microsoft.Win32;
using System;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Activities.Statements;
using System.IO;
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
        private String xamlFilePath;

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
            Grid.SetRow(this.workflowDesigner.View, 1);

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
            ToolboxCategory category = new ToolboxCategory("Basic Activities");

            // Create Toolbox items.
            ToolboxItemWrapper tool1 =
                new ToolboxItemWrapper("System.Activities.Statements.Assign",
                typeof(Assign).Assembly.FullName, null, "Assign");

            ToolboxItemWrapper tool2 = new ToolboxItemWrapper("System.Activities.Statements.Sequence",
                typeof(Sequence).Assembly.FullName, null, "Sequence");

            ToolboxItemWrapper tool3 = new ToolboxItemWrapper("System.Activities.Statements.WriteLine",
                typeof(Sequence).Assembly.FullName, null, "WriteLine");

            ToolboxItemWrapper tool4 = new ToolboxItemWrapper("System.Activities.Statements.If",
                typeof(Sequence).Assembly.FullName, null, "If");

            ToolboxItemWrapper tool5 = new ToolboxItemWrapper("System.Activities.Statements.While",
                typeof(Sequence).Assembly.FullName, null, "While");

            // Add the Toolbox items to the category.
            category.Add(tool1);
            category.Add(tool2);
            category.Add(tool3);
            category.Add(tool4);
            category.Add(tool5);

            // Add the category to the ToolBox control.
            ctrl.Categories.Add(category);
            return ctrl;
        }

        private void AddToolBox()
        {
            ToolboxControl tc = GetToolboxControl();
            Grid.SetColumn(tc, 0);
            Grid.SetRow(tc, 1);
            grid1.Children.Add(tc);
        }
        private void AddPropertyInspector()
        {
            Grid.SetColumn(workflowDesigner.PropertyInspectorView, 2);
            Grid.SetRow(workflowDesigner.PropertyInspectorView, 1);
            grid1.Children.Add(workflowDesigner.PropertyInspectorView);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.CheckPathExists = true;
            dialog.DefaultExt = ".xaml";
            dialog.Filter = "Xaml files (.xaml)|*xaml|All files|*.*";
            dialog.FilterIndex = 0;
            Boolean? result = dialog.ShowDialog(this);
            if (result.HasValue && result.Value)
            {
                workflowDesigner.Flush();
                xamlFilePath = dialog.FileName;
                SaveXamlFile(xamlFilePath, workflowDesigner.Text);
            }
        }

        private void SaveXamlFile(String path, String markup)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(markup);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("SaveXamlFile exception: {0}:{1}",
                    exception.GetType(), exception.Message);
            }
        }
    }
}

