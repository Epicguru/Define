namespace Define.SourceGen.Generators.Data.ConfigGenParts;

public sealed class RequiredGenPart : IConfigGenPart
{
    public string? GenerateBody(DefGenData def, MemberGenData member)
    {
        return $"""
                if ({member.Name} == null)
                    config.Error("The <{member.Name}> field must be specified!");
                """;
    }
}