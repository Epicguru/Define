using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Define.Xml.Members;
using Define.Xml.Parsers;

namespace Define.Xml;

/// <summary>
/// The class responsible for turning XML files into C# defs.
/// It is used by the <see cref="DefDatabase"/> although it can be used standalone if desired.
/// </summary>
public class XmlLoader : IDisposable
{
    /*
     * Attributes:
     *
     * >> C# <<
     * [XmlIgnore] -- This member is always ignored when loading/saving XML, regardless of config.
     * [XmlInclude] -- This member is always included when loading/saving XML, regardless of config.
     * [Include] -- This member is always included when loading/saving FastCache, regardless of config.
     * [Exclude] -- This member is always excluded when loading/saving FastCache, regardless of config.
     * [Alias("MyName")] -- This member can be assigned from an XML node node called "MyName" as well as the normal member name.
     *
     * >> XML <<
     * Abstract="true/false" -- Specifies that this def node is abstract or not. Defaults to false if not specified. Only valid on def root node.
     * Parent="ParentName" -- Specifies that the parent of this def is the def named "ParentName".
     * Type="TypeName" -- Specifies the C# type of this node.
     * ElementType="TypeName" -- Specifies the C# type of element types. Only valid for Lists and Dictionaries. The specified type should be assignable to the base type of this list's elements.
     * KeyType="TypeName" -- Specifies the C# type of key types in a dictionary. Only valid for Dictionaries. The specified type should be assignable to the base type of this dictionary's keys.
     * Inherit="true/false" -- If false, the base value from a parent node is ignored, and is instead entirely replaced with the new child value. Not valid on def root node.
     * Null="true/false" -- If true, the node is treated as a null value and the value is forcibly written to the owning object.
     * IsList="true/false" -- If true, the node is treated as a list.
     */

    private static readonly HashSet<string> doNotInheritAttributeNames =
    [
        "Abstract",
        "Null"
    ];

    /// <summary>
    /// If true, <see cref="ResolveInheritance"/> has been called and the defs
    /// have had their inheritance hierarchy resolved and merged.
    /// </summary>
    public bool HasResolvedInheritance { get; private set; }
    /// <summary>
    /// Gets a read-only list of all <see cref="XmlParser"/>s that are currently registered to this loader.
    /// </summary>
    public IReadOnlyList<XmlParser> AllParsers => allParsers;
    /// <summary>
    /// A collection of types that had static data loaded into them.
    /// </summary>
    public HashSet<Type> TypesWithStaticData { get; } = new HashSet<Type>();

    internal bool TypeToParserIsDirty { get; set; }
    
    /// <summary>
    /// The config that is being used to parse defs.
    /// Passed in through the constructor.
    /// </summary>
    public readonly DefSerializeConfig Config;
    /// <summary>
    /// A list of items that need to have their post-load methods called.
    /// </summary>
    public readonly List<IPostLoad> PostLoadItems = new List<IPostLoad>(256);
    /// <summary>
    /// A list of items that need to have their <see cref="IConfigErrors.ConfigErrors"/>
    /// methods called.
    /// </summary>
    public readonly List<IConfigErrors> ConfigErrorItems = new List<IConfigErrors>(256);
    
    private readonly List<XmlParser> allParsers = new List<XmlParser>();
    private readonly Dictionary<Type, MemberStore> fieldMaps = new Dictionary<Type, MemberStore>();
    private readonly Dictionary<Type, XmlParser?> typeToParser = new Dictionary<Type, XmlParser?>();
    private readonly XmlDocument masterDoc = new XmlDocument();
    private readonly HashSet<XmlNode> tempInheritance = new HashSet<XmlNode>();
    private readonly List<XmlNode> tempInheritanceList = new List<XmlNode>();
    private readonly Dictionary<string, IDef> prePopulatedDefs = new Dictionary<string, IDef>();
    private Func<string, IDef?>? existingDefsFunc;

