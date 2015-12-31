using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities.Presentation.View;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Activities.Presentation.Hosting;
using System.Activities.Presentation.Model;

namespace HostingApplication
{
    public class RoslynExpressionEditorInstance : IExpressionEditorInstance
    {
        private readonly CSharpTextBox textBox;

        public bool AcceptsReturn { get; set; }

        public bool AcceptsTab { get; set; }

        public bool HasAggregateFocus
        {
            get
            {
                return true;
            }
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return this.textBox.HorizontalScrollBarVisibility; }
            set { this.textBox.HorizontalScrollBarVisibility = value; }
        }

        public Control HostControl
        {
            get
            {
                return textBox;
            }
        }

        public int MaxLines { get; set; }

        public int MinLines { get; set; }

        public string Text { get; set; }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return this.textBox.VerticalScrollBarVisibility; }
            set { this.textBox.VerticalScrollBarVisibility = value; }
        }

        public event EventHandler Closing;
        public event EventHandler GotAggregateFocus;
        public event EventHandler LostAggregateFocus;
        public event EventHandler TextChanged;

        public RoslynExpressionEditorInstance(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType, Size initialSize)
        {
            this.textBox = new CSharpTextBox(assemblies, importedNamespaces, variables, text, expressionType, initialSize);
        }

        public bool CanCompleteWord()
        {
            return true;
        }

        public bool CanCopy()
        {
            return true;
        }

        public bool CanCut()
        {
            return true;
        }

        public bool CanDecreaseFilterLevel()
        {
            return true;
        }

        public bool CanGlobalIntellisense()
        {
            return true;
        }

        public bool CanIncreaseFilterLevel()
        {
            return true;
        }

        public bool CanParameterInfo()
        {
            return true;
        }

        public bool CanPaste()
        {
            return true;
        }

        public bool CanQuickInfo()
        {
            return true;
        }

        public bool CanRedo()
        {
            return true;
        }

        public bool CanUndo()
        {
            return true;
        }

        public void ClearSelection()
        {
           
        }

        public void Close()
        {

        }

        public bool CompleteWord()
        {
            return true;
        }

        public bool Copy()
        {
            return true;
        }

        public bool Cut()
        {
            return true;
        }

        public bool DecreaseFilterLevel()
        {
            return true;
        }

        public void Focus()
        {
        }

        public string GetCommittedText()
        {
            return "CommittedText";
        }

        public bool GlobalIntellisense()
        {
            return true;
        }

        public bool IncreaseFilterLevel()
        {
            return true;
        }

        public bool ParameterInfo()
        {
            return true;
        }

        public bool Paste()
        {
            return true;
        }

        public bool QuickInfo()
        {
            return true;
        }

        public bool Redo()
        {
            return true;
        }

        public bool Undo()
        {
            return true;
        }
    }
}
