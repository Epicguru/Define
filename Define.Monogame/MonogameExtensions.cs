using Define.Monogame.Parsers;
using Define.Xml;
using Define.Xml.Parsers;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Define.Monogame;

/// <summary>
/// A collection of extension methods for use with Monogame and Define.
/// </summary>
public static class MonogameExtensions
{
    /// <summary>
    /// A list of all the parsers that are added when calling <see cref="AddMonogameDataParsers"/>.
    /// </summary>
    [PublicAPI]
    public static List<XmlParser> MonogameParserList { get; } =
    [
        new VectorParser(),
        new RectangleParser(),
        new ColorParser()
    ];
    
    /// <summary>
    /// Adds parsers for many common Monogame data types like <see cref="Vector2"/> or <see cref="Color"/>,
    /// excluding content types such as <see cref="Texture2D"/>.
    /// For content types such as <see cref="Texture2D"/>, call the <see cref="AddMonogameContentParsers"/>.
    /// The following asset types are supported:
    /// <list type="bullet">
    /// <item><see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/></item>
    /// <item><see cref="Rectangle"/></item>
    /// <item><see cref="Color"/></item>
    /// </list>
    /// </summary>
    public static void AddMonogameDataParsers(this XmlLoader loader)
    {
        foreach (var parser in MonogameParserList)
        {
            loader.AddParser(parser);
        }
    }
    
    /// <summary>
    /// Adds parsers for common monogame content types such as <see cref="Texture2D"/>
    /// or <see cref="Effect"/>.
    /// The provided <see cref="ContentManager"/> is used to load assets.
    /// The following asset types are supported:
    /// <list type="bullet">
    /// <item><see cref="Texture2D"/></item>
    /// <item><see cref="Effect"/></item>
    /// <item><see cref="SoundEffect"/></item>
    /// <item><see cref="SpriteFont"/></item>
    /// <item><see cref="Model"/></item>
    /// <item><see cref="Song"/></item>
    /// </list>
    /// </summary>
    public static void AddMonogameContentParsers(this XmlLoader loader, ContentManager contentManager)
    {
        void Add<T>() => loader.AddParser(new MonogameContentParser<T>(contentManager));

        Add<Texture2D>();
        Add<Effect>();
        Add<SoundEffect>();
        Add<SpriteFont>();
        Add<Model>();
        Add<Song>();
    }
}