namespace Define.Xml.Parsers;

/// <summary>
/// Parses references to other def objects, anything that implements the <see cref="IDef"/>
/// interface.
/// </summary>
public sealed class DefRefParser : XmlParser
{
    /// <inheritdoc/>
    public override bool CanParseNoContext => true;

    /// <inheritdoc/>
    public override bool CanHandle(Type type) => typeof(IDef).IsAssignableFrom(type);

    /// <inheritdoc/>
    public override object Parse(in XmlParseContext context)
    {
        var found = context.Loader.TryGetDef(context.TextValue);
        if (found != null && !context.TargetType.IsInstanceOfType(found))
            throw new Exception($"Def reference '{context.TextValue}' is of type '{found.GetType().FullName}' which cannot be assigned to target def type '{context.TargetType.FullName}'.");

        return found ?? throw new Exception($"Failed to resolve def reference: '{context.TextValue}' as def type '{context.TargetType.FullName}'.");
    }
}