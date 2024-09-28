using System.Collections.Generic;
using System.Linq;
using Define.SourceGen.Attributes;
using Define.SourceGen.Generators.Data;
using Define.SourceGen.Generators.Data.ConfigGenParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Define.SourceGen.Generators;

public sealed class DefGeneratorCollector : ISyntaxContextReceiver
{
    private static readonly string RequiredAttributeName = typeof(RequiredAttribute).FullName!;
    private static readonly string AssertAttributeName = typeof(AssertAttribute).FullName!;
    private static readonly string MinAttributeName = typeof(MinAttribute).FullName!;
    private static readonly string MaxAttributeName = typeof(MaxAttribute).FullName!;

    public IEnumerable<DefGenData> DefData => typeToDef.Values;
    public List<Diagnostic> DiagnosticsList { get; } = [];
    
    private readonly Dictionary<INamedTypeSymbol, DefGenData> typeToDef = [];
    
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        switch (context.Node)
        {
            case FieldDeclarationSyntax fieldSyntax:
            {
                foreach (var item in fieldSyntax.Declaration.Variables)
                {
                    if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, item) is not IFieldSymbol fieldSymbol)
                        continue;
                    OnVisitField(fieldSyntax, fieldSymbol);
                }
                break;
            }
        }
    }

    private DefGenData GetDefGenData(INamedTypeSymbol defType)
    {
        if (!typeToDef.TryGetValue(defType, out var found))
        {
            found = new DefGenData
            {
                ClassSymbol = defType
            };
            typeToDef.Add(defType, found);
        }

        return found;
    }
    
    private void OnVisitField(FieldDeclarationSyntax fieldSyntax, IFieldSymbol fieldSymbol)
    {
        var parentType = fieldSymbol.ContainingType;
        if (parentType == null)
        {
            return;
        }

        // [Required] attribute:
        bool isRequired = fieldSymbol.HasAttribute(RequiredAttributeName);
        if (isRequired)
        {
            var def = GetDefGenData(parentType);
            var member = def.GetOrCreateMemberData(fieldSymbol);
            member.ConfigGenParts.Add(new RequiredGenPart());
        }
        
        // [Assert] attribute:
        bool isAssert = fieldSymbol.HasAttribute(AssertAttributeName, out var assertAttr);
        if (isAssert)
        {
            var def = GetDefGenData(parentType);
            var member = def.GetOrCreateMemberData(fieldSymbol);
            string? expression = assertAttr!.ConstructorArguments[0].Value as string;

            if (string.IsNullOrWhiteSpace(expression))
            {
                // Output warning if null or blank condition.
                DiagnosticsList.Add(Diagnostic.Create(
                    Diagnostics.AssertionExpressionNull,
                    assertAttr.AttributeConstructor!.Locations.FirstOrDefault()
                ));
            }
            else
            {
                bool? isError = assertAttr.ConstructorArguments.Length > 1 ? (bool)assertAttr.ConstructorArguments[1].Value! : null;
                
                member.ConfigGenParts.Add(new AssertGenPart(expression!, isError ?? true));
            }
        }
        
        // [Min] and [Max] attribute:
        bool isMin = fieldSymbol.HasAttribute(MinAttributeName, out var minAttr);
        bool isMax = fieldSymbol.HasAttribute(MaxAttributeName, out var maxAttr);
        if (isMin || isMax)
        {
            var def = GetDefGenData(parentType);
            var member = def.GetOrCreateMemberData(fieldSymbol);

            if (isMin)
            {
                var toCompareAgainst = minAttr!.ConstructorArguments[0];
                if (toCompareAgainst.IsNull)
                {
                    // TODO invalid value diagnostic!
                }
                string minValueString = toCompareAgainst.ToCSharpString();
                bool enforce = minAttr.ConstructorArguments.Length <= 1 || (bool)minAttr.ConstructorArguments[1].Value!;
                member.ConfigGenParts.Add(new MinGenPart(member.Name, minValueString, enforce, toCompareAgainst));
            }
            if (isMax)
            {
                var toCompareAgainst = maxAttr!.ConstructorArguments[0];
                if (toCompareAgainst.IsNull)
                {
                    // TODO invalid value diagnostic!
                }
                string maxValueString = toCompareAgainst.ToCSharpString();
                bool enforce = maxAttr.ConstructorArguments.Length <= 1 || (bool)maxAttr.ConstructorArguments[1].Value!;
                member.ConfigGenParts.Add(new MaxGenPart(member.Name, maxValueString, enforce, toCompareAgainst));
            }
        }
    }
}