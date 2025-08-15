using Microsoft.CodeAnalysis;

namespace Define.SourceGen.Generators.Data.ConfigGenParts;

public abstract class MinOrMaxGenPart : IConfigGenPart
{
    public string Comparison { get; }
    public bool Enforce { get; }
    public string MemberName { get; }
    
    protected abstract char ComparisonOperator { get; }
    protected abstract string ComparisonText { get; }

    private readonly string suffix;
    
    protected MinOrMaxGenPart(string memberName, string comparison, bool enforce, TypedConstant constant)
    {
        MemberName = memberName;
        Comparison = comparison;
        Enforce = enforce;
        
        string typeName = constant.Type?.Name ?? "";
        suffix = typeName switch
        {
            "Single" => "f",
            "Decimal" => "m",
            _ => ""
        };
    }
    
    public string? GenerateBody(DefGenData def, MemberGenData member)
    {
        if (!Enforce)
        {
            return 
            $$"""
            if ({{MemberName}} {{ComparisonOperator}} {{Comparison + suffix}})
            {
                config.Error("The <{{MemberName}}> field is {{ComparisonText}} value of {{Comparison}}.");
            }
            """;
        }
        return
        $$"""
         if ({{MemberName}} {{ComparisonOperator}} {{Comparison + suffix}})
         {
             config.Error("The <{{MemberName}}> field is {{ComparisonText}} value of {{Comparison}}.\nIt has been set to {{Comparison}}.");
            {{MemberName}} = {{Comparison + suffix}};
         }
         """;
    }
}
