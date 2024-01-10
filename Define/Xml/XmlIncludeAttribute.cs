namespace Define.Xml;

/// <summary>
/// When placed on a class or struct member that would normally be ignored
/// due to the <see cref="DefLoadConfig"/>, this will make the member be loaded.
/// This does <b>not</b> affect loading or saving using Ceras.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class XmlIncludeAttribute : Attribute;