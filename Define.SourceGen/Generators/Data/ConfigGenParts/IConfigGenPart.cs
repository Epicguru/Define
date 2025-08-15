namespace Define.SourceGen.Generators.Data.ConfigGenParts;

public interface IConfigGenPart
{
    string? GenerateBody(DefGenData def, MemberGenData member);
}
