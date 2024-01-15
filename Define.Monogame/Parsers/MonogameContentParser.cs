using Define.Xml;
using Define.Xml.Parsers;
using Microsoft.Xna.Framework.Content;

namespace Define.Monogame.Parsers;

/// <summary>
/// A Define parser that is used to load a specific Monogame content type <see cref="T"/>
/// from a <see cref="ContentManager"/>.
/// The type can be loaded localized (see <see cref="ContentManager.LoadLocalized{T}"/>)
/// by adding the <c>Localized="true"</c> attribute to the XML node.
/// </summary>
/// <typeparam name="T">The content type to load.</typeparam>
public sealed class MonogameContentParser<T> : XmlParser<T>
{
    /// <summary>
    /// The content manager that is used to load content.
    /// Should not be null.
    /// </summary>
    public ContentManager ContentManager { get; set; }

    /// <inheritdoc />
    public override bool CanParseNoContext => true;

    /// <summary>
    /// Creates a new Monogame content parser which will use
    /// the specified content manager to load assets.
    /// </summary>
    /// <param name="contentManager">The <see cref="ContentManager"/> used to load assets.</param>
    public MonogameContentParser(ContentManager contentManager)
    {
        ContentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
    }

    /// <inheritdoc />
    public override object? Parse(in XmlParseContext context)
    {
        string path = context.TextValue;
        if (string.IsNullOrEmpty(path))
            return null;

        bool localized = context.Node?.GetAttributeAsBool("Localized") ?? false;

        if (localized)
            return ContentManager.LoadLocalized<T>(path);
        
        return ContentManager.Load<T>(path);
    }
}