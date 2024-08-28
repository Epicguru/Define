using System.Buffers;
using JetBrains.Annotations;

namespace Define.Xml.Parsers;

/// <summary>
/// An abstract base class for parsers that handle a
/// type that has comma-separated values (CSV), such as a 2D vector like (0, 1, 2).
/// </summary>
/// <typeparam name="TPart">The type of the individual parts of the CSV. Must be parseable from a span.</typeparam>
[PublicAPI]
public abstract class CSVParser<TPart> : XmlParser where TPart : unmanaged, ISpanParsable<TPart>
{
    /// <summary>
    /// The maximum number of parts that any implementation of <see cref="CSVParser{T}"/>
    /// can use. This is needed because variables are stack-allocated for speed,
    /// and this controls the allocation size.
    /// </summary>
    private const int MAX_PARTS_GLOBAL = 32;
    
    /// <inheritdoc/>
    public override bool CanParseNoContext => true;
    
    /// <summary>
    /// The separating character used to split the CSV.
    /// </summary>
    public char Separator { get; protected set; } = ',';
    /// <summary>
    /// The possible characters that can be put at the start of the CSV.
    /// These are normally opening brackets, such as '(' or '{'.
    /// Whether this character is optional is controlled by <see cref="OpenAndCloseAreRequired"/>.
    /// </summary>
    public SearchValues<char> OpeningChars { get; protected set; } = SearchValues.Create(['(']);
    /// <summary>
    /// The possible characters that can be put at the end of the CSV.
    /// These are normally closing brackets, such as ')' or '}'.
    /// Whether this character is optional is controlled by <see cref="OpenAndCloseAreRequired"/>.
    /// </summary>
    public SearchValues<char> ClosingChars { get; protected set; } = SearchValues.Create([')']);
    /// <summary>
    /// If true, the parsed string must be opened by a character that is in the <see cref="OpeningChars"/> set,
    /// and closed by one that is in the <see cref="ClosingChars"/> set.
    /// If false, these characters are optional.
    /// </summary>
    public bool OpenAndCloseAreRequired { get; protected set; }

    /// <inheritdoc/>
    public override object? Parse(in XmlParseContext context)
    {
        ReadOnlySpan<char> txt = context.TextValue.AsSpan().Trim();

        // Detect and remove the first and last characters (normally brackets).
        char? openingChar = null;
        char? closingChar = null;
        bool foundBrackets = false;
        if (txt.Length > 1)
        {
            char first = txt[0];
            char last = txt[^1];
            if (OpeningChars.Contains(first) && ClosingChars.Contains(last))
            {
                txt = txt[1..^1];
                openingChar = first;
                closingChar = last;
                foundBrackets = true;
            }
        }

        if (OpenAndCloseAreRequired && !foundBrackets)
            throw new Exception($"Expected to find a string that starts with {string.Join(" or ", OpeningChars)} and ends with {string.Join(" or ", ClosingChars)}, got '{txt}'");
        
        Span<Range> ranges = stackalloc Range[MAX_PARTS_GLOBAL];
        
        int count = txt.Split(ranges, [Separator], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        bool? allowed = IsValidPartCount(count, context);

        switch (allowed)
        {
            case null:
            
                int expected = GetExpectedPartCount(context);
                if (count != expected)
                    throw new Exception($"Expected between {expected} parts, but got {count}! Input was: '{txt}'");
                break;
            
            case false:
                throw new Exception($"{count} is an invalid number of parts! Input was: '{txt}'");
        }
        
        // Parse the individual parts.
        Span<TPart> parts = stackalloc TPart[MAX_PARTS_GLOBAL];

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<char> partString = txt[ranges[i]];
            if (!TPart.TryParse(partString, null, out TPart parsed))
                throw new Exception($"Failed to parse '{partString}' as a {typeof(TPart).FullName}.");

            parts[i] = parsed;
        }
        
        return Construct(context, parts[..count], openingChar, closingChar);
    }

    /// <summary>
    /// Should return the expected number of parts in this CSV provided the parse
    /// context.
    /// The returned value should be at least 0 and less or equal to <see cref="MAX_PARTS_GLOBAL"/>.
    /// </summary>
    protected abstract int GetExpectedPartCount(in XmlParseContext context);

    /// <summary>
    /// Optional alternative to <see cref="GetExpectedPartCount"/>.
    /// Use this method when the number of parts can be in a specific range.
    /// Return true or false to override the behaviour of <see cref="GetExpectedPartCount"/>.
    /// </summary>
    protected virtual bool? IsValidPartCount(int count, in XmlParseContext context) => null;
    
    /// <summary>
    /// Should return an object of the target type based on the parsed parts
    /// of this CSV.
    /// </summary>
    /// <param name="context">The parse context.</param>
    /// <param name="parts">The individual parsed parts of the CSV.</param>
    /// <param name="openingChar">The opening character, such as a bracket '('. May be null if <see cref="OpenAndCloseAreRequired"/> is false.</param>
    /// <param name="closingChar">The closing character, such as a bracket ')'. May be null if <see cref="OpenAndCloseAreRequired"/> is false.</param>
    protected abstract object? Construct(in XmlParseContext context, ReadOnlySpan<TPart> parts, char? openingChar, char? closingChar);
}