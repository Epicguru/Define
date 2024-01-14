using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Define.Xml;
using Define.Xml.Parsers;
using Microsoft.Xna.Framework;

namespace Define.Monogame.Parsers;

/// <summary>
/// Parsers the Monogame/XNA/FNA types
/// <see cref="ColorParser"/>.
/// </summary>
public sealed class ColorParser : CSVParser<float>
{
    private static readonly Dictionary<string, Color> allNamedColors;

    static ColorParser()
    {
        allNamedColors = new Dictionary<string, Color>(128);
        foreach (var prop in typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (prop.PropertyType != typeof(Color))
                continue;

            string name = prop.Name.ToLowerInvariant();
            Color value = (Color)prop.GetValue(null)!;

            allNamedColors.Add(name, value);
        }
    }
    
    /// <inheritdoc />
    public ColorParser()
    {
        OpenAndCloseAreRequired = true;
    }
    
    /// <inheritdoc />
    public override bool CanHandle(Type type) => type == typeof(Color);

    /// <inheritdoc />
    protected override int GetExpectedPartCount(in XmlParseContext context) => 0; // Not used.
    
    /// <inheritdoc />
    protected override bool? IsValidPartCount(int count, in XmlParseContext context)
    {
        return count is 3 or 4;
    }
    
    /// <inheritdoc />
    public override object? Parse(in XmlParseContext context)
    {
        ReadOnlySpan<char> txtTrimmed = context.TextValue.AsSpan().Trim();
        if (txtTrimmed.Length > 64)
            throw new Exception("Color string is way too long, invalid format.");

        Span<char> txtLower = stackalloc char[64];
        int count = txtTrimmed.ToLowerInvariant(txtLower);
        txtLower = txtLower[..count];
        
        // Is it a hex color?
        if (txtLower[0] == '#')
        {
            return ParseAsHex(txtLower);
        }

        // Is it a number based on it's parts?
        if (txtLower[0] == '(')
            return base.Parse(context);
        
        // Assume that it is a named color.
        if (allNamedColors.TryGetValue(txtLower.ToString(), out var found))
            return found;

        throw new Exception($"Failed to find named color called '{txtLower}'. If this color was intended to be a hex color, it should start with a hashtag (#).");
    }

    private static object? ParseAsHex(ReadOnlySpan<char> txtLower)
    {
        /*
        * All this fuckery below is required because Monogame
        * packs its colors with the least significant bit being the red
        * channel and most significant being alpha.
        * Which is the opposite of the standard hex format.
        */
        
        if (txtLower.Length is not (7 or 9))
            throw new Exception($"Malformed hex color input '{txtLower}'");
        
        uint asInt = uint.Parse(txtLower[1..], NumberStyles.HexNumber);
        bool hasAlpha = txtLower.Length == 9;
        if (!hasAlpha)
            asInt = (asInt << 8) | 0b_11111111;
        
        BoundingBox
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetByte(int offsetFromRight)
        {
            unchecked
            {
                return (byte) (asInt >> (8 * offsetFromRight));
            }
        }
        
        Color c = new Color(GetByte(3), GetByte(2), GetByte(1), GetByte(0));

        if (!hasAlpha)
            asInt = (asInt << 8) | 0b_11111111;

        return c;
    }

    /// <inheritdoc />
    protected override object? Construct(in XmlParseContext context, ReadOnlySpan<float> parts, char? openingChar, char? closingChar)
    {
        Color c = new Color(parts[0], parts[1], parts[2], 1f);
        if (parts.Length > 3)
            c.A = (byte) (parts[3] * 255);

        return c;
    }
}