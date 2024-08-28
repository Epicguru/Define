using Define.Xml;
using Define.Xml.Parsers;
using Microsoft.Xna.Framework;

namespace Define.Monogame.Parsers;

/// <summary>
/// Parsers the Monogame/XNA/FNA types
/// <see cref="Vector2"/>, <see cref="Vector3"/> and <see cref="Vector4"/>.
/// </summary>
public class VectorParser : CSVParser<float>
{
    /// <inheritdoc/>
    public override bool CanHandle(Type type)
        => type == typeof(Vector2) ||
           type == typeof(Vector3) ||
           type == typeof(Vector4);

    /// <inheritdoc/>
    protected override int GetExpectedPartCount(in XmlParseContext context)
    {
        if (context.TargetType == typeof(Vector2))
            return 2;
        if (context.TargetType == typeof(Vector3))
            return 3;
        if (context.TargetType == typeof(Vector4))
            return 4;

        throw new NotImplementedException($"Unexpected target type {context.TargetType}.");
    }

    /// <inheritdoc/>
    protected override object? Construct(in XmlParseContext context, ReadOnlySpan<float> parts, char? openingChar, char? closingChar)
    {
        return parts.Length switch
        {
            2 => new Vector2(parts[0], parts[1]),
            3 => new Vector3(parts[0], parts[1], parts[2]),
            4 => new Vector4(parts[0], parts[1], parts[2], parts[3]),
            _ => null
        };
    }
}