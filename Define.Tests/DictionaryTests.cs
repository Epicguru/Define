using Xunit.Abstractions;

namespace Define.Tests;

public class DictionaryTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestDictionaryLoading()
    {
        var def = LoadSingleDef<TestDef>("Dictionary");
        def.Dict.Should().NotBeNull();
        def.Dict.Should().HaveCount(3);

        def.ObjectWithDifferentName.Should().Be(555);
        
        var firstItem = def.Dict!["FirstItem"];
        firstItem.Should().NotBeNull();
        firstItem.SomeData.Should().Be("Override first item!");
        firstItem.OtherData.Should().Be(5);
        
        var secondItem = def.Dict!["SecondItem"];
        secondItem.Should().NotBeNull();
        secondItem.SomeData.Should().Be("Second item data");
        secondItem.OtherData.Should().Be(8888);
        
        var thirdItem = def.Dict!["ThirdItem"];
        thirdItem.Should().NotBeNull();
        thirdItem.Should().BeOfType<InnerGrandSub>();
        var inner = (InnerGrandSub)thirdItem;
        inner.SomeData.Should().Be("Third item data");
        inner.OtherData.Should().Be(0);
        inner.AnInt.Should().Be(123);
    }
    
    [Fact]
    public void TestDictionaryNoInherit()
    {
        var def = LoadSingleDef<TestDef>("DictionaryNoInherit");
        def.Dict.Should().NotBeNull();
        def.Dict.Should().HaveCount(2);
        
        var firstItem = def.Dict!["FirstItem"];
        firstItem.Should().NotBeNull();
        firstItem.SomeData.Should().Be("Override first item!");
        firstItem.OtherData.Should().Be(0);
        
        var thirdItem = def.Dict!["ThirdItem"];
        thirdItem.Should().NotBeNull();
        thirdItem.Should().BeOfType<InnerGrandSub>();
        var inner = (InnerGrandSub)thirdItem;
        inner.SomeData.Should().Be("Third item data");
        inner.OtherData.Should().Be(0);
        inner.AnInt.Should().Be(123);
    }
}
