using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Define.SourceGen.Generators.Data;

public sealed class DefGenData
{
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public IEnumerable<MemberGenData> MemberGenData => memberToDataMap.Values;
    
    private readonly Dictionary<object, MemberGenData> memberToDataMap = [];

    public MemberGenData GetOrCreateMemberData(IFieldSymbol field)
    {
        if (!memberToDataMap.TryGetValue(field, out var found))
        {
            found = new MemberGenData(field);
            memberToDataMap.Add(field, found);
        }
        return found;
    }
    
    public MemberGenData GetOrCreateMemberData(IPropertySymbol property)
    {
        if (!memberToDataMap.TryGetValue(property, out var found))
        {
            found = new MemberGenData(property);
            memberToDataMap.Add(property, found);
        }
        return found;
    }
}
