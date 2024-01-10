namespace Define.Xml;

/// <summary>
/// Specifies alternative names for this field or property.
/// This allows the value to be loaded from XML if the XML node has this specified name.
/// Multiple aliases can be defined.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class AliasAttribute(string name) : Attribute
{
    /// <summary>
    /// The alternative name that this member can have in XML.
    /// </summary>
    public readonly string Alias = name;
}