    /// <summary>
    /// Creates a new <see cref="XmlLoader"/>
    /// using the provided config, and registers the default <see cref="XmlParser"/>.
    /// </summary>
    /// <param name="config"></param>
    public XmlLoader(DefSerializeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Config = config;

        // Create master root.
        masterDoc.AppendChild(masterDoc.CreateElement("Defs"));

        AddParser(new DelegateParser());
        AddParser(new DefRefParser());
        AddParser(new EnumParser());
        AddParser(new XmlNodeParser());
        AddParser(new TypeParser());
        AddParser(new ParseableParser());
    }

    /// <summary>
    /// Gets the <see cref="MemberStore"/> for a particular type.
    /// </summary>
    public MemberStore GetMembers(Type type)
    {
        if (fieldMaps.TryGetValue(type, out var found))
            return found;

        var store = new MemberStore(Config, type);
        fieldMaps.Add(type, store);
        return store;
    }

    /// <summary>
    /// Tries to get an <see cref="XmlParser"/> that is capable of parsing
    /// the specified type.
    /// Will return null if an appropriate parser is not found.
    /// </summary>
    public XmlParser? TryGetParser(Type type)
    {
        // Clear cache if necessary.
        if (TypeToParserIsDirty)
        {
            TypeToParserIsDirty = false;
            typeToParser.Clear();
        }
        
        if (typeToParser.TryGetValue(type, out var found))
            return found;

        foreach (var parser in allParsers)
        {
            if (!parser.CanHandle(type))
                continue;

            typeToParser.Add(type, parser);
            return parser;
        }

        typeToParser.Add(type, null);
        return null;
    }

    /// <summary>
    /// Adds a new parser to this loader.
    /// Parsers can be retrieved using <see cref="TryGetParser"/>.
    /// Duplicate parsers will be ignored.
    /// </summary>
    public void AddParser(XmlParser parser)
    {
        ArgumentNullException.ThrowIfNull(parser);

        if (allParsers.Contains(parser))
            return;

        allParsers.Add(parser);
        allParsers.Sort();
        // Reset cache because type->null is cached for speed reasons.
        TypeToParserIsDirty = true;
    }

    /// <summary>
    /// Removes 
    /// </summary>
    /// <param name="parser"></param>
    /// <returns></returns>
    public bool RemoveParser(XmlParser parser)
    {
        ArgumentNullException.ThrowIfNull(parser);

        if (allParsers.Contains(parser))
            return false;

        allParsers.Remove(parser);
        allParsers.Sort();
        // Reset cache because type->null is cached for speed reasons.
        TypeToParserIsDirty = true;
        return true;
    }

    private static XmlNode? GetRootNode(XmlDocument doc)
    {
        foreach (XmlNode child in doc)
        {
            if (child.NodeType == XmlNodeType.Element)
                return child;
        }
        return null;
    }

    /// <summary>
    /// Adds a new XML document to this loader, ready to be parsed.
    /// </summary>
    /// <param name="document">The document to add. Should not be null.</param>
    /// <param name="source">The source of this document. Used for debugging only.</param>
    public void AppendDocument(XmlDocument document, string? source)
    {
        ArgumentNullException.ThrowIfNull(document);

        source ??= "unknown-source";
        
        var root = GetRootNode(document);
        if (root == null)
        {
            DefDebugger.Warn($"There are no def nodes in the document '{source}':\n{document.InnerXml}");
            return;
        }

        var masterRoot = GetRootNode(masterDoc);
        if (masterRoot == null)
            throw new Exception("Internal error: root node is null on master document!");

        foreach (XmlNode sub in root)
        {
            if (sub.NodeType != XmlNodeType.Element)
                continue;

            var imported = masterDoc.ImportNode(sub, true);
            masterRoot.AppendChild(imported);

            // Add source attribute for debugging purposes.
            if (source != null)
                imported.SetAttribute("Source", source);
        }
    }

    /// <summary>
    /// Parses all defs that have been added by <see cref="AppendDocument"/>.
    /// See <see cref="MakeDefs(Func{string, IDef})"/> for an overload that allows
    /// parsing into existing defs.
    /// </summary>
    public IEnumerable<IDef> MakeDefs() => MakeDefs(null);

