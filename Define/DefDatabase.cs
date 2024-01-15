using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Define.Xml;

[assembly: InternalsVisibleTo("Define.FastCache")]
namespace Define;

/// <summary>
/// The def database tracks all loaded <see cref="IDef"/>s.
/// It has method to load, register, unregister and get defs.
/// </summary>
public class DefDatabase
{
    /// <summary>
    /// If true the database is currently in the loading process,
    /// which means after <see cref="StartLoading"/> has been called but
    /// before <see cref="FinishLoading"/>.
    /// </summary>
    public bool IsLoading => Loader != null;
    /// <summary>
    /// The total number of defs currently loaded.
    /// </summary>
    public int Count => allDefs.Count;
    /// <summary>
    /// The number of def containers currently loaded.
    /// The number of containers depends on the inheritance hierarchy of all loaded defs,
    /// as well as the number of unique interfaces they implement.
    /// This value should be used for diagnostics only.
    /// </summary>
    public int ContainerCount => defsOfType.Count;
    /// <summary>
    /// The <see cref="XmlLoader"/> that is used during the def loading process.
    /// This loader is only non-null after <see cref="StartLoading"/> is called
    /// and before <see cref="FinishLoading"/> is called.
    /// This loader can be configured to add or remove parsers.
    /// </summary>
    public XmlLoader? Loader { get; private set; }
    /// <summary>
    /// A read-only collection of types that had static data loaded into them.
    /// Used for FastCache.
    /// </summary>
    public IReadOnlyCollection<Type> TypesWithStaticData => typesWithStaticData;
    /// <summary>
    /// The latest <see cref="DefSerializeConfig"/> that has been used to load
    /// defs. Will be null until <see cref="StartLoading"/> is called, or when
    /// a FastCache is loaded into this database.
    /// </summary>
    public DefSerializeConfig? Config { get; internal set; }
    
    private readonly HashSet<Type> typesWithStaticData = new HashSet<Type>();
    private readonly Dictionary<string, IDef> idToDef = new Dictionary<string, IDef>(4096);
    private readonly List<IDef> allDefs = new List<IDef>(4096);
    private readonly Dictionary<Type, DefContainer> defsOfType = new Dictionary<Type, DefContainer>(128);
    private string? finalMasterDocument;
    private bool isReload;
    
    /// <summary>
    /// Clears the def database of all defs.
    /// If the loading process is currently active it is cancelled.
    /// </summary>
    public void Clear()
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
    /// <param name="config">The <see cref="DefSerializeConfig"/> to use when loading.</param>
    /// <param name="reloading">If true, the loaded defs are applied to any existing loaded defs. If false, attempting to load defs with IDs that match existing defs will cause an error.</param>
    /// <exception cref="Exception">If the loading process has already been started and not finished by calling <see cref="FinishLoading"/>.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="config"/> is null.</exception>
    public void StartLoading(DefSerializeConfig config, bool reloading = false)
    {
        if (Loader != null)
            throw new Exception("The loading process has already been started.");
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        isReload = reloading;
        Loader = new XmlLoader(config);
        Config = config;
    }

    /// <summary>
    /// Loads all def (.xml) files from inside the ZIP file at the specified path.
    /// The ZIP file should not be encrypted!
    /// The archive is read asynchronously, <b>but it is not thread-safe!</b>
    /// Only call from one thread at a time!
    /// </summary>
    /// <param name="zipFilePath">The file path of the ZIP file.</param>
    /// <returns>True if adding the defs was successful, false otherwise.</returns>
    public async Task<bool> AddDefsFromZipAsync(string zipFilePath)
    {
        if (!File.Exists(zipFilePath))
        {
            DefDebugger.Error($"Failed to find zip file at '{zipFilePath}'");
            return false;
        }

        await using var fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        
        return await AddDefsFromZipAsync(zip);
    }
    
    /// <summary>
    /// Loads all def (.xml) files from inside the ZIP file at the specified path.
    /// The ZIP file should not be encrypted!
    /// </summary>
    /// <param name="zipFilePath">The file path of the ZIP file.</param>
    /// <returns>True if adding the defs was successful, false otherwise.</returns>
    public bool AddDefsFromZip(string zipFilePath)
    {
        if (!File.Exists(zipFilePath))
        {
            DefDebugger.Error($"Failed to find zip file at '{zipFilePath}'");
            return false;
        }

        using var fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        
        return AddDefsFromZip(zip);
    }
    
