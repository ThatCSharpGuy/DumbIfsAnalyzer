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

            bool? expressionValue = GetResult(ifStatement.Condition);

            if (expressionValue.HasValue)
            {
                var diagnostic = Diagnostic.Create(Rule, ifStatement.Condition.GetLocation(), expressionValue.Value);
                context.ReportDiagnostic(diagnostic);
            }

        }

        private bool? GetResult(ExpressionSyntax condition)
        {
            var literalExpression = condition as LiteralExpressionSyntax;
            if (literalExpression != null)
            {
                return Boolean.Parse(literalExpression.Token.ToString());
            }

            var unaryExpresion = condition as PrefixUnaryExpressionSyntax;
            if (unaryExpresion != null && unaryExpresion.IsKind(SyntaxKind.LogicalNotExpression))
            {
                var operandResult = GetResult(unaryExpresion.Operand);
                if (operandResult.HasValue)
                {
                    return !operandResult.Value;
                }
                return null;
            }


            var binaryExpression = condition as BinaryExpressionSyntax;
            if (binaryExpression == null) return null;

            var right = GetResult(binaryExpression.Right);
            var left = GetResult(binaryExpression.Left);

            if (right.HasValue && left.HasValue)
            {
                if (binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    return right.Value && left.Value;
                }
                else if (binaryExpression.IsKind(SyntaxKind.LogicalOrExpression))
                {
                    return right.Value || left.Value;
                }
            }

            return null;
        }
    }
}