    /// <summary>
    /// Parses all defs that have been added by <see cref="AppendDocument"/>.
    /// </summary>
    public IEnumerable<IDef> MakeDefs(Func<string, IDef?>? existingDefs)
    {
        var root = GetRootNode(masterDoc);
        if (root == null)
            yield break;

        prePopulatedDefs.Clear();
        var ids = new HashSet<string>();

        existingDefsFunc = existingDefs;
        if (existingDefs == null)
        {
            foreach (XmlNode sub in root)
            {
                if (sub.NodeType != XmlNodeType.Element)
                    continue;

                if (sub.GetAttributeAsBool("Abstract"))
                    continue;

                var type = GetDefType(sub.Name, sub);
                if (type == null)
                    continue;

                if (TryCreateInstance(type, default) is IDef created)
                    prePopulatedDefs.Add(sub.Name, created);
            }

            existingDefsFunc = str => prePopulatedDefs.GetValueOrDefault(str);
        }

        foreach (XmlNode sub in root)
        {
            if (sub.NodeType != XmlNodeType.Element)
                continue;

            if (sub.GetAttributeAsBool("Abstract"))
                continue;

            var type = GetDefType(sub.Name, sub, true);
            if (type == null)
                continue;

            if (!ids.Add(sub.Name))
            {
                DefDebugger.Error($"Duplicate def ID: '{sub.Name}'");
                continue;
            }

            var defInstance = existingDefsFunc!(sub.Name);
            var parsed = NodeToDef(sub, defInstance);
            if (parsed != null)
                yield return parsed;
        }

        prePopulatedDefs.Clear();
        existingDefsFunc = null;
    }

    /// <summary>
    /// Attempts to get a def based on it's ID. Unlike <see cref="DefDatabase.Get(string)"/>,
    /// this method will return defs that are partially constructed which makes it appropriate for use inside of parsers.
    /// </summary>
    public IDef? TryGetDef(string defID) => existingDefsFunc?.Invoke(defID);

    private static Type? GetDefType(string id, XmlNode defNode, bool silent = false)
    {
        // Get and validate type.
        string? typeName = defNode.GetAttributeValue("Type");
        if (typeName == null)
        {
            if (!silent)
                DefDebugger.Error($"Def '{id}' does not specify a Type using the Type=\"TypeName\" attribute.");
            return null;
        }

        var type = TypeResolver.Get(typeName);
        if (type == null)
        {
            if (!silent)
                DefDebugger.Error($"Def '{id}' is of type '{typeName}', but that type could not be found in any loaded assembly.");
            return null;
        }

        typeName = type.FullName;
        if (type.IsAbstract)
        {
            if (!silent)
                DefDebugger.Error($"Def '{id}' is of type '{typeName}', but that is an abstract type. A concrete subclass must be specified.");
            return null;
        }

        if (!typeof(IDef).IsAssignableFrom(type))
        {
            if (!silent)
                DefDebugger.Error($"Def '{id}' of type '{typeName}' does not implement the IDef interface, so cannot be loaded as a def.");
            return null;
        }

        return type;
    }

    private IDef? NodeToDef(XmlNode node, IDef? existing)
    {
        // ID is just the node name.
        string id = node.Name;

        Type? type = existing?.GetType() ?? GetDefType(id, node);
        if (type == null)
        {
            DefDebugger.Error($"Failed to find type for def '{node.Name}': specify a type using the Type=\"TypeName\" attribute!");
            return null;
        }

        // Create def instance.
        var instance = existing ?? TryCreateInstance(type, default) as IDef;
        if (instance == null)
        {
            DefDebugger.Error($"Def '{id}' of type '{node.GetAttributeValue("Type")}' could not be instantiated.");
            return null;
        }
        instance.ID = id;

        // Make context.
        var ctx = new XmlParseContext
        {
            Loader = this,
            CurrentValue = instance,
            DefaultType = type,
            TargetType = type,
            Node = node,
            TextValue = "[[DefNode]]",
            Owner = instance
        };

        var created = NodeToClass(ctx).Value as IDef; // Use NodeToClass instead of NodeToObject to bypass the ref resolver.
        Debug.Assert(created == instance);
        Debug.Assert(created.ID == node.Name);

        OnPostParse(created, ctx);

        created.ID = node.Name;
        return created;
    }