    /// <summary>
    /// Loads all def (.xml) files from inside this ZIP archive.
    /// </summary>
    /// <param name="zipArchive">The zip archive to load xml files from. Must be readable.</param>
    /// <returns>True if loading all zip files succeeded, false otherwise.</returns>
    public bool AddDefsFromZip(ZipArchive zipArchive)
    {
        ArgumentNullException.ThrowIfNull(zipArchive);

        bool success = true;
        
        foreach (var entry in zipArchive.Entries)
        {
            if (new FileInfo(entry.FullName).Extension != ".xml")
                continue;

            using var stream = entry.Open();
            bool worked = AddDefDocument(stream, entry.FullName);
            if (!worked)
                success = false;
        }

        return success;
    }
    
    /// <summary>
    /// Loads all def (.xml) files from inside this ZIP archive.
    /// The archive is read asynchronously, <b>but it is not thread-safe!</b>
    /// Only call from one thread at a time!
    /// </summary>
    /// <param name="zipArchive">The zip archive to load xml files from. Must be readable.</param>
    /// <returns>True if loading all zip files succeeded, false otherwise.</returns>
    public async Task<bool> AddDefsFromZipAsync(ZipArchive zipArchive)
    {
        ArgumentNullException.ThrowIfNull(zipArchive);

        bool success = true;
        
        foreach (var entry in zipArchive.Entries)
        {
            if (new FileInfo(entry.FullName).Extension != ".xml")
                continue;

            await using var stream = entry.Open();
            bool worked = await AddDefDocumentAsync(stream, entry.FullName);
            if (!worked)
                success = false;
        }

        return success;
    }

    /// <summary>
    /// Adds a def file that is read from a stream.
    /// The stream is read until the end.
    /// </summary>
    /// <param name="documentStream">The document text stream. It is read into text using provided <paramref name="encoding"/>.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <param name="encoding">The text encoding to use when reading from the stream. If null, UTF8 is used unless a different encoding is detected.</param>
    /// <param name="closeStream">If true, the stream is closed once reading is done. Defaults to false.</param>
    /// <returns>True if the operation succeeded, false otherwise. In the case of an error (returning false), an error will be raised using the <see cref="DefDebugger"/> error event.</returns>
    public bool AddDefDocument(Stream documentStream, string source, Encoding? encoding = null, bool closeStream = false)
    {
        ArgumentNullException.ThrowIfNull(documentStream);
        try
        {
            using var reader = new StreamReader(documentStream, encoding, leaveOpen: !closeStream);
            return AddDefDocument(reader, source);
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Exception creating stream reader when trying to read def document '{source}'. The stream may not be readable.", e);
            return false;
        }
    }

    /// <summary>
    /// Adds a def file that is read from a stream.
    /// The stream is read until the end.
    /// The stream is read asynchronously, <b>but it is not thread-safe!</b>
    /// Only call from one thread at a time!
    /// </summary>
    /// <param name="documentStream">The document text stream. It is read into text using provided <paramref name="encoding"/>.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <param name="encoding">The text encoding to use when reading from the stream. If null, UTF8 is used unless a different encoding is detected.</param>
    /// <param name="closeStream">If true, the stream is closed once reading is done. Defaults to false.</param>
    /// <returns>True if the operation succeeded, false otherwise. In the case of an error (returning false), an error will be raised using the <see cref="DefDebugger"/> error event.</returns>
    public async Task<bool> AddDefDocumentAsync(Stream documentStream, string source, Encoding? encoding = null, bool closeStream = false)
    {
        ArgumentNullException.ThrowIfNull(documentStream);
        try
        {
            using var reader = new StreamReader(documentStream, encoding, leaveOpen: !closeStream);
            return await AddDefDocumentAsync(reader, source);
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Exception creating stream reader when trying to read def document '{source}'. The stream may not be readable.", e);
            return false;
        }
    }

