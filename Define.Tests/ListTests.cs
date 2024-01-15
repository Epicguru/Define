using Xunit.Abstractions;

namespace Define.Tests;

public class ListTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestListAndRefLoading()
    {
        LoadDefFile("TestListsAndRefs");
        DefDatabase.GetAll<TestDef>().Count.Should().Be(3);
        
        var def = DefDatabase.Get<TestDef>("MyTestDef")!;
        def.Should().NotBeNull();

        var def2 = DefDatabase.Get<SubclassDef>("MyTestDef2")!;
        def2.Should().NotBeNull();
        
        var def3 = DefDatabase.Get<AltSubclassDef>("MyTestDef3")!;
        var def3Alt = DefDatabase.Get<AltSubclassAbstractDef>("MyTestDef3")!;
        def3.Should().NotBeNull();
        def3Alt.Should().Be(def3);
        
        def.List.Should().NotBeNull();
        def.List.Should().HaveCount(4);
        def.List![0].Should().BeNull();
        def.List![1].Should().Be(def); // Self reference
        def.List![2].Should().Be(def2); // Other ref.
        def.List![3].Should().Be(def3); // Other ref.

        def2.List.Should().NotBeNull();
        def2.List.Should().HaveCount(1);
        def2.List![0].Should().Be(def);
    }

    [Fact]
    public void TestListWithExistingItems()
    {
        var def = LoadSingleDef<TestDef>("ListWithExisting");
        def.ListWithExisting.Should().BeEquivalentTo([1, 2, 3, 55.6f]);
    }

    [Fact]
    public void TestAlternateListNames()
    {
        Config.ListItemName = "list-item";

        var def = LoadSingleDef<TestDef>("AltListItemNames");
        def.List.Should().NotBeNull().And.HaveCount(4);
    }
    
    [Fact]
    public void TestForcedListParse()
    {
        var def = LoadSingleDef<TestDef>("ForceParseAsList");
        def.List.Should().NotBeNull().And.HaveCount(4);
    }
    
    [Fact]
    public void TestBadListNames()
    {
        var def = LoadSingleDef<TestDef>("ParseListBadNames", expectWarnings: true);
        // The bad names are actually fine because no inheritance is happening, but it should give a warning for each
        // name that is not as expected.
        def.List.Should().NotBeNull().And.HaveCount(4);
    }
    
    [Fact]
    public void ElemTypeTest()
    {
        var def = LoadSingleDef<TestDef>("ListElemTypeDef");

        def.InnerDataList.Should().NotBeNull().And.HaveCount(2);
        
        def.InnerDataList![0].Should().BeOfType<InnerGrandSub>();
        ((InnerGrandSub)def.InnerDataList[0]!).AnInt.Should().Be(1234);
        
        def.InnerDataList![1].Should().BeOfType<InnerSub>();
        ((InnerSub)def.InnerDataList[1]!).InnerSubData.Should().Be("Hello, world!");
    }
}