    private static Type GetNodeType(XmlNode node, Type defaultType)
    {
        // Check for custom Type attribute.
        string? specific = node.GetAttributeValue("Type");
        if (specific != null)
        {
            var found = TypeResolver.Get(specific)?.StripNullable();
            if (found != null)
                return found;

            DefDebugger.Error($"Failed to find specified type '{specific}'. Falling back to default type '{defaultType}'.");
            return defaultType.StripNullable();
        }

        return defaultType.StripNullable();
    }

    private static NodeType GetParseType(in XmlParseContext context)
    {
        var type = context.TargetType;

        if (typeof(IList).IsAssignableFrom(type))
            return NodeType.List;

        if (typeof(IDictionary).IsAssignableFrom(type))
            return NodeType.Dictionary;

        return NodeType.Default;
    }

    /// <summary>
    /// Attempts to convert an XML node to a C# type based on the provided
    /// context. This is the most generic method that will attempt to find the appropriate type (list, dictionary, class etc.)
    /// to parse.
    /// </summary>
    public ParseResult NodeToObject(in XmlParseContext context)
    {
        // If the Null attribute is "true", then just return null and make sure it overwrites the existing value.
        if (context.Node!.GetAttributeAsBool("Null"))
        {
            // Ignore contents, force write that null back to owner.
            return new ParseResult(null, true);
        }

        var handleMethod = GetParseType(context);
        ParseResult parsed;

        switch (handleMethod)
        {
            case NodeType.Default:

                // Check for simple/raw parser.
                var parser = TryGetParser(context.TargetType);
                if (parser != null)
                {
                    try
                    {
                        parsed = new ParseResult(parser.Parse(context));
                        break;
                    }
                    catch (Exception e)
                    {
                        DefDebugger.Error($"Exception when parsing <{context.Node!.Name}> using parser '{parser.GetType().Name}'.", e, context);
                        return default;
                    }
                }

                // Do default node -> class parsing, by writing to each field.
                parsed = NodeToClass(context);
                break;

            case NodeType.List:
                parsed = NodeToList(context);
                break;

            case NodeType.Dictionary:
                parsed = NodeToDictionary(context);
                break;

            default:
                throw new NotImplementedException(handleMethod.ToString());
        }

        if (parsed.Value != null)
            OnPostParse(parsed.Value, context);
        return parsed;
    }

    private void OnPostParse(object parsed, in XmlParseContext context)
    {
        // All constructed types are passed through here, so it's a good place to do callbacks...
        // PostXmlConstruct on the created object itself.
        if (parsed is IPostXmlConstruct post)
        {
            try
            {
                post.PostXmlConstruct(context);
            }
            catch (Exception e)
            {
                DefDebugger.Error($"An exception was thrown in the {nameof(IPostXmlConstruct.PostXmlConstruct)} method in the {post.GetType().FullName} class:", e);
            }
        }

        if (Config.DoPostLoad || Config.DoLatePostLoad)
        {
            if (parsed is IPostLoad postLoad)
                PostLoadItems.Add(postLoad);
        }

        if (Config.DoConfigErrors)
        {
            if (parsed is IConfigErrors configErrorItem)
                ConfigErrorItems.Add(configErrorItem);
        }

        if (context.Member is { IsValid: true, IsStatic: true, DeclaringType: not null})
        {
            // Loaded static data into this class here!
            TypesWithStaticData.Add(context.Member.DeclaringType);
        }
    }

