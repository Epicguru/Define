using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Define;

/// <summary>
/// A class representing the configuration used when loading and saving defs from either XML or FastCache.
/// </summary>
[PublicAPI]
public sealed class DefSerializeConfig : IEquatable<DefSerializeConfig>
{
    /// <summary>
    /// The binding flags for members that will be included in XML/FastCache serialization by default.
    /// Default value is <c>BindingFlags.Public | BindingFlags.Instance</c>
    /// The only valid flags are:
    /// <list type="bullet">
    /// <item><see cref="BindingFlags.Public"/></item>
    /// <item><see cref="BindingFlags.NonPublic"/></item>
    /// <item><see cref="BindingFlags.Instance"/></item>
    /// <item><see cref="BindingFlags.Static"/></item>
    /// </list>
    /// </summary>
    public BindingFlags DefaultMemberBindingFlags { get; set; } = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>
    /// The member types that will be included in XML/FastCache serialization by default.
    /// Only valid flags are <see cref="MemberTypes.Field"/> and <see cref="MemberTypes.Property"/>.
    /// Default value is <c>MemberTypes.Field</c>
    /// </summary>
    public MemberTypes DefaultMemberTypes { get; set; } = MemberTypes.Field;

    /// <summary>
    /// Are field and property names case-sensitive when loading from XML?
    /// Default value is <c>true</c>.
    /// </summary>
    public bool MemberNamesAreCaseSensitive { get; set; } = true;

    /// <summary>
    /// The XML node name for list items.
    /// </summary>
    public string ListItemName { get; set; } = "li";

    /// <summary>
    /// If true, any parsed class that implements <see cref="IPostLoad"/> will have
    /// <see cref="IPostLoad.PostLoad"/> called on it.
    /// You can disable this for a gain in performance.
    /// </summary>
    public bool DoPostLoad { get; set; } = true;

    /// <summary>
    /// If true, any parsed class that implements <see cref="IPostLoad"/> will have
    /// <see cref="IPostLoad.LatePostLoad"/> called on it.
    /// You can disable this for a gain in performance.
    /// </summary>
    public bool DoLatePostLoad { get; set; } = true;

    /// <summary>
    /// If true, any parsed class that implements <see cref="IConfigErrors"/> will have
    /// <see cref="IConfigErrors.ConfigErrors"/> called on it.
    /// You can disable this for a gain in performance.
    /// </summary>
    public bool DoConfigErrors { get; set; } = true;

    /// <inheritdoc/>
    public bool Equals(DefSerializeConfig? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        
        return DefaultMemberBindingFlags == other.DefaultMemberBindingFlags 
               && DefaultMemberTypes == other.DefaultMemberTypes 
               && MemberNamesAreCaseSensitive == other.MemberNamesAreCaseSensitive 
               && ListItemName == other.ListItemName 
               && DoPostLoad == other.DoPostLoad 
               && DoLatePostLoad == other.DoLatePostLoad 
               && DoConfigErrors == other.DoConfigErrors;
    }
    
    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is DefSerializeConfig other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }
}