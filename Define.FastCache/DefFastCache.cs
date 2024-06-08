using System.Diagnostics;
using System.Reflection;
using Ceras;
using JetBrains.Annotations;

namespace Define.FastCache;

/// <summary>
/// A cache of an array of <see cref="IDef"/>s that are intended to be saved and loaded
/// from a file in Ceras binary format, for very fast loading that skips the parsing process.
/// </summary>
public class DefFastCache
{
    /// <summary>
    /// The time, in UTC, that this cache was created at.
    /// </summary>
    [Include]
    [PublicAPI]
    public DateTime TimeCreatedUtc { get; private set; }

    /// <summary>
    /// The defs stored in this cache.
    /// </summary>
    [Include]
    public IDef[] Defs { get; [UsedImplicitly] private set; } = [];

    /// <summary>
    /// If static members are serialized (see <see cref="DefSerializeConfig.DefaultMemberBindingFlags"/>) then
    /// this dictionary contains serialized data for those types' static members.
    /// </summary>
    [Include]
    public Dictionary<Type, byte[]> StaticClassData { get; [UsedImplicitly] private set; } = new Dictionary<Type, byte[]>();

    /// <summary>
    /// The config that this <see cref="DefFastCache"/> uses when
    /// saving and loading.
    /// </summary>
    [Include]
    [PublicAPI]
    public DefSerializeConfig Config { get; private set; } = null!;

    /// <summary>
    /// Creates a new <see cref="DefFastCache"/> based on the current contents
    /// and config of the provided database.
    /// </summary>
    public DefFastCache(DefDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(database.Config);

        TimeCreatedUtc = DateTime.UtcNow;
        Defs = database.GetAll().ToArray();
        Config = database.Config;
        
        // Use this to store a list of types that have static data.
        foreach (var type in database.TypesWithStaticData)
            StaticClassData.Add(type, null!);
    }

    /// <summary>
    /// Creates a <see cref="DefFastCache"/> from serialized data.
    /// The <paramref name="config"/> should be identical to the one used to save
    /// the cache in the first place.
    /// Will throw an exception if the loading fails for any reason.
    /// You can later call <see cref="LoadIntoDatabase"/> to put the contents of this cache into a database.
    /// </summary>
    public DefFastCache(byte[] serializedData, DefSerializeConfig config)
    {
        ArgumentNullException.ThrowIfNull(serializedData);
        ArgumentNullException.ThrowIfNull(config);

        var serializer = new CerasSerializer(config.ToFastCacheConfig());
        
        // Ref self is required because Deserialize only has an overload that takes ref.
        DefFastCache self = this;
        serializer.Deserialize(ref self, serializedData);
        
        if (!Config.Equals(config))
        {
            DefDebugger.Warn("The def config that was used to save this FastCache does not match the config used to save it, this can lead to broken defs.");
        }
    }
    
    [CerasConstructor]
    [UsedImplicitly]
    private DefFastCache() { }
    
    /// <summary>
    /// Serializes this <see cref="DefFastCache"/> to a byte array.
    /// This data is suitable for saving to a file.
    /// </summary>
    public byte[] Serialize()
    {
        var bytes = SerializeSelf();
        return bytes;
    }

    private byte[] SerializeSelf()
    {
        Debug.Assert(Config != null);
        
        var config = Config.ToFastCacheConfig();
        var serializer = new CerasSerializer(config);

        // Serialize static class data where necessary.
        PopulateStaticTypeData(serializer);
        
        // Serialize self, includes defs and static class data.
        return serializer.Serialize(this);
    }

    /// <summary>
    /// Puts the contents of this <see cref="DefFastCache"/> into the specified
    /// <see cref="DefDatabase"/>.
    /// If the database already contains defs, they should not have the same ID as the ones
    /// in this cache.
    /// Additionally this method also optionally assigns the value of static fields that were
    /// written during the XML parsing process. This only happens if <paramref name="applyStaticMemberData"/>
    /// is true and <see cref="DefSerializeConfig.DefaultMemberBindingFlags"/> contains <see cref="BindingFlags.Static"/>.
    /// </summary>
    /// <param name="database">The database to populate.</param>
    /// <param name="applyStaticMemberData">If true, static fields/properties that were assigned during XML loading are re-applied here.</param>
    public void LoadIntoDatabase(DefDatabase database, bool applyStaticMemberData = true)
    {
        ArgumentNullException.ThrowIfNull(database);

        // Set config.
        database.Config = Config;
        
        foreach (var def in Defs)
        {
            if (!database.Register(def))
            {
                DefDebugger.Error($"Tried to load a def with duplicate ID '{def.ID}' into the database from this FastCache.");
            }
        }
        
        // Apply static member data.
        if (!applyStaticMemberData)
            return;

        var serializer = new CerasSerializer(Config.ToFastCacheConfig());
        foreach (var pair in StaticClassData)
        {
            serializer.Advanced.DeserializeStatic(pair.Key, pair.Value);
        }
    }
    
    private void PopulateStaticTypeData(CerasSerializer serializer)
    {
        foreach (var type in StaticClassData.Keys)
        {
            var bytes = serializer.Advanced.SerializeStatic(type);
            StaticClassData[type] = bytes;
        }
    }
}
