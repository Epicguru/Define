using System.Reflection;

namespace Define.Xml.Parsers;

/// <summary>
/// An <see cref="XmlParser"/> that parses all type of delegates, including
/// <see cref="Action"/> and <see cref="Func{T}"/>.
/// This parser is only capable of handling static methods.
/// </summary>
public class DelegateParser : XmlParser
{
    /// <inheritdoc/>
    public override bool CanParseNoContext => true;
    
    /// <inheritdoc/>
    public override bool CanHandle(Type type) => typeof(Delegate).IsAssignableFrom(type);

    /// <inheritdoc/>
    public override object? Parse(in XmlParseContext context)
    {
        string raw = context.TextValue;
        if (string.IsNullOrEmpty(raw))
            return null;

        var delegateFormat = context.TargetType;
        var delegateMethod = delegateFormat.GetMethod("Invoke")!;
        var delegateArgsCount = delegateMethod.GetParameters().Length;
        var genericArgsCount = delegateMethod.GetGenericArguments().Length;

        string[] parts = context.TextValue.Split(':');
        if (parts.Length != 2)
            throw new Exception($"Expected Action in the format 'Namespace.ClassName:MethodName', got '{context.TextValue}'");

        var type = TypeResolver.Get(parts[0]);
        if (type == null)
            throw new Exception($"Failed to find class called '{context.TextValue}'");

        string methodName = parts[1];
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var method in methods)
        {
            /*
             * There is quite a lot of complexity in trying to figure out whether a method can be assigned to a delegate
             * because of covariant return types, generics and generic constraints, in, out, ref etc.
             * It is easiest for me to simply discard method signatures that are obviously wrong, then tell C# to attempt to generate
             * a delegate instance and check to see if that fails.
             */
            
            // Same name:
            if (method.Name != methodName)
                continue;
            
            // Check for the same number of parameters:
            var args = method.GetParameters();
            if (args.Length != delegateArgsCount)
                continue;
            
            // Check for number of generic args:
            var genericArgs = method.GetGenericArguments();
            if (genericArgs.Length != genericArgsCount)
                continue;
            
            // Attempt to generate delegate:
            try
            {
                return method.CreateDelegate(delegateFormat, null);
            }
            catch
            {
                // Ignore. Assumed to be invalid sig.
            }
        }
        
        throw new Exception($"Failed to find compatible static method called '{parts[1]}' in class {type.FullName}!");
    }
}