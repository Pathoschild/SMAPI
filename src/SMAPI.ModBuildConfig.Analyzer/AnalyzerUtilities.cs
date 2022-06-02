using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StardewModdingAPI.ModBuildConfig.Analyzer;

/// <summary>Provides generic utilities for SMAPI's Roslyn analyzers.</summary>
internal static class AnalyzerUtilities
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get the metadata for an explicit cast or 'x as y' expression.</summary>
    /// <param name="node">The member access expression.</param>
    /// <param name="semanticModel">provides methods for asking semantic questions about syntax nodes.</param>
    /// <param name="fromExpression">The expression whose value is being converted.</param>
    /// <param name="fromType">The type being converted from.</param>
    /// <param name="toType">The type being converted to.</param>
    /// <returns>Returns true if the node is a matched expression, else false.</returns>
    public static bool TryGetCastOrAsInfo(SyntaxNode node, SemanticModel semanticModel, out ExpressionSyntax fromExpression, out TypeInfo fromType, out TypeInfo toType)
    {
        // (type)x
        if (node is CastExpressionSyntax cast)
        {
            fromExpression = cast.Expression;
            fromType = semanticModel.GetTypeInfo(fromExpression);
            toType = semanticModel.GetTypeInfo(cast.Type);
            return true;
        }

        // x as y
        if (node is BinaryExpressionSyntax binary && binary.Kind() == SyntaxKind.AsExpression)
        {
            fromExpression = binary.Left;
            fromType = semanticModel.GetTypeInfo(fromExpression);
            toType = semanticModel.GetTypeInfo(binary.Right);
            return true;
        }

        // invalid
        fromExpression = null;
        fromType = default;
        toType = default;
        return false;
    }

    /// <summary>Get the metadata for a member access expression.</summary>
    /// <param name="node">The member access expression.</param>
    /// <param name="semanticModel">provides methods for asking semantic questions about syntax nodes.</param>
    /// <param name="declaringType">The object type which has the member.</param>
    /// <param name="memberType">The type of the accessed member.</param>
    /// <param name="memberName">The name of the accessed member.</param>
    /// <returns>Returns true if the node is a member access expression, else false.</returns>
    public static bool TryGetMemberInfo(SyntaxNode node, SemanticModel semanticModel, out ITypeSymbol declaringType, out TypeInfo memberType, out string memberName)
    {
        // simple access
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            declaringType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            memberType = semanticModel.GetTypeInfo(node);
            memberName = memberAccess.Name.Identifier.Text;
            return true;
        }

        // conditional access
        if (node is ConditionalAccessExpressionSyntax { WhenNotNull: MemberBindingExpressionSyntax conditionalBinding } conditionalAccess)
        {
            declaringType = semanticModel.GetTypeInfo(conditionalAccess.Expression).Type;
            memberType = semanticModel.GetTypeInfo(node);
            memberName = conditionalBinding.Name.Identifier.Text;
            return true;
        }

        // invalid
        declaringType = null;
        memberType = default;
        memberName = null;
        return false;
    }

    /// <summary>Get the class types in a type's inheritance chain, including itself.</summary>
    /// <param name="type">The initial type.</param>
    public static IEnumerable<ITypeSymbol> GetConcreteTypes(ITypeSymbol type)
    {
        while (type != null)
        {
            yield return type;
            type = type.BaseType;
        }
    }
}
