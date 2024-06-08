using System.Reflection;
using Ceras;
using JetBrains.Annotations;

namespace Define.FastCache;

/// <summary>
/// Extension methods to work with <see cref="DefFastCache"/>.
/// </summary>
public static class Extensions
{
    private static readonly PropertyInfo[] defConfigProps = typeof(DefSerializeConfig).GetProperties(BindingFlags.Instance | BindingFlags.Public).ToArray();
    
    /// <summary>
    /// Creates a Ceras serializer config based on this <see cref="DefSerializeConfig"/>.
    /// This sets up the serializer such that it matches this config and can handle
    /// <see cref="IDef"/>s correctly.
    /// </summary>
    public static SerializerConfig ToFastCacheConfig(this DefSerializeConfig config)
    {
        var c = new SerializerConfig
        {
            PreserveReferences = true,
            DefaultTargets = MakeTargetMembers(config),
            Advanced =
            {
                ReadonlyFieldHandling = ReadonlyFieldHandling.Members,
                SealTypesWhenUsingKnownTypes = false
            }
        };

        c.ConfigureForDefine();
        return c;
    }

    /// <summary>
    /// This sets up the serializer such that it matches this config and can handle
    /// <see cref="IDef"/>s correctly.
    /// </summary>
    [PublicAPI]
    public static void ConfigureForDefine(this SerializerConfig config)
    {
        // Set IDef.ID to always be included regardless of other settings.
        // This is equivalent to putting the [Include] attribute on it.
        // Because it is an interface, the implementation property needs to be found
        // on each implementation.
        config.OnConfigNewType += t =>
        {
            if (t.IsStatic || t.Type.GetInterfaces().All(type => type != typeof(IDef)))
                return;
            
            // Unfortunately for some reason the Include method is not exposed
            // in the base TypeConfig class, so I have to resort to generics.
            var member = t.Members.First(m => m.Member.Name == nameof(IDef.ID) && m.Member is PropertyInfo);
            member.GetType().GetMethod("Include", [])!.Invoke(member, []);
        };
        
        var def = config.ConfigType<IDef>();
        def.ConfigProperty(nameof(IDef.ID)).Include();
        
        var defConfig = config.ConfigType<DefSerializeConfig>();
        foreach (var prop in defConfigProps)
        {
            defConfig.ConfigProperty(prop.Name).Include();
        }
    }

    /// <summary>
    /// Creates a new <see cref="DefFastCache"/> based on the current contents of
    /// this database. This cache can then be saved to file using <see cref="DefFastCache.Serialize"/>.
    /// </summary>
    public static DefFastCache ToFastCache(this DefDatabase database) => new DefFastCache(database);

    private static TargetMember MakeTargetMembers(DefSerializeConfig config)
    {
        var output = TargetMember.None;
        bool doPublic = (config.DefaultMemberBindingFlags & BindingFlags.Public) != 0;
        bool doPrivate = (config.DefaultMemberBindingFlags & BindingFlags.NonPublic) != 0;
        
        if (config.DefaultMemberTypes.HasFlag(MemberTypes.Field))
        {
            if (doPublic)
                output |= TargetMember.PublicFields;
            if (doPrivate)
                output |= TargetMember.PrivateFields;
        }

        if (!config.DefaultMemberTypes.HasFlag(MemberTypes.Property))
        {
            return output;
        }
        
        if (doPublic)
            output |= TargetMember.PublicProperties;
        if (doPrivate)
            output |= TargetMember.PrivateProperties;

        return output;
    }
}