    private ParseResult NodeToList(scoped in XmlParseContext context)
    {
        var listType = context.TargetType;

        Type elementType = listType.IsConstructedGenericType ?
            listType.GenericTypeArguments[0] : // Generic List<> and the such.
            listType.IsArray ? 
                listType.GetElementType()!
                : typeof(object);

        bool isExplicitList = context.Node!.GetAttributeAsBool("IsList");
        
        string? elemOverrideName = context.Node!.GetAttributeValue("ElementType");
        if (elemOverrideName != null)
        {
            var type = TypeResolver.Get(elemOverrideName);
            if (type == null)
            {
                DefDebugger.Error($"Failed to find type named '{elemOverrideName}' to use as list element override.", ctx: context);
            }
            else if (!elementType.IsAssignableFrom(type))
            {
                DefDebugger.Error($"List element type '{type.FullName}' is not assignable to base list element type '{elementType}'.", ctx: context);
            }
            else
            {
                elementType = type;
            }
        }

        int expectedNewItems = context.Node!.ChildNodes.Cast<XmlNode>().Count(n => n.NodeType == XmlNodeType.Element);
        IList? existingList = context.CurrentValue as IList;
        
        /*
         * If the existing list is not null, and it is of fixed size,
         * then a new list (probably an array) needs to be made
         * with the new capacity.
         * The old items the need to be copied over.
         */

        IList? list;
        int arrayIndex;
        bool isFixedLength;
        
        if (existingList == null || (existingList.IsFixedSize && expectedNewItems > 0))
        {
            // Make new list if it is fixed-size.
            isFixedLength = existingList is { IsFixedSize: true } ||
                                 context.TargetType.IsArray;
            if (isFixedLength)
            {
                list = TryCreateInstance(listType, context, [expectedNewItems + existingList?.Count ?? 0]) as IList;

                if (list != null)
                {
                    // Copy over old items.
                    for (int i = 0; i < existingList!.Count; i++)
                    {
                        list[i] = existingList[i];
                    }
                }

                arrayIndex = existingList?.Count ?? 0;
            }
            else
            {
                // If not fixed size, just use the existing list or create 
                // one if it is null. It will be expanded to fit new items later.
                list = existingList ?? TryCreateInstance(listType, context, [expectedNewItems]) as IList;
                arrayIndex = list?.Count ?? 0;
                
                if (list is { IsReadOnly: true })
                {
                    DefDebugger.Error($"Cannot write to read-only list-like type '{list.GetType()}' for node {((XmlElement)context.Node).GetFullXPath()}.");
                    return default;
                }
            }
        }
        else
        {
            list = existingList;
            arrayIndex = list.Count;
            isFixedLength = list.IsFixedSize;
        }
        
        if (list == null) // No need to log error, TryCreateInstance already logs.
            return default;

        foreach (XmlNode node in context.Node!)
        {
            if (node.NodeType != XmlNodeType.Element)
                continue;

            var type = GetNodeType(node, elementType);
            if (!elementType.IsAssignableFrom(type))
            {
                DefDebugger.Error($"List element type '{type.FullName}' is not assignable to base list element type '{elementType}'.", ctx: context);
                continue;
            }
            
            // Give a warning if list name is not as expected.
            string name = node.Name;
            if (name != Config.ListItemName && !isExplicitList)
            {
                DefDebugger.Warn($"List item nodes are expected to be called '{Config.ListItemName}' but found one called '{name}'. " +
                                 $"This may result in errors when child defs attempt to append to this list. You can override the default list item name using the config {nameof(Config.ListItemName)} property," +
                                 $"or you can explicitly declare that {context.Node.Name} is a list by setting the IsList attribute to \"true\".");
            }
            
            var ctx = new XmlParseContext
            {
                Loader = this,
                CurrentValue = null,
                DefaultType = elementType,
                TargetType = type,
                ListIndex = list.Count,
                Node = node,
                TextValue = node.InnerText,
                Owner = list
            };

            // Recursive parse call.
            var parsed = NodeToObject(ctx);

            // Add to list!
            if (parsed.ShouldWrite)
            {
                if (isFixedLength)
                {
                    list[arrayIndex] = parsed.Value;
                }
                else
                {
                    list.Add(parsed.Value);
                }
            }

            arrayIndex++;
        }

        return new ParseResult(list);
    }

