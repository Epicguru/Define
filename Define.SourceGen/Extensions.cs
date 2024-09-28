using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Define.SourceGen;

public static class Extensions
{
    public static bool HasAttribute(this ISymbol symbol, string extensionFullName)
        => symbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == extensionFullName);

    [Pure]
    public static bool HasAttribute(this ISymbol symbol, string extensionFullName, [NotNullWhen(true)] out AttributeData? attribute)
    {
        attribute = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == extensionFullName);
        return attribute != null;
    }

    public static bool IsDeclaredPartial(this INamedTypeSymbol symbol)
        => symbol.DeclaringSyntaxReferences.Any(syntax => syntax.GetSyntax() is BaseTypeDeclarationSyntax declaration && declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
    
    [Pure]
    public static string MakeNestedName(this ITypeSymbol type)
    {
        string output = type.Name;
        bool first = true;

        while (true)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                output = $"{type.Name}+{output}";
            }

            if (type.ContainingType == null)
            {
                output = ((type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace) 
                    ? "" 
                    : $"{type.ContainingNamespace.ToDisplayString()}.") + output;
                break;
            }

            type = type.ContainingType;
        }

        return output;
    }
    
    [Pure]
    public static bool IsSubclassOf(this ITypeSymbol type, string parentFullName)
    {
        while (true)
        {
            if (type.BaseType == null)
            {
                return false;
            }

            if (type.BaseType.ToDisplayString() == parentFullName)
            {
                return true;
            }

            type = type.BaseType;
        }
    }

    [Pure]
    public static bool DoesImplementInterface(this ITypeSymbol type, string interfaceFullName)
    {
        return type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceFullName);
    }
}
