using System.Xml;
using JetBrains.Annotations;

namespace Define.Xml.Parsers;

/// <summary>
/// The base class for an object that can turn an <see cref="XmlNode"/>
/// into a C# object of a particular type.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithInheritors)]
public abstract class XmlParser : IComparable<XmlParser>
{
    /// <summary>
    /// Can this parser create an object based just on
    /// <see cref="XmlParseContext.TextValue"/> and <see cref="XmlParseContext.TargetType"/>?
    /// False by default.
    /// </summary>
    public virtual bool CanParseNoContext => false;

    /// <summary>
    /// If multiple <see cref="XmlParser"/>s are capable of handling a particular type
    /// (see <see cref="CanHandle"/>), then this priority decides which parser will be used.
    /// Higher priority means it will be used first.
    /// </summary>
    [PublicAPI]
    public int Priority
    {
        get => priority;
        set
        {
            if (priority == value)
                return;
            
            priority = value;
            
            if (Loader != null)
                Loader.TypeToParserIsDirty = true;
        }
    }
    
    /// <summary>
    /// The <see cref="XmlLoader"/> that this parser has been registered to.
    /// </summary>
    [PublicAPI]
    public XmlLoader? Loader { get; internal set; }

    private int priority;

    /// <summary>
    /// Returns true if this parser can parse the specified type.
    /// </summary>
    public abstract bool CanHandle(Type type);

    /// <summary>
    /// This method should return the parsed C# value based on the <see cref="XmlParseContext"/>
    /// that is passed in.
    /// This should return null to not write a value to the member that is being parsed,
    /// and it should throw an exception if there is a parsing error.
    /// </summary>
    public abstract object? Parse(in XmlParseContext context);

    /// <inheritdoc/>
    public int CompareTo(XmlParser? other)
    {
        if (other == null)
            return 1;
        return Priority - other.Priority;
    }
}

/// <summary>
/// A generic subclass of <see cref="XmlParser"/>.
/// This generic version overrides the <see cref="CanHandle"/> method and makes it only
/// return true if the type <b>exactly</b> matches the type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The exact C# type that this parser supports.</typeparam>
public abstract class XmlParser<T> : XmlParser
{
    /// <summary>
    /// Returns true if this parser can parse the specified type.
    /// Only returns true when <paramref name="t"/> exactly matches the type <typeparamref name="T"/>.
    /// </summary>
    public sealed override bool CanHandle(Type t) => typeof(T) == t;
}
