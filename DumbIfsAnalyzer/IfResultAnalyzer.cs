using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UselessIfAnalyzer
{
    public static class IfResultAnalyzer
    {
        public static bool? IsEvaluable(IfStatementSyntax @if, SemanticModel semanticModel)
        {
            var condition = @if.Condition;

            // Process only simple ifs
            if (@if.Parent.IsKind(SyntaxKind.ElseClause))
                return null;

            // Find if the "if" condition is a simple binary expression
            var binaryExpression = condition as BinaryExpressionSyntax;
            if (binaryExpression == null) return null;

            // Find if one of the parts is a literal
            var literal = binaryExpression.Left as LiteralExpressionSyntax ?? binaryExpression.Right as LiteralExpressionSyntax;
            if (literal == null) return null;

            IdentifierNameSyntax identifier = null;

            // Find if one of the parts is a member expression
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

            // if no identifier is found, return null
            if (identifier == null)
                return null;

            var info = semanticModel.GetTypeInfo(identifier);
            var typeInfo = info.Type as INamedTypeSymbol;
            if (info.Type == null )
                return null;
            

            var nullableType = semanticModel.Compilation.GetTypeByMetadataName("System.Nullable`1");

            if (typeInfo.ConstructedFrom.Equals(nullableType))
                return null;

            if (literal.IsKind(SyntaxKind.NullLiteralExpression) && !info.Type.IsReferenceType)
            {
                if (binaryExpression.IsKind(SyntaxKind.EqualsExpression))
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
