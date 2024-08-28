using JetBrains.Annotations;

namespace Define.Xml;

/// <summary>
/// When placed on a class or struct member that would normally be ignored
/// due to the <see cref="DefSerializeConfig"/>, this will make the member be loaded.
/// This does <b>not</b> affect loading or saving using FastCache.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[UsedImplicitly(ImplicitUseKindFlags.Assign)]
public class XmlIncludeAttribute : Attribute;