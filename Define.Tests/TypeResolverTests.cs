using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;

namespace Define.Tests;

public sealed class TypeResolverTests : DefTestBase
{
    public TypeResolverTests()
    {
        // Called before every test in class:
        TypeResolver.ClearCache();
    }

    [Test]
    [MethodDataSource(nameof(Generate_ResolveGenericTypes_Args))]
    public void TestResolveGenericTypes(string name, Type type)
    {
        var resolved = TypeResolver.Get(name);
        resolved.Should().Be(type);
    }

    public static TheoryData<string, Type> Generate_ResolveGenericTypes_Args()
    {
        var data = Generate_ResolveGenericTypes_ArgsBase();

        // Using square brackets:
        int baseCount = data.Count;
        var pending = new List<(string, Type)>();
        for (int i = 0; i < baseCount; i++)
        {
            var pair = data.ElementAt(i);
            pair = [.. pair]; // Copy array to avoid changing original.
            pair[0] = ((string)pair[0]).Replace('<', '[').Replace('>', ']');
            pending.Add(((string)pair[0], (Type)pair[1]));
        }

        foreach (var pair in pending)
        {
            data.Add(pair.Item1, pair.Item2);
        }
        
        data.Count.Should().Be(baseCount * 2);
        return data;
    }

    private static TheoryData<string, Type> Generate_ResolveGenericTypes_ArgsBase()
    {
        var data = new TheoryData<string, Type>
        {
            {"List<string>", typeof(List<string>)}, // Short aliased name.
            {"List<String>", typeof(List<string>)}, // Actual type name.
            {"List<int>", typeof(List<int>)},
            // ReSharper disable once ConvertNullableToShortForm
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
            {"List<Dictionary<Nullable<int>, HashSet<double?>>>", typeof(List<Dictionary<Nullable<int>, HashSet<double?>>>) },
            {"Dictionary<int?, SubNestedClass<Vector2>>", typeof(Dictionary<int?, SubNestedClass<Vector2>>) }
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        };
        return data;
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
        var resolved = TypeResolver.Get(name);
        resolved.Should().Be(type);
    }

    [Test]
    public void TestNullableReferenceType()
    {
        // Attempting to resolve any reference type as a nullable
        // should result in an error.
        var resolved = TypeResolver.Get("string?");
        resolved.Should().BeNull();
        
        TypeResolver.ClearCache();

        // Test exception throwing...
        Assert.ThrowsAny<Exception>(() =>
        {
            resolved = TypeResolver.Get("StringBuilder?", true);
        });
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