    /// <summary>
    /// Adds a def file that is read from the provided <see cref="StreamReader"/>.
    /// The stream is read asynchronously, <b>but it is not thread-safe!</b>
    /// Only call from one thread at a time!
    /// </summary>
    /// <param name="streamReader">The reader to read XML text from. The stream is read until the end.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <returns>True if the operation succeeded, false otherwise. In the case of an error (returning false), an error will be raised using the <see cref="DefDebugger"/> error event.</returns>
    public async Task<bool> AddDefDocumentAsync(StreamReader streamReader, string source)
    {
        ArgumentNullException.ThrowIfNull(streamReader);
        try
        {
            string xml = await streamReader.ReadToEndAsync();
            return AddDefDocument(xml, source);
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Exception when reading def document from '{source}'", e);
            return false;
        }
    }
    
    /// <summary>
    /// Adds a def file that is read from the provided <see cref="StreamReader"/>.
    /// </summary>
    /// <param name="streamReader">The reader to read XML text from. The stream is read until the end.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <returns>True if the operation succeeded, false otherwise. In the case of an error (returning false), an error will be raised using the <see cref="DefDebugger"/> error event.</returns>
    public bool AddDefDocument(StreamReader streamReader, string source)
    {
        ArgumentNullException.ThrowIfNull(streamReader);

        string xml;
        try
        {
            xml = streamReader.ReadToEnd();
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Exception reading stream when adding def document '{source}'", e);
            return false;
        }
        
        return AddDefDocument(xml, source);
    }
    
    /// <summary>
    /// Adds a new def document based on the XML text passed in.
    /// </summary>
    /// <param name="xmlDocumentContents">The contents of the XML file.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <returns>True if the operation succeeded, false otherwise. In the case of an error (returning false), an error will be raised using the <see cref="DefDebugger"/> error event.</returns>
    public bool AddDefDocument(string xmlDocumentContents, string source)
    {
        ArgumentException.ThrowIfNullOrEmpty(xmlDocumentContents);

        XmlDocument doc;
        try
        {
            doc = new XmlDocument
            {
                PreserveWhitespace = true
            };
            doc.LoadXml(xmlDocumentContents);
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Exception parsing def file '{source}':", e);
            return false;
        }

        try
        {
            AddDefDocument(doc, source);
            return true;
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Exception adding def document '{source}':", e);
            return false;
        }
    }

    /// <summary>
    /// Loads all def files in the specified folder into the database ready to be loaded.
    /// This is equivalent to calling <see cref="AddDefDocument(System.IO.Stream,string,System.Text.Encoding?,bool)"/>
    /// for each XML file in the folder.
    /// The method returns true if every def file was added successfully, and false if any or all failed to be added.
    /// </summary>
    /// <param name="folderPath">The folder to load def files from.</param>
    /// <param name="searchOption">Determines whether sub-folders should also be searched. Defaults to include sub-folders.</param>
    /// <param name="searchPattern">Determines what file extension is searched for. Defaults to search for .xml files.</param>
    /// <param name="fileFilter">An optional predicate to pick which found files to include. If null, all files found in the folder that match the <paramref name="searchPattern"/> are included.</param>
    /// <returns>True if every def file was added successfully, and false if any or all failed to be added</returns>
    public bool AddDefFolder(string folderPath, SearchOption searchOption = SearchOption.AllDirectories, string searchPattern = "*.xml", Predicate<string>? fileFilter = null)
    {
        if (!Directory.Exists(folderPath))
        {
            DefDebugger.Error($"Failed to find directory '{folderPath}' to load defs from.");
            return false;
        }

        bool success = true;
        
        foreach (var file in Directory.EnumerateFiles(folderPath, searchPattern, searchOption))
        {
            if (!(fileFilter?.Invoke(file) ?? true))
                continue;
            
            bool worked;
            try
            {
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                worked = AddDefDocument(fs, file);
            }
            catch (Exception e)
            {
                DefDebugger.Error($"Exception creating file stream when trying to read def document '{file}'.", e);
                worked = false;
            }

            if (!worked)
                success = false;
        }
        
        return success;
    }
    
    /// <summary>
    /// Loads all def files in the specified folder into the database ready to be loaded.
    /// The files are read asynchronously.
    /// This is equivalent to calling <see cref="AddDefDocumentAsync(System.IO.Stream,string,System.Text.Encoding?,bool)"/>
    /// for each XML file in the folder.
    /// The method returns true if every def file was added successfully, and false if any or all failed to be added.
    /// </summary>
    /// <param name="folderPath">The folder to load def files from.</param>
    /// <param name="searchOption">Determines whether sub-folders should also be searched. Defaults to include sub-folders.</param>
    /// <param name="searchPattern">Determines what file extension is searched for. Defaults to search for .xml files.</param>
    /// <returns>true if every def file was added successfully, and false if any or all failed to be added</returns>
    public async Task<bool> AddDefFolderAsync(string folderPath, SearchOption searchOption = SearchOption.AllDirectories, string searchPattern = "*.xml")
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Could find find directory '{folderPath}'");

