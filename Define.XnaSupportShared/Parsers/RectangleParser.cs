using System.Buffers;
using Define.Xml;
using Define.Xml.Parsers;
using Microsoft.Xna.Framework;

namespace Define.Monogame.Parsers;

/// <summary>
/// Parsers the Monogame/XNA/FNA type
/// <see cref="Rectangle"/>.
/// </summary>
public class RectangleParser : CSVParser<int>
{
    /// <inheritdoc/>
    public RectangleParser()
    {
        OpeningChars = SearchValues.Create(['(', '[']);
        ClosingChars = SearchValues.Create([')', ']']);
    }

    /// <inheritdoc/>
    public override bool CanHandle(Type type)
        => type == typeof(Rectangle);

    /// <inheritdoc/>
    protected override int GetExpectedPartCount(in XmlParseContext context)
    {
        return 4;
    }

    /// <inheritdoc/>
    protected override object Construct(in XmlParseContext context, ReadOnlySpan<int> parts, char? openingChar, char? closingChar)
    {
        return new Rectangle(parts[0], parts[1], parts[2], parts[3]);
    }
}