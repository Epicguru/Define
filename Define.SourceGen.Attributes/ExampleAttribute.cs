using System;

namespace Define.SourceGen.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ExampleAttribute : Attribute;