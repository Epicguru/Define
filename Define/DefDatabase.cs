using System.Diagnostics;
using System.Text;
using System.Xml;
using Define.Xml;

namespace Define;

/// <summary>
/// The def database tracks all loaded <see cref="IDef"/>s.
/// It has method to load, register, unregister and get defs.
/// </summary>
public static class DefDatabase
{
    /// <summary>
    /// The total number of defs currently loaded.
    /// </summary>
    public static int Count => allDefs.Count;
    /// <summary>
    /// The number of def containers currently loaded.
    /// The number of containers depends on the inheritance hierarchy of all loaded defs,
    /// as well as the number of unique interfaces they implement.
    /// This value should be used for diagnostics only.
    /// </summary>
    public static int ContainerCount => defsOfType.Count;
    
    /// <summary>
    /// The <see cref="XmlLoader"/> that is used during the def loading process.
    /// This loader is only non-null after <see cref="StartLoading"/> is called
    /// and before <see cref="FinishLoading"/> is called.
    /// This loader can be configured to add or remove parsers.
    /// </summary>
    public static XmlLoader? Loader { get; private set; }

    private static readonly Dictionary<string, IDef> idToDef = new Dictionary<string, IDef>(4096);
    private static readonly List<IDef> allDefs = new List<IDef>(4096);
    private static readonly Dictionary<Type, DefContainer> defsOfType = new Dictionary<Type, DefContainer>(128);
    private static string? finalMasterDocument;
    private static bool isReload;

    /// <summary>
    /// Clears the def database of all defs.
    /// If the loading process is currently active it is cancelled.
    /// </summary>
    public static void Clear()
    {
        idToDef.Clear();
        allDefs.Clear();
        defsOfType.Clear();
        Loader?.Dispose();
        isReload = false;
        Loader = null;
        finalMasterDocument = null;
    }

    /// <summary>
    /// Starts the def loading process using the provided config.
    /// After this call, <see cref="AddDefDocument(XmlDocument, string)"/> and similar methods can be called.
    /// If <paramref name="reloading"/> is true, then the def files are loaded into existing defs if their <see cref="IDef.ID"/>'s match.
    /// </summary>
    /// <param name="config">The <see cref="DefLoadConfig"/> to use when loading.</param>
    /// <param name="reloading">If true, the loaded defs are applied to any existing loaded defs. If false, attempting to load defs with IDs that match existing defs will cause an error.</param>
    /// <exception cref="Exception">If the loading process has already been started and not finished by calling <see cref="FinishLoading"/>.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="config"/> is null.</exception>
    public static void StartLoading(DefLoadConfig config, bool reloading = false)
    {
        if (Loader != null)
            throw new Exception("The loading process has already been started.");
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        isReload = reloading;
        Loader = new XmlLoader(config);
    }

    /// <summary>
    /// Adds a def file that is read from a stream.
    /// The stream is read until the end.
    /// </summary>
    /// <param name="documentStream">The document text stream. It is read into text using provided <paramref name="encoding"/>.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <param name="encoding">The text encoding to use when reading from the stream. If null, UTF8 is used unless a different encoding is detected.</param>
    /// <param name="closeStream">If true, the stream is closed once reading is done. Defaults to false.</param>
    public static void AddDefDocument(Stream documentStream, string source, Encoding? encoding = null, bool closeStream = false)
    {
        ArgumentNullException.ThrowIfNull(documentStream);

        using var reader = new StreamReader(documentStream, encoding, leaveOpen: !closeStream);
        AddDefDocument(reader, source);
    }

    /// <summary>
    /// Adds a def file that is read from the provided <see cref="StreamReader"/>.
    /// </summary>
    /// <param name="streamReader">The reader to read XML text from. The stream is read until the end.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    public static void AddDefDocument(StreamReader streamReader, string source)
    {
        ArgumentNullException.ThrowIfNull(streamReader);
        string xml = streamReader.ReadToEnd();
        AddDefDocument(xml, source);
    }
    
