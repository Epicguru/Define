using System;
using SourceGenerator.Helper.CopyCode;

namespace Define.SourceGen.Attributes;

/// <summary>
/// When placed on a serialized field or property in a def,
/// the specified condition is Asserted during ConfigErrors.
/// The contents of the condition string is a C# expression, with the following pre-processing
/// <list type="bullet">
/// <item>All instances of 'value' (without quotes) are replaced with the name of the target member.</item>
/// <item>If no instances of 'value' are found, then the name of the member is inserted at the start of the expression.
/// For example the condition "!= 5" will be automatically turned into "MemberName != 5"</item>
/// <item>Single quotes around more than once character are converted into double quotes.
/// For example <c>MyString != 'Something'</c> becomes <c>MyString != "Something"</c>.</item>
/// </list> 
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Copy]
public sealed class AssertAttribute : Attribute
{
    public AssertAttribute(string condition, bool isError = true) { }
}