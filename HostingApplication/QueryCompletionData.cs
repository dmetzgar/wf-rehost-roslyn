using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HostingApplication
{
    public class QueryCompletionData : ICompletionData
    {
        private static ImageSource MethodIcon;

        static QueryCompletionData()
        {
            MethodIcon = GetImageSourceFromResource("Method.png");
        }

        static internal ImageSource GetImageSourceFromResource(string resourceName)
        {
            return BitmapFrame.Create(typeof(QueryCompletionData).Assembly.GetManifestResourceStream(typeof(QueryCompletionData).Namespace + "." + resourceName));
        }

        public QueryCompletionData(string name, ISymbol[] symbols)
        {
            this.Text = name;
        }

        public ImageSource Image
        {
            get { return MethodIcon; }
        }

        public string Text { get; private set; }

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content
        {
            get { return this.Text; }
        }

        public object Description
        {
            get { return "Description for " + this.Text; }
        }

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }

        public double Priority
        {
            get { return 1.0; }
        }
    }
}
