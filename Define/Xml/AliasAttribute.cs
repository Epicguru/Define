namespace Define.Xml;

/// <summary>
/// Specifies alternative names for this field or property.
/// This allows the value to be loaded from XML if the XML node has this specified name.
/// Multiple aliases can be defined.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class AliasAttribute : Attribute
{
    /// <summary>
    /// The alternative name that this member can have in XML.
    /// </summary>
    public readonly string[] Aliases;

    /// <summary>
    /// Marks this member with a single alias.
    /// </summary>
    public AliasAttribute(string alias) => Aliases = [alias];

    /// <summary>
    /// Masks this member with multiple aliases.
    /// </summary>
    public AliasAttribute(params string[] aliases) => Aliases = aliases;
}