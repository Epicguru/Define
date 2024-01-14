using Define.Monogame.Parsers;
using Define.Xml;
using Define.Xml.Parsers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Define.Monogame;

/// <summary>
/// A collection of extension methods for use with Monogame and Define.
/// </summary>
public static class MonogameExtensions
{
    /// <summary>
    /// A list of all the parsers that are added when calling <see cref="AddMonogameParsers"/>.
    /// </summary>
    public static List<XmlParser> MonogameParserList { get; } =
    [
        new VectorParser(),
        new RectangleParser(),
        new ColorParser()
    ];
    
    /// <summary>
    /// Adds parsers for all common Monogame data types like <see cref="Vector2"/> or <see cref="Color"/>,
    /// excluding content types such as <see cref="Texture2D"/>.
    /// </summary>
    public static void AddMonogameParsers(this XmlLoader loader)
    {
        foreach (var parser in MonogameParserList)
        {
            loader.AddParser(parser);
        }
    }
}