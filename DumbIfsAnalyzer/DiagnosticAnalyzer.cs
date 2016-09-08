using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DumbIfsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DumbIfsAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DumbIfs";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Awful code";

        private static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
                Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            var condigionalExpression = IsEvaluable(ifStatement.Condition, context.SemanticModel);

            if (condigionalExpression.HasValue)
            {
                var diagnostic = Diagnostic.Create(Rule, ifStatement.Condition.GetLocation(), condigionalExpression);
                context.ReportDiagnostic(diagnostic);
            }

        }

        private static bool? IsEvaluable(ExpressionSyntax condition, SemanticModel semanticModel)
        {
            var binaryExpression = condition as BinaryExpressionSyntax;
            if (binaryExpression == null) return false;

            var literal = binaryExpression.Left as LiteralExpressionSyntax ?? binaryExpression.Right as LiteralExpressionSyntax;
            if (literal == null)
                return null;

            IdentifierNameSyntax identifier = null;

            var member = binaryExpression.Left as MemberAccessExpressionSyntax ?? binaryExpression.Right as MemberAccessExpressionSyntax;
            if (member != null)
            {
                var nodes = member.ChildNodes().ToList();

                if (nodes.Count == 2)
                {
                    identifier = nodes[1] as IdentifierNameSyntax;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                identifier = binaryExpression.Left as IdentifierNameSyntax ?? binaryExpression.Right as IdentifierNameSyntax;
            }

            if (identifier == null)
                return null;

            var info = semanticModel.GetTypeInfo(identifier);
            if (info.Type == null)
                return null;

            if (literal.IsKind(SyntaxKind.NullLiteralExpression) && !info.Type.IsReferenceType)
            {
                if(binaryExpression.IsKind(SyntaxKind.EqualsExpression))
                {
                    return false;
                }
                else if (binaryExpression.IsKind(SyntaxKind.NotEqualsExpression))
                {
                    return true;
                }
            }

            return null;
        }

    }
}
