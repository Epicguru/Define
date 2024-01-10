namespace Define.Xml.Parsers;

/// <summary>
/// An <see cref="XmlParser"/> that handles all
/// enum types.
/// </summary>
public sealed class EnumParser : XmlParser
{
    /// <inheritdoc/>
    public override bool CanParseNoContext => true;
    
    /// <inheritdoc/>
    public override bool CanHandle(Type type) => type.IsEnum;
    
    /// <inheritdoc/>
    public override object Parse(in XmlParseContext context) => Enum.Parse(context.TargetType, context.TextValue);
}