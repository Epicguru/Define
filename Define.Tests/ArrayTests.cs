using TUnit.Core;
using Xunit.Abstractions;

namespace Define.Tests;

public class ArrayTests : DefTestBase
{
    [Test]
    public void TestLoadArray()
    {
        var def = LoadSingleDef<TestDef>("ArrayDef");
        def.Array.Should().BeEquivalentTo([123.4f, 998.1f]);
    }
    
    [Test]
    public void TestLoadArrayWithExisting()
    {
        var def = LoadSingleDef<TestDef>("ArrayDefWithExisting");
        def.ArrayWithExisting.Should().BeEquivalentTo([1, 2, 3, 123.4f, 998.1f]);
    }
    
    [Test]
    public void TestLoadArrayWithInheritance()
    {
        var def = LoadSingleDef<TestDef>("ArrayDefWithInheritance");
        def.Array.Should().BeEquivalentTo([123.4f, 998.1f, 12.1f]);
    }
}