    private ParseResult NodeToDictionary(scoped in XmlParseContext context)
    {
        var dictType = context.TargetType;

        Type TryGetType(int genericIndex, string attrName, in XmlParseContext context)
        {
            Type elementType = dictType.IsConstructedGenericType && dictType.GenericTypeArguments.Length > genericIndex ? dictType.GenericTypeArguments[genericIndex] : typeof(object);

            string? elemOverrideName = context.Node!.GetAttributeValue(attrName);
            if (elemOverrideName != null)
            {
                var type = TypeResolver.Get(elemOverrideName);
                if (type == null)
                {
                    DefDebugger.Error($"Failed to find type named '{elemOverrideName}' to use as dictionary key/value override.", ctx: context);
                }
                else if (!elementType.IsAssignableFrom(type))
                {
                    DefDebugger.Error($"Dictionary key/value type '{type.FullName}' is not assignable to base dictionary key/value type '{elementType}'.", ctx: context);
                }
                else
                {
                    elementType = type;
                }
            }

            return elementType;
        }

        // Resolve key and value types.
        Type keyType = TryGetType(0, "KeyType", context);
        Type valueType = TryGetType(1, "ElementType", context);

        // Get or create dictionary instance.
        var dict = (context.CurrentValue ?? TryCreateInstance(dictType, context)) as IDictionary;
        if (dict == null) // No need to log error, TryCreateInstance already logs.
            return default;

        // Get key parser and validate it.
        var keyParser = TryGetParser(keyType);
        if (keyParser == null)
        {
            DefDebugger.Error($"There is no simple parser for dictionary key type '{keyType}'. A parser for that type should be added using AddParser.", ctx: context);
            return new ParseResult(dict);
        }
        if (!keyParser.CanParseNoContext)
        {
            DefDebugger.Error($"The parser '{keyParser.GetType()}' for dictionary key type '{keyType}' does not have the capability to parse with no context, so it that type cannot be used as a dictionary key.", ctx: context);
            return new ParseResult(dict);
        }

        foreach (XmlNode node in context.Node!)
        {
            if (node.NodeType != XmlNodeType.Element)
                continue;

            // Confirm final value type.
            var localValueType = GetNodeType(node, valueType);
            if (!valueType.IsAssignableFrom(localValueType))
            {
                DefDebugger.Error($"Type '{localValueType}' is not assignable to base dictionary value type '{valueType}'.", ctx: context);
                continue;
            }

            // Parse key. Key must not be null.
            var key = keyParser.Parse(new XmlParseContext
            {
                Loader = this,
                TextValue = node.Name,
                DefaultType = keyType,
                TargetType = keyType,
            });
            if (key == null)
            {
                DefDebugger.Error($"Parser '{keyParser.GetType()} returned null when parsing key '{node.Name}' of type '{keyType}' for a dictionary. Dictionary keys cannot be null, so this entry will be discarded.", ctx: context);
                continue;
            }

            // Make value context...
            var ctx = new XmlParseContext
            {
                Loader = this,
                CurrentValue = null,
                DefaultType = valueType,
                TargetType = localValueType,
                DictionaryKey = key,
                Owner = dict,
                Node = node,
                TextValue = node.InnerText
            };

            // Recursive parse call.
            var parsed = NodeToObject(ctx);

            // Add to dictionary!
            if (parsed.ShouldWrite)
                dict.Add(key, parsed.Value);
        }

        return new ParseResult(dict);
    }

    private ParseResult NodeToClass(scoped in XmlParseContext context)
    {
        var type = context.TargetType;
        Debug.Assert(type != null);

        // Final type cannot be abstract.
        if (type.IsAbstract)
        {
            DefDebugger.Error($"Cannot create instance of abstract type/interface '{type}'.", ctx: context);
            return default;
        }

        // Get current value or create new instance of type.
        var instance = context.CurrentValue ?? TryCreateInstance(type, context);
        if (instance == null) // No need to log error, TryCreateInstance already logs.
            return default;

        foreach (XmlNode node in context.Node!.ChildNodes)
        {
            if (ShouldSkipNodeForClassLikeParsing(node))
                continue;

            // Try to find member from name.
            var member = GetMember(instance.GetType(), node.Name);
            if (!member.IsValid)
            {
                DefDebugger.Error($"Failed to find member called '{node.Name}' in class '{instance.GetType().FullName}'!", ctx: context); // This technically isn't the right context...
                continue;
            }

            // Resolve type (uses member type unless overriden using Type="name")
            var childType = GetNodeType(node, member.Type);

            // Make child context.
            var ctx = new XmlParseContext
            {
                Loader = this,
                Node = node,
                TextValue = node.InnerText,
                DefaultType = member.Type,
                TargetType = childType,
                CurrentValue = member.GetValue(instance),
                Member = member,
                Owner = instance
            };

            // Parse recursively.
            var parsed = NodeToObject(ctx);

            // Assign value back unless it is null.
            if (parsed.ShouldWrite)
                member.SetValue(instance, parsed.Value);
        }

        return new ParseResult(instance);
    }