        bool success = true;
        
        foreach (var file in Directory.EnumerateFiles(folderPath, searchPattern, searchOption))
        {
            bool worked;
            try
            {
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                worked = await AddDefDocumentAsync(fs, file);
            }
            catch (Exception e)
            {
                DefDebugger.Error($"Exception creating file stream when trying to read def document '{file}'.", e);
                worked = false;
            }

            if (!worked)
                success = false;
        }
        
        return success;
    }
    
    /// <summary>
    /// Adds a new def document.
    /// This operation will throw an exception if adding the document fails, such as due to an invalid format or contents.
    /// Note that this call does not actually parse the document, that is done in <see cref="FinishLoading"/>.
    /// </summary>
    /// <param name="document">The XML document to register.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    /// <returns>True if the operation succeeded, false otherwise. In the case of an error (returning false), an error will be raised using the <see cref="DefDebugger"/> error event.</returns>
    public void AddDefDocument(XmlDocument document, string source)
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
    public void FinishLoading()
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
        
        // Post load...
        DoPostLoadCallbacks();

        // Copy the final master document xml string for future diagnostics...
        finalMasterDocument = Loader.GetMasterDocumentXml();
        
        // Copy over all types with static data for FastCache.
        foreach (var item in Loader.TypesWithStaticData)
            typesWithStaticData.Add(item);
        
        // Remove the loader as it is no longer needed and has a lot of garbage.
        Loader.Dispose();
        Loader = null;
    }

    /// <summary>
    /// Gets the user-friendly contents on the master XML document
    /// for the latest loading process.
    /// Used for debugging only.
    /// </summary>
    public string? GetMasterDocumentXML() => finalMasterDocument ?? Loader?.GetMasterDocumentXml() ?? null;

    private void DoPostLoadCallbacks()
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
    public IDef? Get(string id) => idToDef.GetValueOrDefault(id);
    
    /// <summary>
    /// Tries to get a def of type <see cref="T"/> based on its <see cref="IDef.ID"/>.
    /// Will return null if a matching def was not found, or the def was not of the expected type.
    /// </summary>
    /// <param name="id">The case-sensitive ID of the def to look for.</param>
    /// <returns>The found def, or null.</returns>
    public T? Get<T>(string id) where T : class => idToDef.TryGetValue(id, out var found) ? found as T : null;

    /// <summary>
    /// Registers a new def to the database, allowing it to be accessed via the <see cref="Get"/> family of methods.
    /// Normally this method does not need to be manually called, because the <see cref="FinishLoading"/> method does it
    /// automatically for all loaded defs.
    /// Defs cannot have duplicate IDs.
    /// This method returns true on success and false otherwise.
    /// </summary>
    /// <param name="def">The def to register.</param>
    /// <returns>True on success and false otherwise.</returns>
    public bool Register(IDef def)
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
    public bool UnRegister(IDef def)
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

    private IEnumerable<DefContainer> GetAllContainersForDefType(Type? type)
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
    public IReadOnlyList<IDef> GetAll() => allDefs;

    /// <summary>
    /// Gets a read-only list of all defs in the database that inherit from or implement the type <see name="T"/>.
    /// <see cref="T"/> may be any class or interface.
    /// The returned list will include all defs that are of the target type or a subclass of that type.
    /// If <see cref="T"/> is an interface, this returns all defs that implement that interface.
    /// Calls to this method are fast because the groups are pre-computed.
    /// </summary>
    /// <typeparam name="T">The type of def to look for.</typeparam>
    /// <returns>The list of defs matching the target type, or an empty list if none were found.</returns>
    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        if (defsOfType.TryGetValue(typeof(T), out var found))
            return ((DefContainer<T>)found).Defs;

        return Array.Empty<T>();
    }

    /// <summary>
    /// Gets or creates a def container for the specified type.
    /// </summary>
    private DefContainer GetContainerForType(Type type)
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
