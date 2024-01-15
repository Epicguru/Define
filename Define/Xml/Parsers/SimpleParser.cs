using System.Diagnostics;
using System.Text;

namespace Define.Xml.Parsers;

/// <summary>
/// A parser class that can be used to quickly make
/// a parser that can handle a single specific type, and can parse from just a string.
/// The parsing is done by the <see cref="ParseFunction"/> which is passed in to the
/// constructor.
/// </summary>
/// <typeparam name="T">The single specific type that this parser can handle.</typeparam>
public sealed class SimpleParser<T> : XmlParser<T>
{
    /// <inheritdoc/>
    public override bool CanParseNoContext => true;

    /// <summary>
    /// The function that is used to convert strings to objects
    /// of type <typeparamref name="T"/>.
    /// This function is provided via the constructor.
    /// </summary>
    public readonly Func<string, T?> ParseFunction;

    /// <summary>
    /// Creates a new simple parser and sets it up to use the provided
    /// parser function, that takes in a string and outputs an object of type
    /// <typeparamref name="T"/>.
    /// </summary>
    public SimpleParser(Func<string, T?> parseFunc)
    {
        ArgumentNullException.ThrowIfNull(parseFunc);
        ParseFunction = parseFunc;
    }
    
    /// <inheritdoc/>
    public override object? Parse(in XmlParseContext context)
    {
        Debug.Assert(context.TextValue != null);
        return ParseFunction(context.TextValue);
    }
}