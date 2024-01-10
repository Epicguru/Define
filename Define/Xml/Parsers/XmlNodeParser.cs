using System.Xml;

namespace Define.Xml.Parsers;

/// <summary>
/// An <see cref="XmlParser"/> that handles <see cref="XmlNode"/>s.
/// The 'parsed' value is simply the input node.
/// </summary>
public sealed class XmlNodeParser : XmlParser<XmlNode>
{
    /// <inheritdoc/>
    public override object? Parse(in XmlParseContext context) => context.Node;
}