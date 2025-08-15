using System;
using JetBrains.Annotations;
using SourceGenerator.Helper.CopyCode;

namespace Define.SourceGen.Attributes;

/// <summary>
/// When placed on a def member, it is required to not be null by the time
/// ConfigErrors is called.
/// This attribute is only valid on reference types or nullable value types.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
[Copy]
internal sealed class RequiredAttribute : Attribute;