    private static bool ShouldSkipNodeForClassLikeParsing(XmlNode node)
    {
        switch (node.NodeType)
        {
            case XmlNodeType.Element:
                return false;
            
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
            case XmlNodeType.Comment:
                return true;
            
            default:
                var parent = node.ParentNode as XmlElement;
                DefDebugger.Warn($"Unexpected XML node type '{node.NodeType}' found when parsing part of {parent?.GetFullXPath() ?? "?"}.\n" +
                                  "This is often due to bad XML formatting or a missing parser.");
                return true;
        }
    }

    private static object? TryCreateInstance(Type type, in XmlParseContext context, Span<object> args = default)
    {
        try
        {
            object[]? argsArray = args.Length > 0 ? args.ToArray() : null;
            var instance = Activator.CreateInstance(type, argsArray);
            return instance;
        }
        catch (Exception e)
        {
            if (context.IsValid)
                DefDebugger.Error($"Failed to create instance of '{type.FullName}'.", e, context);
            else
                DefDebugger.Error($"Failed to create instance of '{type.FullName}'.", e);
            return null;
        }
    }

    internal MemberWrapper GetMember(Type type, string name) => GetMembers(type).GetMember(name);

    private bool IsListImplied(XmlNode node)
    {
        if (!node.HasChildNodes)
            return false;

        foreach (XmlNode child in node)
        {
            if (child.NodeType == XmlNodeType.Element && child.Name != Config.ListItemName)
                return false;
        }
        return true;
    }

    private static bool IsValueNode(XmlNode node)
    {
        if (!node.HasChildNodes)
            return false;

        foreach (XmlNode child in node.ChildNodes)
        {
            switch (child.NodeType)
            {
                // Ignore comments.
                case XmlNodeType.Comment:
                    continue;

                // Text is just the value.
                case XmlNodeType.Text:
                    continue;

                // Anything else means that it is not a simple name-value pair.
                default:
                    return false;
            }
        }

        return true;
    }

