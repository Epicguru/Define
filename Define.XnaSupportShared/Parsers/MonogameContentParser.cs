using Define.Xml;
using Define.Xml.Parsers;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Content;

namespace Define.Monogame.Parsers;

/// <summary>
/// A Define parser that is used to load a specific Monogame content type <typeparamref name="T"/>
/// from a <see cref="ContentManager"/>.
/// The type can be loaded localized (see <see cref="ContentManager.LoadLocalized{T}"/>)
/// by adding the <c>Localized="true"</c> attribute to the XML node.
/// </summary>
/// <typeparam name="T">The content type to load.</typeparam>
/// <remarks>
/// Creates a new Monogame content parser which will use
/// the specified content manager to load assets.
/// </remarks>
/// <param name="contentManager">The <see cref="ContentManager"/> used to load assets.</param>
public sealed class MonogameContentParser<T>(ContentManager contentManager) : XmlParser<T>
{
    /// <summary>
    /// The content manager that is used to load content.
    /// Should not be null.
    /// </summary>
    [PublicAPI]
    public ContentManager ContentManager { get; } = contentManager ?? throw new ArgumentNullException(nameof(contentManager));

    /// <inheritdoc />
    public override bool CanParseNoContext => true;

    /// <inheritdoc />
    public override object? Parse(in XmlParseContext context)
    {
        string path = context.TextValue;
        if (string.IsNullOrEmpty(path))
            return null;

        bool localized = context.Node?.GetAttributeAsBool("Localized") ?? false;

        return localized ? ContentManager.LoadLocalized<T>(path) : ContentManager.Load<T>(path);
    }
}