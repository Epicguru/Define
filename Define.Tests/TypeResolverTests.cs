using System.Numerics;
using JetBrains.Annotations;
using TUnit.Core.Exceptions;

namespace Define.Tests;

public sealed class TypeResolverTests : DefTestBase
{
    [Test]
    [MethodDataSource(nameof(Generate_ResolveGenericTypes_Args))]
    public void TestResolveGenericTypes(string name, Type type)
    {
        var resolver = new TypeResolver();
        var resolved = resolver.Get(name);
        resolved.Should().Be(type);
    }

    public static IEnumerable<Func<(string name, Type type)>> Generate_ResolveGenericTypes_Args()
    {
        var baseData = Generate_ResolveGenericTypes_ArgsBase();
        foreach (var func in baseData)
        {
            // Re-emit the original pair.
            yield return func;
            
            // Emit a copy with the '<' and '>' replaced with '[' and ']' respectively.
            var pair = func();
            string newName = pair.name.Replace('<', '[').Replace('>', ']');
            yield return () => (newName, pair.type);
        }
    }

    private static IEnumerable<Func<(string name, Type type)>> Generate_ResolveGenericTypes_ArgsBase()
    {
        yield return () => ("List<string>", typeof(List<string>)); // Short aliased name.
        yield return () => ("List<String>", typeof(List<string>)); // Actual type name.
        yield return () => ("List<int>", typeof(List<int>));
                
        // ReSharper disable once ConvertNullableToShortForm
        #pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        yield return () => ("List<Dictionary<Nullable<int>, HashSet<double?>>>", typeof(List<Dictionary<Nullable<int>, HashSet<double?>>>));
        yield return () => ("Dictionary<int?, SubNestedClass<Vector2>>", typeof(Dictionary<int?, SubNestedClass<Vector2>>));
        #pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    }

    [Test]
    [Arguments("Int32", typeof(int))]
    [Arguments("int?", typeof(int?))]
    [Arguments("float", typeof(float))]
    [Arguments("Vector2", typeof(Vector2))]
    [Arguments("Vector<decimal>?", typeof(Vector<decimal>?))]
    [Arguments("NestedClass", typeof(NestedClass))]
    [Arguments("TypeResolverTests+NestedClass", typeof(NestedClass))]
    [Arguments("Define.Tests.TypeResolverTests+NestedClass", typeof(NestedClass))]
    [Arguments("SubNestedClass<float>", typeof(SubNestedClass<float>))] // Without explicitly specifying it as a nested type, it should find the outer one.
    [Arguments("TypeResolverTests+NestedClass+SubNestedClass<float>", typeof(NestedClass.SubNestedClass<float>))]
    [Arguments("Define.Tests.TypeResolverTests+NestedClass+SubNestedClass<float>", typeof(NestedClass.SubNestedClass<float>))]
    public void TestResolveSimpleTypes(string name, Type type)
    {
        Console.WriteLine(typeof(NestedClass).FullName);
        
        var resolver = new TypeResolver();
        var resolved = resolver.Get(name);
        resolved.Should().Be(type);
    }

    [Test]
    public void TestNullableReferenceType()
    {
        // Attempting to resolve any reference type as a nullable
        // should result in an error.
        var resolver = new TypeResolver();
        var resolved = resolver.Get("string?");
        resolved.Should().BeNull();
        
        resolver.ClearCache();

        // Creating a nullable reference type should also fail.
        // And should throw an exception since the second parameter is true.
        try
        {
            resolver.Get("StringBuilder?", true);
            throw new FailTestException("Expected exception was not thrown.");
        }
        catch (Exception e)
        {
            if (e is FailTestException)
                throw;
            
            // Expected.
            Console.WriteLine($"Got exception: {e.Message}");
        }
    }

    internal class NestedClass
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        // ReSharper disable once UnusedTypeParameter
        public class SubNestedClass<T>;
    }
    
    [UsedImplicitly]
    public class SubNestedClass;

    // ReSharper disable once UnusedTypeParameter
    internal class SubNestedClass<T>;
}