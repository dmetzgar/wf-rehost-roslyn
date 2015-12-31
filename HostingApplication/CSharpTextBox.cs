using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Activities.Presentation.Hosting;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HostingApplication
{
    class CSharpTextBox : TextEditor
    {
        CompletionWindow completionWindow;
        readonly MetadataReference[] baseAssemblies;
        readonly string usingNamespaces;

        public CSharpTextBox(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType, Size initialSize) 
        {
            this.TextArea.TextEntering += TextArea_TextEntering;
            this.TextArea.TextEntered += TextArea_TextEntered;
            this.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            this.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            this.FontSize = 12;
            this.Width = initialSize.Width;
            this.Height = initialSize.Height;
            this.Text = text;

            var references = new List<MetadataReference>();

            foreach (var assembly in assemblies.AllAssemblyNamesInContext)
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load(assembly).Location));
                }
                catch { }
            }

            baseAssemblies = references.ToArray();

            usingNamespaces = string.Join("", importedNamespaces.ImportedNamespaces.Select(ns => "using " + ns + ";\n").ToArray());

            string s;
            foreach (var variable in variables)
            {
                s = variable.ToString();
            }
        }

        private void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                try
                {
                    string startString = usingNamespaces + "namespace SomeNamespace { public class NotAProgram { private void SomeMethod() { var blah = ";
                    //string endString = " } } }";

                    var tree = CSharpSyntaxTree.ParseText(startString + this.Text.Substring(0, this.CaretOffset));
                    var compilation = CSharpCompilation.Create(
                        "MyCompilation",
                        syntaxTrees: new[] { tree },
                        references: baseAssemblies);
                    var semanticModel = compilation.GetSemanticModel(tree);

                    // Ask for symbols at the caret position.
                    var position = this.CaretOffset + startString.Length - 1;
                    var token = tree.GetRoot().FindToken(position);
                    var identifier = token.Parent;
                    IList<ISymbol> symbols = null;
                    if (identifier is QualifiedNameSyntax)
                    {
                        var semanticInfo = semanticModel.GetTypeInfo((identifier as QualifiedNameSyntax).Left);
                        var type = semanticInfo.Type;
                        symbols = semanticModel.LookupSymbols(position, container: type, includeReducedExtensionMethods: true);
                    }
                    else if (identifier is MemberAccessExpressionSyntax)
                    {
                        var semanticInfo = semanticModel.GetTypeInfo((identifier as MemberAccessExpressionSyntax).Expression);
                        var type = semanticInfo.Type;
                        symbols = semanticModel.LookupSymbols(position, container: type, includeReducedExtensionMethods: true);
                    }
                    else if (identifier is IdentifierNameSyntax)
                    {
                        var semanticInfo = semanticModel.GetTypeInfo(identifier as IdentifierNameSyntax);
                        var type = semanticInfo.Type;
                        symbols = semanticModel.LookupSymbols(position, container: type, includeReducedExtensionMethods: true);
                    }

                    if (symbols != null && symbols.Count > 0)
                    {
                        completionWindow = new CompletionWindow(this.TextArea);
                        IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                        //var distinctSymbols = (from s in symbols select s.Name).Distinct();
                        var distinctSymbols = from s in symbols group s by s.Name into g select new { Name = g.Key, Symbols = g };
                        foreach (var group in distinctSymbols.OrderBy(s => s.Name))
                        {
                            data.Add(new QueryCompletionData(group.Name, group.Symbols.ToArray()));
                        }

                        completionWindow.Show();
                        completionWindow.Closed += delegate
                        {
                            completionWindow = null;
                        };
                    }
                }
                catch { }
            }
        }

        private void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }
    }
}
