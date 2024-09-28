using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Define.SourceGen.Generators.Data.ConfigGenParts;

public sealed class AssertGenPart : IConfigGenPart
{
    private static readonly Regex StringReplaceRegex
        = new Regex("'..+'", RegexOptions.Compiled);
    
    public AssertGenPart(string rawConditionExpression)
    {
        RawConditionExpression = rawConditionExpression;
    }

    public string RawConditionExpression { get; }

    public string? GenerateBody(DefGenData def, MemberGenData member)
    {
        string condition = TransformCondition(member);
        return $"config.Assert({condition});";
    }

    private string TransformCondition(MemberGenData member)
    {
        string name = member.Name;

        string condition = RawConditionExpression.Trim();
        string replaced = condition.Replace("value", name);
        bool didChange = replaced != condition;

        if (!didChange)
        {
            condition = $"{name} {condition}";
        }

        condition = StringReplaceRegex.Replace(condition, m => $"\"{m.Value[1..^1]}\"");

        return condition;
    }
}