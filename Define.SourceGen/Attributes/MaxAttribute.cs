using System;
using SourceGenerator.Helper.CopyCode;

namespace Define.SourceGen.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Copy]
internal class MaxAttribute : Attribute
{
     public MaxAttribute(object max, bool enforce = true) { }
}
