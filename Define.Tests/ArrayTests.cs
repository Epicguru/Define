using Xunit.Abstractions;

namespace Define.Tests;

public class ArrayTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestLoadArray()
    {
        var def = LoadSingleDef<TestDef>("ArrayDef");
        def.Array.Should().BeEquivalentTo([123.4f, 998.1f]);
    }
    
    [Fact]
    public void TestLoadArrayWithExisting()
    {
        var def = LoadSingleDef<TestDef>("ArrayDefWithExisting");
        def.ArrayWithExisting.Should().BeEquivalentTo([1, 2, 3, 123.4f, 998.1f]);
    }
    
    [Fact]
    public void TestLoadArrayWithInheritance()
    {
        var def = LoadSingleDef<TestDef>("ArrayDefWithInheritance");
        def.Array.Should().BeEquivalentTo([123.4f, 998.1f, 12.1f]);
    }
}
