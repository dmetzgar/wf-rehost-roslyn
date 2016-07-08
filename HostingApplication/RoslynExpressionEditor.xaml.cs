using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Presentation.Expressions;
using System.Activities.Presentation.Hosting;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace HostingApplication
{
    /// <summary>
    /// Interaction logic for RoslynExpressionEditor.xaml
    /// </summary>
    public partial class RoslynExpressionEditor : TextualExpressionEditor
    {
        internal static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(RoslynExpressionEditor));
        internal static readonly DependencyProperty ExpressionTextProperty = DependencyProperty.Register("ExpressionText", typeof(string), typeof(RoslynExpressionEditor), new PropertyMetadata(null));
        internal static readonly DependencyProperty EditingStateProperty = DependencyProperty.Register("EditingState", typeof(EditingState), typeof(RoslynExpressionEditor), new PropertyMetadata(EditingState.Idle));
        internal static readonly DependencyProperty HasValidationErrorProperty = DependencyProperty.Register("HasValidationError", typeof(bool), typeof(RoslynExpressionEditor), new PropertyMetadata(false));
        internal static readonly DependencyProperty ValidationErrorMessageProperty = DependencyProperty.Register("ValidationErrorMessage", typeof(string), typeof(RoslynExpressionEditor), new PropertyMetadata(null));

        double blockHeight = double.NaN;
        double blockWidth = double.NaN;
        private const int ValidationWaitTime = 800;

        bool isEditorLoaded = false;
        string previousText = null;

        IExpressionEditorService expressionEditorService;
        public IExpressionEditorInstance expressionEditorInstance;

        Control hostControl;
        string editorName;
        public TextBox editingTextBox;

        private Type inferredType;

        BackgroundWorker validator = null;

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public RoslynExpressionEditor()
        {
            InitializeComponent();

            this.MinHeight = this.FontSize + 4; /* 4 pixels for border*/

            this.ContentTemplate = (DataTemplate)FindResource("textblock");
            this.innerControl.ContentTemplate = this.ContentTemplate;
            this.HintText = "Enter C# Expression";
        }

        public override bool Commit(bool isExplicitCommit)
        {
            bool committed = false;
            //only generate and validate the expression when when we don't require explicit commit change
            //or when the commit is explicit
            if (!this.ExplicitCommit || isExplicitCommit)
            {
                // Generate and validate the expression.
                // Get the text from the snapshot and set it to the Text property
                this.previousText = null;

                if (this.expressionEditorInstance != null)
                {
                    this.previousText = this.Text;
                    this.Text = this.expressionEditorInstance.GetCommittedText();
                }
                if (this.Expression != null)
                {
                    Activity expression = this.Expression.GetCurrentValue() as Activity;
                    // if expression is null, GetExpressionString will return null                           
                    this.previousText = ExpressionHelper.GetExpressionString(expression, this.OwnerActivity);
                }
                else
                {
                    this.previousText = null;
                }

                if (this.editingTextBox != null)
                {
                    this.editingTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                }

                // If the Text is null, or equal to the previous value, or changed from null to empty, don't bother generating the expression
                // We still need to generate the expression when it is changed from other value to EMPTY however - otherwise
                // the case where you had an expression (valid or invalid), then deleted the whole thing will not be evaluated.
                if (ShouldGenerateExpression(this.previousText, this.Text))
                {
                    GenerateExpression();
                    committed = true;
                }
            }
            if (!this.ContentTemplate.Equals((DataTemplate)FindResource("textblock")))
            {
                this.ContentTemplate = (DataTemplate)FindResource("textblock");
            }
            return committed;
        }

        internal static bool ShouldGenerateExpression(string oldText, string newText)
        {
            return newText != null && !string.Equals(newText, oldText) && !(oldText == null && newText.Equals(string.Empty));
        }

        private void GenerateExpression()
        {
            //TODO: the expression type is hard coded to Int. This should be fixed to dynamically populate the type
            Type resultType = this.ExpressionType != null ? this.ExpressionType : typeof(Int32);

            ////This could happen when:
            ////1) No ExpressionType is specified and
            ////2) The expression is invalid so that the inferred type equals to null
            if (resultType == null)
            {
                resultType = typeof(object);
            }

            // If the text is null we don't need to bother generating the expression (this would be the case the
            // first time you enter an ETB. We still need to generate the expression when it is EMPTY however - otherwise
            // the case where you had an expression (valid or invalid), then deleted the whole thing will not be evaluated.
            if (this.Text != null)
            {
                using (ModelEditingScope scope = this.OwnerActivity.BeginEdit("Property Change"))
                {
                    this.EditingState = EditingState.Validating;
                    // we set the expression to null
                    // a) when the expressionText is empty AND it's a reference expression or
                    // b) when the expressionText is empty AND the DefaultValue property is null
                    if (this.Text.Length == 0 &&
                        (this.UseLocationExpression || (this.DefaultValue == null)))
                    {
                        this.Expression = null;
                    }
                    else
                    {
                        if (this.Text.Length == 0)
                        {
                            this.Text = this.DefaultValue;
                        }

                        ModelTreeManager modelTreeManager = this.Context.Services.GetService<ModelTreeManager>();
                        ActivityWithResult newExpression = CSharpExpressionHelper.CreateExpressionFromString(this.Text, this.UseLocationExpression, resultType);
                        ModelItem expressionItem = modelTreeManager.CreateModelItem(null, newExpression);

                        this.Expression = expressionItem;
                    }
                    scope.Complete();
                }
            }
        }

        internal string ExpressionText
        {
            get { return (string)GetValue(ExpressionTextProperty); }
            set { SetValue(ExpressionTextProperty, value); }
        }

        internal bool HasErrors
        {
            get
            {
                bool hasErrors = false;
                return hasErrors;
            }
        }

        internal bool HasValidationError
        {
            get { return (bool)GetValue(HasValidationErrorProperty); }
            set { SetValue(HasValidationErrorProperty, value); }
        }

        internal string ValidationErrorMessage
        {
            get { return (string)GetValue(ValidationErrorMessageProperty); }
            set { SetValue(ValidationErrorMessageProperty, value); }
        }

        internal EditingState EditingState
        {
            get { return (EditingState)GetValue(EditingStateProperty); }
            set { SetValue(EditingStateProperty, value); }
        }

        private void OnTextBlockMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (!this.IsReadOnly)
            {
                TextBlock textBlock = sender as TextBlock;
                if (textBlock != null)
                {
                    Keyboard.Focus(textBlock);
                    e.Handled = true;
                }
            }
        }

        void OnGotTextBlockFocus(object sender, RoutedEventArgs e)
        {
            if (this.Context == null)
            {
                return;
            }

            DesignerView designerView = this.Context.Services.GetService<DesignerView>();

            if (!designerView.IsMultipleSelectionMode)
            {
                TextBlock textBlock = sender as TextBlock;
                bool isInReadOnlyMode = this.IsReadOnly;
                if (this.Context != null)
                {
                    ReadOnlyState readOnlyState = this.Context.Items.GetValue<ReadOnlyState>();
                    isInReadOnlyMode |= readOnlyState.IsReadOnly;
                }
                if (null != textBlock)
                {
                    this.blockHeight = textBlock.ActualHeight;
                    this.blockHeight = Math.Max(this.blockHeight, textBlock.MinHeight);
                    this.blockHeight = Math.Min(this.blockHeight, textBlock.MaxHeight);
                    this.blockWidth = textBlock.ActualWidth;
                    this.blockWidth = Math.Max(this.blockWidth, textBlock.MinWidth);
                    this.blockWidth = Math.Min(this.blockWidth, textBlock.MaxWidth);

                    // If it's already an editor, don't need to switch it/reload it (don't create another editor/grid if we don't need to)
                    // Also don't create editor when we are in read only mode
                    if (this.ContentTemplate.Equals((DataTemplate)FindResource("textblock")) && !isInReadOnlyMode)
                    {
                        if (this.Context != null)
                        {
                            // Get the ExpressionEditorService
                            this.expressionEditorService = this.Context.Services.GetService<IExpressionEditorService>();
                        }

                        // If the service exists, use the editor template - else switch to the textbox template
                        if (this.expressionEditorService != null)
                        {
                            this.ContentTemplate = (DataTemplate)FindResource("expressioneditor");
                        }

                    }
                }

                if (!isInReadOnlyMode)
                {
                    //disable the error icon
                    this.StartValidator();
                    this.EditingState = EditingState.Editing;
                    e.Handled = true;
                }
            }
        }

        void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            if (!this.isEditorLoaded)
            {
                // If the service exists, create an expression editor and add it to the grid - else switch to the textbox data template
                if (this.expressionEditorService != null)
                {
                    Border border = (Border)sender;
                    // Get the references and variables in scope
                    AssemblyContextControlItem assemblies = (AssemblyContextControlItem)this.Context.Items.GetValue(typeof(AssemblyContextControlItem));
                    List<ModelItem> declaredVariables = GetVariablesInScope(this.OwnerActivity);

                    ImportedNamespaceContextItem importedNamespaces = this.Context.Items.GetValue<ImportedNamespaceContextItem>();
                    importedNamespaces.EnsureInitialized(this.Context);
                    //if the expression text is empty and the expression type is set, then we initialize the text to prompt text
                    if (String.Equals(this.ExpressionText, string.Empty, StringComparison.OrdinalIgnoreCase) && this.ExpressionType != null)
                    {
                        this.Text = TypeToPromptTextConverter.GetPromptText(this.ExpressionType);
                    }

                    //this is a hack
                    this.blockWidth = Math.Max(this.ActualWidth - 8, 0);  //8 is the margin
                    if (this.HasErrors)
                    {
                        this.blockWidth = Math.Max(this.blockWidth - 16, 0); //give 16 for error icon
                    }
                    try
                    {
                        if (this.ExpressionType != null)
                        {
                            this.expressionEditorInstance = this.expressionEditorService.CreateExpressionEditor(assemblies, importedNamespaces, declaredVariables, this.Text, this.ExpressionType, new Size(this.blockWidth, this.blockHeight));
                        }
                        else
                        {
                            this.expressionEditorInstance = this.expressionEditorService.CreateExpressionEditor(assemblies, importedNamespaces, declaredVariables, this.Text, new Size(this.blockWidth, this.blockHeight));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    if (this.expressionEditorInstance != null)
                    {
                        try
                        {
                            this.expressionEditorInstance.VerticalScrollBarVisibility = this.VerticalScrollBarVisibility;
                            this.expressionEditorInstance.HorizontalScrollBarVisibility = this.HorizontalScrollBarVisibility;

                            this.expressionEditorInstance.AcceptsReturn = this.AcceptsReturn;
                            this.expressionEditorInstance.AcceptsTab = this.AcceptsTab;

                            // Add the expression editor to the text panel, at column 1
                            this.hostControl = this.expressionEditorInstance.HostControl;

                            // Subscribe to this event to change scrollbar visibility on the fly for auto, and to resize the hostable editor
                            // as necessary
                            this.expressionEditorInstance.LostAggregateFocus += new EventHandler(OnEditorLostAggregateFocus);
                            this.expressionEditorInstance.Closing += new EventHandler(OnEditorClosing);

                            // Set up Hostable Editor properties
                            this.expressionEditorInstance.MinLines = this.MinLines;
                            this.expressionEditorInstance.MaxLines = this.MaxLines;

                            this.expressionEditorInstance.HostControl.Style = (Style)FindResource("editorStyle");

                            border.Child = this.hostControl;
                            this.expressionEditorInstance.Focus();
                        }
                        catch (KeyNotFoundException ex)
                        {
                            new ApplicationException("Unable to find editor with the following editor name: " + this.editorName);
                        }
                    }
                }
                this.isEditorLoaded = true;
            }
        }

        void OnEditorUnloaded(object sender, RoutedEventArgs e)
        {
            // Blank the editorSession and the expressionEditor so as not to use up memory
            // Destroy both as you can only ever spawn one editor per session
            if (this.expressionEditorInstance != null)
            {
                //if we are unloaded during editing, this means we got here by someone clicking breadcrumb, we should try to commit
                if (this.EditingState == EditingState.Editing)
                {
                    this.Commit(false);
                }
                this.expressionEditorInstance.Close();
            }
            else
            {
                this.editingTextBox = null;
            }

            this.isEditorLoaded = false;
        }

        void OnGotEditingFocus(object sender, RoutedEventArgs e)
        {
            //disable the error icon
            this.EditingState = EditingState.Editing;
            this.StartValidator();
        }
        void StartValidator()
        {
            if (this.validator == null)
            {
                this.validator = new BackgroundWorker();
                this.validator.WorkerReportsProgress = true;
                this.validator.WorkerSupportsCancellation = true;

                this.validator.DoWork += delegate (object obj, DoWorkEventArgs args)
                {
                    BackgroundWorker worker = obj as BackgroundWorker;
                    if (worker.CancellationPending)
                    {
                        args.Cancel = true;
                        return;
                    }
                    ExpressionValidationContext validationContext = args.Argument as ExpressionValidationContext;
                    if (validationContext != null)
                    {
                        string errorMessage;
                        if (DoValidation(validationContext, out errorMessage))
                        {
                            worker.ReportProgress(0, errorMessage);
                        }

                        //sleep
                        if (worker.CancellationPending)
                        {
                            args.Cancel = true;
                            return;
                        }

                        Thread.Sleep(ValidationWaitTime);
                        args.Result = validationContext;
                    }

                };

                this.validator.RunWorkerCompleted += delegate (object obj, RunWorkerCompletedEventArgs args)
                {
                    if (!args.Cancelled)
                    {
                        ExpressionValidationContext validationContext = args.Result as ExpressionValidationContext;
                        if (validationContext != null)
                        {
                            Dispatcher.BeginInvoke(new Action<ExpressionValidationContext>((target) =>
                            {
                                //validator could be null by the time we try to validate again or
                                //if it's already busy
                                if (this.validator != null && !this.validator.IsBusy)
                                {
                                    target.Update(this);
                                    this.validator.RunWorkerAsync(target);
                                }
                            }), validationContext);
                        }
                    }
                };

                this.validator.ProgressChanged += delegate (object obj, ProgressChangedEventArgs args)
                {
                    string error = args.UserState as string;
                    Dispatcher.BeginInvoke(new Action<string>(UpdateValidationError), error);
                };

                this.validator.RunWorkerAsync(new ExpressionValidationContext(this));
            }
        }

        void OnEditorLostAggregateFocus(object sender, EventArgs e)
        {
            this.DoLostFocus();
        }
        void OnEditorClosing(object sender, EventArgs e)
        {
            if (this.expressionEditorInstance != null)
            {
                //these events are expected to be unregistered during lost focus event, but
                //we are unregistering them during unload just in case.  Ideally we want to
                //do this in the CloseExpressionEditor method
                this.expressionEditorInstance.LostAggregateFocus -= new EventHandler(OnEditorLostAggregateFocus);

                this.expressionEditorInstance.Closing -= new EventHandler(OnEditorClosing);
                this.expressionEditorInstance = null;
            }
            Border boarder = this.hostControl.Parent as Border;
            if (boarder != null)
            {
                boarder.Child = null;
            }
            this.hostControl = null;
            this.editorName = null;

        }
        private void KillValidator()
        {
            if (validator != null)
            {
                this.validator.CancelAsync();
                this.validator.Dispose();
                this.validator = null;
            }
        }

        static void ValidateExpression(RoslynExpressionEditor etb)
        {
            string errorMessage;
            if (etb.DoValidation(new ExpressionValidationContext(etb), out errorMessage))
            {
                etb.UpdateValidationError(errorMessage);
            }
        }
        void UpdateValidationError(string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                //report error
                this.HasValidationError = true;
                this.ValidationErrorMessage = errorMessage;
            }
            else
            {
                this.HasValidationError = false;
                this.ValidationErrorMessage = null;
            }
        }

        bool DoValidation(ExpressionValidationContext validationContext, out string errorMessage)
        {
            errorMessage = null;

            //validate
            //if the text is empty we clear the error message
            if (string.IsNullOrEmpty(validationContext.ExpressionText))
            {
                errorMessage = null;
                return true;
            }
            // if the expression text is different from the last time we run the validation we run the validation
            else if (!string.Equals(validationContext.ExpressionText, validationContext.ValidatedExpressionText))
            {
                try
                {
                    //TODO: Add logic to validate expression
                }
                catch (Exception err)
                {
                    errorMessage = err.Message;
                }

                return true;
            }

            return false;
        }

        private void DoLostFocus()
        {
            KillValidator();

            ValidateExpression(this);

            if (this.Context != null)
            {   // Unselect if this is the currently selected one.
                ExpressionSelection current = this.Context.Items.GetValue<ExpressionSelection>();
                if (current != null && current.ModelItem == this.Expression)
                {
                    ExpressionSelection emptySelection = new ExpressionSelection(null);
                    this.Context.Items.SetValue(emptySelection);
                }
            }

            // Generate and validate the expression.
            // Get the text from the snapshot and set it to the Text property
            if (this.expressionEditorInstance != null)
            {
                this.expressionEditorInstance.ClearSelection();
            }

            bool committed = false;
            if (!this.ExplicitCommit)
            {
                //commit change and let the commit change code do the revert
                committed = Commit(false);

                //reset the error icon if we didn't get to set it in the commit
                if (!committed || this.IsIndependentExpression)
                {
                    this.EditingState = EditingState.Idle;
                    // Switch the control back to a textbox -
                    // but give it the text from the editor (textbox should be bound to the Text property, so should
                    // automatically be filled with the correct text, from when we set the Text property earlier)
                    if (!this.ContentTemplate.Equals((DataTemplate)FindResource("textblock")))
                    {
                        this.ContentTemplate = (DataTemplate)FindResource("textblock");
                    }
                }
            }

            //raise EditorLostLogical focus - in case when some clients need to do explicit commit
            this.RaiseEvent(new RoutedEventArgs(ExpressionTextBox.EditorLostLogicalFocusEvent, this));
        }

        internal static List<ModelItem> GetVariablesInScope(ModelItem ownerActivity)
        {
            List<ModelItem> declaredVariables = new List<ModelItem>();
            if (ownerActivity != null)
            {
                bool includeArguments = !(ownerActivity.GetCurrentValue() is ActivityBuilder);
                //TODO: Add logic to GetVariables in Scope and add for ownerActivity 
            }
            return declaredVariables;
        }

        internal sealed class TypeToPromptTextConverter : IValueConverter
        {
            #region IValueConverter Members

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return TypeToPromptTextConverter.GetPromptText(value);
            }

            internal static string GetPromptText(object value)
            {
                Type expressionType = value as Type;
                if (value == DependencyProperty.UnsetValue || expressionType == null || !expressionType.IsValueType)
                {
                    return "Nothing";
                }
                else
                {
                    return Activator.CreateInstance(expressionType).ToString();
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }

            #endregion
        }
    }
}
