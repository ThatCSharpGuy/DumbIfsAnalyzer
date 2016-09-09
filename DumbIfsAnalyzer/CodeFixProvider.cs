using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UselessIfAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUselessIfCodeProvider)), Shared]
    public sealed class RemoveUselessIfCodeProvider : CodeFixProvider
    {
        private const string title = "Delete if";
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(DumbIfsAnalyzerAnalyzer.DiagnosticId);
            }
        }



        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root =
              await context.Document.GetSyntaxRootAsync(context.CancellationToken)
              .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the invocation expression identified by the diagnostic.
            var invocationExpr =
              root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
              .OfType<IfStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
              CodeAction.Create(title, c =>
              FixIfStatement(context.Document, invocationExpr, c), equivalenceKey: title), diagnostic);
        }

        private async Task<Document> FixIfStatement(Document document,
      IfStatementSyntax ifStatement,
      CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var ifContent = ifStatement.Statement;
            var elseContent = ifStatement?.Else?.Statement;
            var condigionalExpression = IfResultAnalyzer.IsEvaluable(ifStatement.Condition, semanticModel);

            List<StatementSyntax> statements;
            var blockStatementSyntax = (condigionalExpression.Value ? ifContent : elseContent) as BlockSyntax;
            if (blockStatementSyntax != null)
            {
                statements = blockStatementSyntax.Statements.ToList();
            }
            else
            {
                statements = new List<StatementSyntax> { (condigionalExpression.Value ? ifContent : elseContent) };
            }


            SyntaxNode newRoot = null;
            var root = await document.GetSyntaxRootAsync();
            // No else
            if (statements != null && !statements.Any(s => s == null))
            {
                var list = SyntaxFactory.List(statements);
                newRoot = root.ReplaceNode(ifStatement, list);
            }
            else
            {
                newRoot = root.RemoveNode(ifStatement, SyntaxRemoveOptions.KeepNoTrivia);
            }

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
