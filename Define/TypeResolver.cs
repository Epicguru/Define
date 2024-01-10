using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Define;

/// <summary>
/// A utility class that aids in parsing <see cref="Type"/>
/// beyond what <see cref="Type.Parse"/> is capable of,
/// including support for short class names, subclasses and fully-constructed generic types.
/// </summary>
public static partial class TypeResolver
{
    private static readonly Dictionary<string, Type?> cache = new Dictionary<string, Type?>(1024);
    private static readonly List<Assembly> allAssemblies = new List<Assembly>();
    private static readonly Dictionary<Type, string> typeAliases = new Dictionary<Type, string>
    {
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(int), "int" },
        { typeof(uint), "uint" },
        { typeof(long), "long" },
        { typeof(ulong), "ulong" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(decimal), "decimal" },
        { typeof(object), "object" },
        { typeof(bool), "bool" },
        { typeof(char), "char" },
        { typeof(string), "string" },
        { typeof(void), "void" }
    };
    private static readonly Dictionary<string, Type> typeAliasesInverse = new Dictionary<string, Type>();
    [ThreadStatic] private static StringBuilder? strBuilder;

    static TypeResolver()
    {
        RefreshAssembliesList();

        foreach (var pair in typeAliases)
            typeAliasesInverse.Add(pair.Value, pair.Key);
    }

    /// <summary>
    /// Registers all assemblies that are currently loaded into the <see cref="AppDomain"/>.
    /// </summary>
    /// <param name="getAssemblyPriority">If not null, this provides the priority of assemblies when searching for types.</param>
    public static void RefreshAssembliesList(Func<Assembly, int>? getAssemblyPriority = null)
    {
        allAssemblies.Clear();
        allAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

        if (getAssemblyPriority != null)
            allAssemblies.Sort((a, b) => getAssemblyPriority(b).CompareTo(getAssemblyPriority(a)));
    }

    [GeneratedRegex(@"([\w\+]+)[<\[]([\w\+<>\[\],? ]+)[>\]]")] // See https://regexr.com/7q1ou
    private static partial Regex GetGenericTypeRegex();

    /// <summary>
    /// Attempts to parse a type based on the type's name.
    /// The allowed name formats are as followed:
    /// <list type="bullet">
    /// <item><b>Full assembly qualified:</b> The full assembly-qualified name of the type.</item>
    /// <item><b>Namespace qualified name:</b> The name of the type including its namespace.</item>
    /// <item><b>Short name:</b> Just the name of the type.</item>
    /// </list>
    /// Additionally, the following features are supported:
    /// <list type="bullet">
    /// <item><b>Nested types:</b> Nested types can be specified using the + symbol. For example: <c>ParentClass+NestedClass</c>.</item>
    /// <item><b>Fully-constructed generic types:</b> Generics can be specified using either angular or square brackets. For example: <c>List[string]</c></item>
    /// <item><b>Nullable types:</b> Nullable types can be specified using the ? symbol. For example: <c>int?</c>.</item>
    /// </list>
    /// The result of this call is cached for future use.
    /// </summary>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="throwOnError">If true, an exception is thrown when the type is not found. If false, an error or warning is logged (see <see cref="DefDebugger"/>) and null is returned.</param>
    /// <returns>The found type, or null.</returns>
    public static Type? Get(string typeName, bool throwOnError = false)
    {
        if (cache.TryGetValue(typeName, out var found))
        {
            if (throwOnError && found == null)
                throw new Exception($"Type not found: '{typeName}' was not found in any loaded assembly.");
            return found;
        }

        bool isNullable = typeName[^1] == '?';
        if (isNullable)
            typeName = typeName[..^1];

        // Attempt to resolve short type aliases i.e. 'float' => 'System.Single'
        if (typeAliasesInverse.TryGetValue(typeName, out var alias))
        {
            return isNullable ? MakeNullable(alias) : alias;
        }

        found = TryFindType(typeName);
        cache.Add(typeName, found);

        // Make nullable if required:
        found = isNullable ? MakeNullable(found) : found;
        
        if (throwOnError && found == null)
            throw new Exception($"Type not found: '{typeName}' was not found in any loaded assembly.");

        return found;
    }

    private static bool IsGenericTypeName(string input, out string name, out string[]? genericArgs)
    {
        if (GetGenericTypeRegex().Match(input) is { Success: true } m)
        {
            genericArgs = TopLevelSplit(m.Groups[2].Value).ToArray();
            name = $"{m.Groups[1].Value}`{genericArgs.Length}"; // This makes the name something like 'List`1' which is what the compiler generates for generic types.
            return true;
        }

        name = input;
        genericArgs = null;
        return false;
    }

    private static IEnumerable<string> TopLevelSplit(string str)
    {
        strBuilder ??= new StringBuilder(1024);
        strBuilder.Clear();

        int depth = 0;
        foreach (char c in str)
        {
            switch (c)
            {
                case '[':
                case '<':
                    strBuilder.Append('<');
                    depth++;
                    break;

                case ']':
                case '>':
                    strBuilder.Append('>');
                    depth--;
                    break;

                case ',' when depth == 0:
                    yield return strBuilder.ToString().Trim();
                    strBuilder.Clear();
                    break;

                default:
                    strBuilder.Append(c);
                    break;
            }
        }

        string final = strBuilder.ToString().Trim();
        if (final.Length > 0)
            yield return final;
    }

    private static Type? TryFindType(string name, StringComparison comp = StringComparison.Ordinal)
    {
        Type? found = null;
        string originalName = name;

        bool isGeneric = IsGenericTypeName(name, out string baseGenericName, out string[]? genericArgs);
        if (isGeneric)
        {
            name = baseGenericName;
        }

        // This expects a fully assembly-qualified name:
        Type? simple = Type.GetType(name, false);
        if (simple != null)
            found = simple;

        bool expectNested = name.Contains('+');

        // Find via full type name.
        if (found == null)
        {
            foreach (var ass in allAssemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    if (type.FullName!.Equals(name, comp))
                    {
                        found = type;
                        break;
                    }
                }
                if (found != null)
                    break;
            }
        }

        // Find by short name (just class name).
        if (found == null)
        {
            foreach (var ass in allAssemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    if (expectNested)
                    {
                        if (!type.IsNested)
                            continue;

                        if (MakeNestedName(type).Equals(name, comp))
                        {
                            found = type;
                            break;
                        }
                    }
                    
                    if (type.Name.Equals(name, comp))
                    {
                        found = type;
                        break;
                    }
                }
                if (found != null)
                    break;
            }
        }

        if (found == null)
        {
            DefDebugger.Warn($"Failed to find type '{name}'.");
            return null;
        }

        if (!isGeneric)
            return found;

        // Construct generic type!
        Type[] args = new Type[genericArgs!.Length];

        for (int i = 0; i < args.Length; i++)
        {
            var parsed = Get(genericArgs[i]);
            if (parsed == null)
            {
                DefDebugger.Error($"Failed to parse generic argument {i}, '{genericArgs[i]}', so the generic type '{originalName}' can not be constructed");
                return null;
            }
            args[i] = parsed;
        }

        try
        {
            return found.MakeGenericType(args);
        }
        catch (Exception e)
        {
            DefDebugger.Error($"Failed to construct generic type '{found}' with type args: {string.Join(", ", args.Select(a => a.Name))}. This is probably due to an invalid generic constraint. See exception below.", e);
            return null;
        }
    }

    private static string MakeNestedName(Type type)
    {
        if (!type.IsNested)
            return type.Name;

        return $"{MakeNestedName(type.DeclaringType!)}+{type.Name}";
    }

    private static Type? MakeNullable(Type? type)
    {
        if (type == null)
            return null;

        if (!type.IsValueType)
        {
            DefDebugger.Error($"The type '{type}' cannot be nullable, it is passed by ref.");
            return null;
        }

        // Construct nullable wrapper...
        return typeof(Nullable<>).MakeGenericType(type);
    }

    /// <summary>
    /// Clears the cached types.
    /// Calls to <see cref="Get"/> are cached for speed reasons.
    /// </summary>
    public static void ClearCache()
    {
        cache.Clear();
    }
}