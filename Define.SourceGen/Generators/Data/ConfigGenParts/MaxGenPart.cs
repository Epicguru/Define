using Microsoft.CodeAnalysis;

namespace Define.SourceGen.Generators.Data.ConfigGenParts;

public sealed class MaxGenPart
    (string memberName, string comparison, bool enforce, TypedConstant constant)
    : MinOrMaxGenPart(memberName, comparison, enforce, constant)
{
    protected override char ComparisonOperator => '<';
    protected override string ComparisonText => "greater than the maximum";
}
