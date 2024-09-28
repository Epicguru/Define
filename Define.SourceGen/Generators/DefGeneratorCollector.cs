using System.Collections.Generic;
using System.Linq;
using Define.SourceGen.Generators.Data;
using Define.SourceGen.Generators.Data.ConfigGenParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Define.SourceGen.Generators;

public sealed class DefGeneratorCollector : ISyntaxContextReceiver
{
    private static readonly string RequiredAttributeName = typeof(RequiredAttribute).FullName!;
    private static readonly string AssertAttributeName = typeof(AssertAttribute).FullName!;

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
                    if (context.SemanticModel.GetDeclaredSymbol(item) is not IFieldSymbol fieldSymbol)
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
                member.ConfigGenParts.Add(new AssertGenPart(expression!));
            }
        }
    }
}