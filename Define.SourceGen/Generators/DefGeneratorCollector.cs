using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Define.SourceGen.Generators;

public sealed class DefGeneratorCollector : ISyntaxContextReceiver
{
    private const string EXAMPLE_ATTR_NAME = "Define.SourceGen.Attributes.ExampleAttribute";
    
    public List<(FieldDeclarationSyntax syntax, IFieldSymbol symbol)> ToGenerate { get; } = new();
    
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

    private void OnVisitField(FieldDeclarationSyntax fieldSyntax, IFieldSymbol fieldSymbol)
    {
        if (!fieldSymbol.HasAttribute(EXAMPLE_ATTR_NAME))
        {
            return;
        }
        
        ToGenerate.Add((fieldSyntax, fieldSymbol));
    }
}