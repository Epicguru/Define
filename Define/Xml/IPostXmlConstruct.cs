namespace Define.Xml;

/// <summary>
/// An interface with a single callback method that is invoked
/// after the implementing object has been parsed from XML and all it's containing
/// members have been populated.
/// Unlike <see cref="IPostLoad"/>, this is called before the def has finished constructing.
/// </summary>
public interface IPostXmlConstruct
{
    /// <summary>
    /// A method called after this object, and all its members, have been parsed
    /// but before any <see cref="IPostLoad"/> or <see cref="IConfigErrors"/> callbacks.
    /// </summary>
    /// <param name="context">The context that was used to parse this object.</param>
    void PostXmlConstruct(in XmlParseContext context);
}