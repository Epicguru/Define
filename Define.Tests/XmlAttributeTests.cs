using Xunit.Abstractions;

namespace Define.Tests;

public sealed class XmlAttributeTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestNullAttributeSimple()
    {
        LoadDefFile("NullAttrs");

        var nullDef = DefDatabase.Get<TestDef>("MyTestDef")!;
        var allDefs = DefDatabase.GetAll<TestDef>();

        nullDef.Should().NotBeNull();
        allDefs.Should().NotBeNull();
        allDefs.Should().HaveCount(5);

        nullDef.ObjectWithDifferentName.Should().BeNull();

        foreach (var obj in allDefs)
        {
            if (obj == nullDef)
                continue;

            obj.ObjectWithDifferentName.Should().NotBeNull();
        }
    }

    [Fact]
    public void TestNullAttrOnNodeWithContent()
    {
        var def = LoadSingleDef<TestDef>("NullWithContents");
        def.Inner.Should().BeNull();
    }
}
