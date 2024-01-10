using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Define.Xml.Members;

/// <summary>
/// A structure that represents either a <see cref="FieldInfo"/>
/// or a <see cref="PropertyInfo"/>, with utility methods to
/// read and write to it.
/// </summary>
public readonly struct MemberWrapper
{
    /// <summary>
    /// A <see cref="MemberWrapper"/> that is invalid.
    /// </summary>
    public static readonly MemberWrapper Null = default;

    /// <summary>
    /// Whether or not this member can be read from.
    /// Fields can always be read but properties may not have getters.
    /// </summary>
    public bool CanRead => IsField || property!.GetMethod != null;

    /// <summary>
    /// Whether or not this member can be written to.
    /// Fields can always be read but properties may not have setters.
    /// </summary>
    public bool CanWrite => IsField || property!.SetMethod != null;
    
    /// <summary>
    /// Is this wrapper valid?
    /// If it is not valid, it should not be used as most
    /// methods and properties will result in exceptions.
    /// </summary>
    public bool IsValid => IsProperty || IsField;
    
    /// <summary>
    /// Does this wrapper represent a property?
    /// </summary>
    [MemberNotNullWhen(true, nameof(property))]
    public bool IsProperty => property != null;
    
    /// <summary>
    /// Does this wrapper represent a field?
    /// </summary>
    [MemberNotNullWhen(true, nameof(field))]
    public bool IsField => field != null;
    
    /// <summary>
    /// The name of this field or property.
    /// </summary>
    public string Name => Member.Name;
    
    /// <summary>
    /// The <see cref="Type"/> of this field or property.
    /// </summary>
    public Type Type => IsField ? field.FieldType : property!.PropertyType;
    
    /// <summary>
    /// The backing <see cref="MemberInfo"/>
    /// behind the field or property this wrapper represents.
    /// </summary>
    public MemberInfo Member => IsField ? field : property!;
    
    /// <summary>
    /// An enumeration of all custom attributes on this field or property.
    /// </summary>
    public IEnumerable<CustomAttributeData> Attributes => Member.CustomAttributes;

    /// <summary>
    /// Is this field or property static?
    /// </summary>
    public bool IsStatic => IsField ? field.IsStatic : property!.GetMethod?.IsStatic ?? property.SetMethod!.IsStatic;

    private readonly PropertyInfo? property;
    private readonly FieldInfo? field;

    /// <summary>
    /// Creates a new <see cref="MemberWrapper"/> that represents the property
    /// <paramref name="property"/>.
    /// </summary>
    public MemberWrapper(PropertyInfo property)
    {
        this.property = property ?? throw new ArgumentNullException(nameof(property));
        field = null;
    }

    /// <summary>
    /// Creates a new <see cref="MemberWrapper"/> that represents the field
    /// <paramref name="field"/>.
    /// </summary>
    public MemberWrapper(FieldInfo field)
    {
        this.field = field ?? throw new ArgumentNullException(nameof(field));
        property = null;
    }

    /// <summary>
    /// Enumerates all custom attributes that are of the type <see name="T"/> (or a subclass of <see name="T"/>).
    /// </summary>
    /// <param name="inherit">Whether or not to include inherited attributes.</param>
    public IEnumerable<T> GetAttributes<T>(bool inherit = true) where T : class
        => from attr in Member.GetCustomAttributes(inherit)
           let t = attr as T
           where t != null
           select t;

    /// <summary>
    /// Tries to get a custom attribute that is of the type <see name="T"/> (or a subclass of <see name="T"/>).
    /// </summary>
    /// <param name="inherit">Whether or not to include inherited attributes.</param>
    public T? TryGetAttribute<T>(bool inherit = true) where T : Attribute
        => IsField ? field.GetCustomAttribute<T>(inherit) : property!.GetCustomAttribute<T>(inherit);

    /// <summary>
    /// Attempts to read the value of this field or property provided
    /// an instance of the owning type to read from.
    /// If this is a static member (see <see cref="IsStatic"/>) then the <paramref name="owner"/> should be null.
    /// If this wrapper represents a property without a getter, this method will always return null.
    /// </summary>
    /// <returns>The read object, or null if this is a property without a getter.</returns>
    public object? GetValue(object? owner)
        => IsField ? field.GetValue(owner) : property!.GetMethod != null ? property.GetValue(owner) : null;

    /// <summary>
    /// Attempts to write a value to this field or property provided
    /// an instance of the owning type to write to.
    /// If this is a static member (see <see cref="IsStatic"/>) then the <paramref name="owner"/> should be null.
    /// If this wrapper represents a property without a getter, this method will do nothing.
    /// </summary>
    public void SetValue(object owner, object? value)
    {
        if (IsField)
            field.SetValue(owner, value);
        else if (property!.CanWrite)
            property.SetValue(owner, value);
    }
}
