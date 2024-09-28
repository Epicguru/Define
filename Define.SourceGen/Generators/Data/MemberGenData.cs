using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Define.SourceGen.Generators.Data.ConfigGenParts;
using Microsoft.CodeAnalysis;

namespace Define.SourceGen.Generators.Data;

public sealed class MemberGenData
{
    [MemberNotNullWhen(true, nameof(FieldSymbol))]
    public bool IsField => FieldSymbol != null;
    public string Name => IsField ? FieldSymbol.Name : PropertySymbol!.Name;
    
    public IFieldSymbol? FieldSymbol { get; }
    public IPropertySymbol? PropertySymbol { get; }
    public List<IConfigGenPart> ConfigGenParts { get; } = [];
    public string? ConfigMethodContents { get; set; }

    public MemberGenData(IFieldSymbol fieldSymbol)
    {
        FieldSymbol = fieldSymbol;
    }

    public MemberGenData(IPropertySymbol propertySymbol)
    {
        PropertySymbol = propertySymbol;
    }

    public string GetConfigMethodContents(DefGenData defGenData)
    {
        IEnumerable<string> contents =
            from part in ConfigGenParts
            let body = part.GenerateBody(defGenData, this)
            where !string.IsNullOrEmpty(body)
            select body;

        return string.Join("\n", contents);
    }
    
    public override string ToString() => Name;
}
