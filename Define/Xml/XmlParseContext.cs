using System.Xml;
using Define.Xml.Members;
using Define.Xml.Parsers;
using JetBrains.Annotations;

namespace Define.Xml;

/// <summary>
/// Represents the current context when parsing from XML for a particular node (<see cref="Node"/>)
/// and for a particular member (<see cref="Member"/>).
/// In some cases, when the parser allows <see cref="XmlParser.CanParseNoContext"/>,
/// there may be no information except for <see cref="TextValue"/> and <see cref="TargetType"/>.
/// </summary>
[PublicAPI]
public readonly struct XmlParseContext
{
    /// <summary>
    /// Is this a valid context?
    /// Returns true when <see cref="TextValue"/> is not null.
    /// </summary>
    public bool IsValid => TextValue != null;

    /// <summary>
    /// The <see cref="XmlLoader"/> that is loading this node.
    /// </summary>
    public XmlLoader Loader { get; init; }
    /// <summary>
    /// The <see cref="XmlNode"/> that is being parsed.
    /// May be null, but <see cref="TextValue"/> will never be.
    /// </summary>
    public XmlNode? Node { get; init; }
    /// <summary>
    /// The target type for this node.
    /// Will never be null.
    /// </summary>
    public Type TargetType { get; init; }
    /// <summary>
    /// The default type for this node. You probably want to read <see cref="TargetType"/> instead.
    /// This value is almost always equal to <see cref="Member"/>.Type.
    /// Will never be null.
    /// </summary>
    public Type DefaultType { get; init; }
    /// <summary>
    /// The text value that is being parsed.
    /// It is normally equal to <see cref="XmlNode.InnerText"/>,
    /// but <see cref="Node"/> may be null whereas this never will.
    /// </summary>
    public string TextValue { get; init; }
    /// <summary>
    /// The member that is being written to.
    /// May be invalid (null), check <see cref="MemberWrapper.IsValid"/>.
    /// </summary>
    public MemberWrapper Member { get; init; }
    /// <summary>
    /// The current value of the node that is being serialized.
    /// May be null.
    /// </summary>
    public object? CurrentValue { get; init; }
    /// <summary>
    /// The index within the parent list. Only valid if a list is being serialized.
    /// </summary>
    public int ListIndex { get; init; }
    /// <summary>
    /// The key within the parent dictionary. Only valid if a dictionary is being serialized.
    /// </summary>
    public object? DictionaryKey { get; init; }
    /// <summary>
    /// Gets the owner object for this node.
    /// This is normally an instance the class that contains the <see cref="Member"/>,
    /// but it may be the list or dictionary in certain contexts.
    /// May be null if parsing from text alone, or other edge cases.
    /// </summary>
    public object? Owner { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Node is XmlElement e ? e.GetFullXPath() : TextValue;
}