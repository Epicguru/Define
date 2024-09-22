using System;
using JetBrains.Annotations;

namespace Define.SourceGen.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
public sealed class ExampleAttribute : Attribute;