    /// <summary>
    /// Adds a new def document based on the XML text passed in.
    /// </summary>
    /// <param name="xmlDocumentContents">The contents of the XML file.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    public static void AddDefDocument(string xmlDocumentContents, string source)
    {
        ArgumentException.ThrowIfNullOrEmpty(xmlDocumentContents);

        var doc = new XmlDocument
        {
            PreserveWhitespace = true
        };
        doc.LoadXml(xmlDocumentContents);
        
        AddDefDocument(doc, source);
    }
    
    /// <summary>
    /// Adds a new def document.
    /// </summary>
    /// <param name="document">The XML document to register.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    public static void AddDefDocument(XmlDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (Loader == null)
            throw new Exception("The loading process has not been started, call StartLoading() first.");

        Loader.AppendDocument(document, source);
    }

    /// <summary>
    /// Finishes the current loading process by parsing and registering all defs that have been
    /// added by calling the <see cref="AddDefDocument(XmlDocument, string)"/>
    /// family of methods.
    /// This call will parse and register all defs, and also do post-load callbacks on all parsed objects.
    /// </summary>
    /// <exception cref="Exception">If loading has not been started.</exception>
    public static void FinishLoading()
    {
        if (Loader == null)
            throw new Exception("The loading process has not been started, call StartLoading() first.");
        
        // Resolve inheritance.
        Debug.Assert(!Loader.HasResolvedInheritance);
        Loader.ResolveInheritance();
        
        Func<string, IDef?>? existing = null;
        if (isReload)
            existing = str => idToDef.GetValueOrDefault(str);

        // Parse defs.
        foreach (var def in Loader.MakeDefs(existing))
        {
            if (isReload && idToDef.ContainsKey(def.ID))
                continue;

            Register(def);
        }
        
        DoPostLoadCallbacks();

        finalMasterDocument = Loader.GetMasterDocumentXml();
        Loader.Dispose();
        Loader = null;
    }

    /// <summary>
    /// Gets the user-friendly contents on the master XML document
    /// for the latest loading process.
    /// Used for debugging only.
    /// </summary>
    public static string? GetMasterDocumentXML() => finalMasterDocument ?? Loader?.GetMasterDocumentXml() ?? null;

    private static void DoPostLoadCallbacks()
    {
        ArgumentNullException.ThrowIfNull(Loader);

        if (Loader.Config.DoPostLoad)
        {
            // Post-load.
            foreach (var item in Loader.PostLoadItems)
            {
                try
                {
                    item.PostLoad();
                }
                catch (Exception e)
                {
                    DefDebugger.Error($"Exception PostLoading item '{item}'", e);
                }
            }
        }

        if (Loader.Config.DoLatePostLoad)
        {
            // Late Post-load.
            foreach (var item in Loader.PostLoadItems)
            {
                try
                {
                    item.LatePostLoad();
                }
                catch (Exception e)
                {
                    DefDebugger.Error($"Exception PostLoading item '{item}'.", e);
                }
            }
        }

        // Config errors.
        var reporter = new ConfigErrorReporter();
        foreach (var item in Loader.ConfigErrorItems)
        {
            try
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                reporter.CurrentDef = item as IDef;
                item.ConfigErrors(reporter);
            }
            catch (Exception e)
            {
                DefDebugger.Error($"Exception PostLoading item '{item}'.", e);
            }
        }
    }

    /// <summary>
    /// Tries to get an <see cref="IDef"/> based on its <see cref="IDef.ID"/>.
    /// Will return null if a matching def was not found.
    /// There is a generic version of this method which is preferred, see <see cref="Get{T}"/>.
    /// </summary>
    /// <param name="id">The case-sensitive ID of the def to look for.</param>
    /// <returns>The found def, or null.</returns>
    public static IDef? Get(string id) => idToDef.GetValueOrDefault(id);
    
    /// <summary>
    /// Tries to get a def of type <see cref="T"/> based on its <see cref="IDef.ID"/>.
    /// Will return null if a matching def was not found, or the def was not of the expected type.
    /// </summary>
    /// <param name="id">The case-sensitive ID of the def to look for.</param>
    /// <returns>The found def, or null.</returns>
    public static T? Get<T>(string id) where T : class => idToDef.TryGetValue(id, out var found) ? found as T : null;

    /// <summary>
    /// Registers a new def to the database, allowing it to be accessed via the <see cref="Get"/> family of methods.
    /// Normally this method does not need to be manually called, because the <see cref="FinishLoading"/> method does it
    /// automatically for all loaded defs.
    /// Defs cannot have duplicate IDs.
    /// This method returns true on success and false otherwise.
    /// </summary>
    /// <param name="def">The def to register.</param>
    /// <returns>True on success and false otherwise.</returns>
    public static bool Register(IDef def)
    {
        ArgumentNullException.ThrowIfNull(def);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (def.ID == null)
            return false;

        if (!idToDef.TryAdd(def.ID, def))
            return false;

        allDefs.Add(def);

        foreach (var container in GetAllContainersForDefType(def.GetType()))
        {
            container.Add(def);
        }
        return true;
    }

    /// <summary>
    /// Un-registers a def.
    /// This removes it from the database.
    /// Returns true on success and false otherwise.
    /// </summary>
    /// <param name="def">The def to remove.</param>
    /// <returns>True on success or false otherwise.</returns>
    public static bool UnRegister(IDef def)
    {
        ArgumentNullException.ThrowIfNull(def);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (def.ID == null)
            return false;

        if (!idToDef.TryGetValue(def.ID, out var found) || found != def)
            return false;

        idToDef.Remove(def.ID);
        allDefs.Remove(def);
        
        foreach (var container in GetAllContainersForDefType(def.GetType()))
        {
            container.Remove(def);
        }
        
        return true;
    }

    private static IEnumerable<DefContainer> GetAllContainersForDefType(Type? type)
    {
        if (type != null)
        {
            var interfaces = type.GetInterfaces();
            foreach (var i in interfaces)
            {
                yield return GetContainerForType(i);
            }
        }
        
        while (type != null)
        {
            if (type != typeof(object))
            {
                // The class itself.
                yield return GetContainerForType(type);
            }
            type = type.BaseType;
        }
    }

    /// <summary>
    /// Gets a read-only list of all defs currently in the database.
    /// See the generic version <see cref="GetAll{T}"/> which is preferred over this one.
    /// </summary>
    public static IReadOnlyList<IDef> GetAll() => allDefs;

    /// <summary>
    /// Gets a read-only list of all defs in the database that inherit from or implement the type <see name="T"/>.
    /// <see cref="T"/> may be any class or interface.
    /// The returned list will include all defs that are of the target type or a subclass of that type.
    /// If <see cref="T"/> is an interface, this returns all defs that implement that interface.
    /// Calls to this method are fast because the groups are pre-computed.
    /// </summary>
    /// <typeparam name="T">The type of def to look for.</typeparam>
    /// <returns>The list of defs matching the target type, or an empty list if none were found.</returns>
    public static IReadOnlyList<T> GetAll<T>() where T : class
    {
        if (defsOfType.TryGetValue(typeof(T), out var found))
            return ((DefContainer<T>)found).Defs;

        return Array.Empty<T>();
    }

    /// <summary>
    /// Gets or creates a def container for the specified type.
    /// </summary>
    private static DefContainer GetContainerForType(Type type)
    {
        if (defsOfType.TryGetValue(type, out var found))
            return found;

        if (Activator.CreateInstance(typeof(DefContainer<>).MakeGenericType(type)) is not DefContainer created)
            throw new Exception($"Internal error: failed to make def container for type '{type.FullName}'");

        defsOfType.Add(type, created);
        return created;
    }

    #region Container classes
    private abstract class DefContainer
    {
        public abstract void Add(object def);
        public abstract void Remove(object def);
    }

    private sealed class DefContainer<T> : DefContainer where T : class
    {
        public readonly List<T> Defs = new List<T>();

        public override void Add(object def)
        {
#if DEBUG
            if (Defs.Contains((T) def))
                throw new Exception($"Internal error: attempt to add duplicate def to container: {def}");
#endif
            Defs.Add((T) def);
        }

        public override void Remove(object def)
        {
#if DEBUG
            if (!Defs.Contains((T) def))
                throw new Exception($"Internal error: attempt to remove def from container where it doesn't exist: {def}");
#endif
            Defs.Remove((T)def);
        }
    }
    #endregion
}
