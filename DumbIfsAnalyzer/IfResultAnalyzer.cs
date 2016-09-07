using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumbIfsAnalyzer
{
    public static class IfResultAnalyzer
    {
        public static bool? GetResult(ExpressionSyntax condition)
        {
            var literalExpression = condition as LiteralExpressionSyntax;
            if (literalExpression != null)
            {
                bool value;
                return Boolean.TryParse(literalExpression.Token.ToString(), out value)
                    ? new bool?(value) : null;
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
