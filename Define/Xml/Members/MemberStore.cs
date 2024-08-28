using System.Reflection;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Define.Xml.Members;

/// <summary>
/// A class containing all the known members (fields and properties)
/// for a particular type.
/// </summary>
[PublicAPI]
public class MemberStore
{
    /// <summary>
    /// The type that this object is tracking the members of.
    /// </summary>
    public readonly Type TargetType;
    /// <summary>
    /// The <see cref="DefSerializeConfig"/> that was used when creating this member store - this
    /// changes what types of field and properties are discovered, among other settings.
    /// </summary>
    public readonly DefSerializeConfig Config;

    private readonly Dictionary<string, MemberWrapper> members = [];

    /// <summary>
    /// Creates a new member store for a particular type (<paramref name="targetType"/>)
    /// using a config that defines how members are found.
    /// </summary>
    /// <param name="config">The config to use when finding members on the target type. See <see cref="DefSerializeConfig"/> for more info.</param>
    /// <param name="targetType">The <see cref="Type"/> to look for fields and properties in.</param>
    /// <exception cref="ArgumentNullException">If either parameter is null.</exception>
    public MemberStore(DefSerializeConfig config, Type targetType)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        Config = config ?? throw new ArgumentNullException(nameof(config));

        // Disallow writing ID if the class implements IDef, because that property should only be written by the def database.
        if (targetType.GetInterfaces().Contains(typeof(IDef)))
        {
            members["ID"] = default;
            if (!Config.MemberNamesAreCaseSensitive)
            {
                members["Id"] = default;
                members["id"] = default;
                members["iD"] = default;
            }
        }
    }

    private bool ShouldSee(FieldInfo field)
    {
        if (field.GetCustomAttribute<XmlIncludeAttribute>() != null)
            return true;

        if (field.GetCustomAttribute<XmlIgnoreAttribute>() != null)
            return false;

        if (!Config.DefaultMemberTypes.HasFlag(MemberTypes.Field))
            return false;

        if (field.IsPublic && !Config.DefaultMemberBindingFlags.HasFlag(BindingFlags.Public))
            return false;

        if (!field.IsPublic && !Config.DefaultMemberBindingFlags.HasFlag(BindingFlags.NonPublic))
            return false;

        if (field.IsStatic && !Config.DefaultMemberBindingFlags.HasFlag(BindingFlags.Static))
            return false;
        
        return true;
    }

    private bool ShouldSee(PropertyInfo prop)
    {
        if (prop.SetMethod == null)
            return false;

        if (prop.GetCustomAttribute<XmlIncludeAttribute>() != null)
            return true;

        if (prop.GetCustomAttribute<XmlIgnoreAttribute>() != null)
            return false;

        if (!Config.DefaultMemberTypes.HasFlag(MemberTypes.Property))
            return false;

        if (prop.SetMethod.IsPublic && !Config.DefaultMemberBindingFlags.HasFlag(BindingFlags.Public))
            return false;

        if (!prop.SetMethod.IsPublic && !Config.DefaultMemberBindingFlags.HasFlag(BindingFlags.NonPublic))
            return false;

        if (prop.SetMethod.IsStatic && !Config.DefaultMemberBindingFlags.HasFlag(BindingFlags.Static))
            return false;

        return true;
    }

    /// <summary>
    /// Given a <see cref="MemberInfo"/>, returns all the names that the member can be accessed by.
    /// This is a slow method because it always performs reflection, no caching is done unlike in <see cref="GetMember"/>.
    /// </summary>
    /// <param name="member">The member to get the names of. Valid types are <see cref="FieldInfo"/> and <see cref="PropertyInfo"/>. Must not be null.</param>
    /// <returns>An enumeration of possible names. Will never be null or empty.</returns>
    public static IEnumerable<string> GetNames(MemberInfo member)
    {
        yield return member.Name;

        foreach (var alias in member.GetCustomAttributes<AliasAttribute>())
        {
            foreach (string name in alias.Aliases)
            {
                yield return name;
            }
        }
    }

    private IEnumerable<MemberWrapper> GetMember(Predicate<MemberInfo> selector)
    {
        const BindingFlags ALL = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        foreach (var field in TargetType.GetFields(ALL))
        {
            if (!ShouldSee(field))
                continue;

            if (selector(field))
                yield return new MemberWrapper(field);
        }

        foreach (var prop in TargetType.GetProperties(ALL))
        {
            if (!ShouldSee(prop))
                continue;

            if (selector(prop))
                yield return new MemberWrapper(prop);
        }
    }

    /// <summary>
    /// Attempts to get a field or property (returned as a <see cref="MemberWrapper"/>)
    /// from the <see cref="TargetType"/> based on the member's name.
    /// What fields/properties can be found, and other settings,
    /// can be configured using <see cref="Config"/>.
    /// The return value of this method is cached to repeated calls are fast.
    /// Note: If the target type has multiple members with the same name
    /// (or the same name with different capitalization if <see cref="DefSerializeConfig.MemberNamesAreCaseSensitive"/> is set to false)
    /// then the member returned by this is non-deterministic. Use <see cref="GetMembers"/> instead to enumerate all
    /// possible matches instead.
    /// </summary>
    /// <param name="name">The name of the field or property to look for.</param>
    /// <returns>The member wrapper for the found member, or an invalid wrapper if the member was not found. See <see cref="MemberWrapper.IsValid"/>.</returns>
    public MemberWrapper GetMember(string name)
    {
        if (members.TryGetValue(name, out var f))
            return f;

        var found = GetMembers(name).FirstOrDefault();
        members.Add(name, found);
        return found;
    }

    /// <summary>
    /// Attempts to get all fields or properties (returned as a <see cref="MemberWrapper"/>s)
    /// from the <see cref="TargetType"/> based on the member's name.
    /// What fields/properties can be found, and other settings,
    /// can be configured using <see cref="Config"/>.
    /// Note: If the target type has multiple members with the same name
    /// (or the same name with different capitalization if <see cref="DefSerializeConfig.MemberNamesAreCaseSensitive"/> is set to false)
    /// then the member returned by this is non-deterministic and as such avoiding duplicate field names is encouraged.
    /// The return value of this method is cached to repeated calls are fast.
    /// </summary>
    /// <param name="name">The name of the field or property to look for.</param>
    /// <returns>The member wrapper for the found member, or an invalid wrapper if the member was not found. See <see cref="MemberWrapper.IsValid"/>.</returns>
    public IEnumerable<MemberWrapper> GetMembers(string name)
    {
        var stringComp = Config.MemberNamesAreCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return GetMember(member => GetNames(member).Any(n => n.Equals(name, stringComp)));
    }
    
    /// <summary>
    /// Enumerates all members in the <see cref="TargetType"/> that can be found using the
    /// current <see cref="Config"/>.
    /// Unlike <see cref="GetMember(string)"/>, the results of this method call are not cached:
    /// this is quite slow so use sparingly.
    /// </summary>
    /// <returns>An enumeration of all <see cref="MemberWrapper"/>s on the <see cref="TargetType"/>.</returns>
    public IEnumerable<MemberWrapper> GetAllMembers() => GetMember(_ => true);
}