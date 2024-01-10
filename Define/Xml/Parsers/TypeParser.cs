namespace Define.Xml.Parsers;

/// <summary>
/// A parser that handles parsing <see cref="Type"/>s.
/// It uses <see cref="TypeResolver"/> to locate C# types.
/// </summary>
public class TypeParser : XmlParser<Type>
{
    /// <inheritdoc/>
    public override bool CanParseNoContext => true;

    /// <inheritdoc/>
    public override object? Parse(in XmlParseContext context)
    {
        return TypeResolver.Get(context.TextValue, true);
    }
}