    private void Merge(XmlElement destination, XmlElement source)
    {
        bool shouldInherit = source.GetAttributeAsBool("Inherit", true);
        if (!shouldInherit)
        {
            var clone = (XmlElement)source.CloneNode(true);
            clone.Attributes.RemoveNamedItem("Inherit");
            destination.ParentNode?.ReplaceChild(clone, destination);
            return;
        }

        // Remove attributes that should not be inherited.
        foreach (var attr in doNotInheritAttributeNames)
        {
            var found = destination.Attributes[attr];
            if (found != null)
                destination.Attributes.Remove(found);
        }

        // Merge attributes.
        foreach (XmlAttribute attr in source.Attributes)
        {
            destination.SetAttribute(attr.Name, attr.Value);
        }

        // Is the target a simple value type then copy over the value.
        if (IsValueNode(destination))
        {
            destination.InnerText = source.InnerText;
            return;
        }

        // Should we be doing a regular merge or an append? Append is normally used for lists.
        bool shouldAppend = destination.GetAttributeAsBool("IsList", IsListImplied(source));

        // Merges nodes.
        foreach (XmlNode child in source)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;

            var dest = destination[child.Name];

            // Append if necessary.
            if (shouldAppend || dest == null)
            {
                var newChild = child.CloneNode(true);
                destination.AppendChild(newChild);
                continue;
            }

            // Replace (merge) mode.
            Merge(dest, (XmlElement)child);
        }
    }

    private List<XmlNode>? GetInheritance(XmlNode node)
    {
        var original = node;
        var root = GetRootNode(masterDoc);
        if (root == null)
            return null;
        
        tempInheritance.Clear();
        tempInheritanceList.Clear();

        while (true)
        {
            if (!tempInheritance.Add(node))
            {
                DefDebugger.Error($"Cyclic inheritance detected in '{original.Name}' tree: {node.Name}. Def will not be loaded.");
                return null;
            }
            tempInheritanceList.Add(node);
            
            string? parentName = node.GetAttributeValue("Parent");
            if (parentName == null)
            {
                tempInheritanceList.Reverse();
                return tempInheritanceList;
            }

            var found = root[parentName];
            if (found == null)
            {
                DefDebugger.Error($"Failed to find parent called '{parentName}' of '{node.Name}' for def '{original.Name}'. Def will not be loaded.");
                return null;
            }

            node = found;
        }
    }

    /// <summary>
    /// Resolves inheritance hierarchies for all pending defs and updates
    /// the master XML document accordingly.
    /// Do not call twice.
    /// </summary>
    public void ResolveInheritance()
    {
        var root = GetRootNode(masterDoc);

        if (root == null)
        {
            DefDebugger.Error("Called ResolveInheritance when there are no xml documents loaded! Use AppendDocument before calling this.");
            return;
        }

        var toDelete = new HashSet<XmlNode>();
        var created = new List<XmlNode>();

        foreach (XmlNode def in root.ChildNodes)
        {
            // Do not do inheritance for abstract types.
            if (def.GetAttributeAsBool("Abstract"))
            {
                // Also delete abstract nodes from the final master document because they are not needed,
                // and they can create unexpected behaviour in patches.
                toDelete.Add(def);
                continue;
            }

            var inheritance = GetInheritance(def);
            if (inheritance == null)
                continue;

            // No inheritance to do.
            if (inheritance.Count == 1)
                continue;

            // Clone base def, add to root.
            var baseDef = inheritance[0];
            var baseClone = CloneWithName(baseDef, def.Name);
            created.Add(baseClone);

            // Merge all parts of the inheritance tree back into that base clone.
            for (int i = 1; i < inheritance.Count; i++)
            {
                var part = inheritance[i];
                Merge(baseClone, (XmlElement)part);
            }

            // Delete the def node, because the base clone has 'become' it.
            toDelete.Add(def);
        }

        foreach (var item in toDelete)
        {
            root.RemoveChild(item);
        }

        foreach (var toAdd in created)
        {
            root.AppendChild(toAdd);
        }

        HasResolvedInheritance = true;
    }

    private XmlElement CloneWithName(XmlNode toClone, string newName)
    {
        Debug.Assert(toClone is XmlElement);
        
        var created = masterDoc.CreateElement(newName);

        // Copy attributes.
        if (toClone is XmlElement e)
        {
            foreach (XmlAttribute attr in e.Attributes)
            {
                created.SetAttribute(attr.Name, attr.Value);
            }
        }

        // Copy inner nodes.
        foreach (XmlNode inner in toClone)
        {
            created.AppendChild(inner.CloneNode(true));
        }

        return created;
    }

    /// <summary>
    /// Writes the current contents of the XML master document to a string.
    /// </summary>
    public string GetMasterDocumentXml()
    {
        var stringBuilder = new StringBuilder();

        var element = XElement.Parse(masterDoc.InnerXml);

        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            NewLineOnAttributes = false,
        };

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Helper types
    
    /// <summary>
    /// The results of an XML parsing operation.
    /// </summary>
    public readonly ref struct ParseResult
    {
        /// <summary>
        /// If true, the <see cref="Value"/> should be written back to the member that it was loaded for.
        /// </summary>
        public bool ShouldWrite => Value != null || ForceWrite;

        /// <summary>
        /// The parsed value.
        /// </summary>
        public object? Value { get; init; }
        
        /// <summary>
        /// If true, the value should be written back to the target member even if the
        /// <see cref="Value"/> is null.
        /// See <see cref="ShouldWrite"/>.
        /// </summary>
        public bool ForceWrite { get; init; }

        /// <summary>
        /// Creates a new <see cref="ParseResult"/>.
        /// </summary>
        public ParseResult(object? value, bool forceWrite = false)
        {
            Value = value;
            ForceWrite = forceWrite;
        }
    }

    private enum NodeType
    {
        Default,
        List,
        Dictionary
    }
    #endregion
}