using System;
using System.Activities.Presentation.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HostingApplication
{
    /// <summary>
    /// Interaction logic for RoslynExpressionEditor.xaml
    /// </summary>
    public partial class RoslynExpressionEditor : TextualExpressionEditor
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(RoslynExpressionEditor));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }


        public RoslynExpressionEditor()
        {
            InitializeComponent();
            this.ContentTemplate = (DataTemplate)FindResource("textblock");
            this.innerControl.ContentTemplate = this.ContentTemplate;
            this.HintText = "Enter C# Expression";
        }

        public override bool Commit(bool isExplicitCommit)
        {
            if (!this.ExplicitCommit || isExplicitCommit)
            {
                if (this.innerControl.Content != null)
                {
                    // Close editor 
                }

                if (isExplicitCommit)
                {
                    //this.GenerateExpression();
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnTextBlockMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
        }

        private void OnTextBlockLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void OnTextBlockGotFocus(object sender, RoutedEventArgs e)
        {
        }
    }
}
