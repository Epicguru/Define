using Xunit.Abstractions;

namespace Define.Tests;

public class DefDatabaseTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestUnregisterDef()
    {
        LoadDefFile("SimpleSubDefs");
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
        DefDatabase.GetAll<IDef>().Should().HaveCount(2);
        DefDatabase.GetAll<TestDef>().Should().HaveCount(2);
        
        // But only one of the specific subclass type:
        DefDatabase.GetAll<AltSubclassDef>().Should().HaveCount(1);
        DefDatabase.GetAll<AltSubclassAbstractDef>().Should().HaveCount(1);
        
        // Now unregister that sub:
        var sub = DefDatabase.GetAll<AltSubclassAbstractDef>().First();
        string id = sub.ID;
        id.Should().NotBeNullOrEmpty();

        DefDatabase.Get(id).Should().Be(sub);
        DefDatabase.UnRegister(sub).Should().BeTrue();
        
        // It should now be unregistered:
        DefDatabase.Get(id).Should().BeNull();
        DefDatabase.GetAll().Should().HaveCount(1);
        DefDatabase.GetAll<TestDef>().Should().HaveCount(1);
        DefDatabase.GetAll<AltSubclassDef>().Should().HaveCount(0);
        DefDatabase.GetAll<AltSubclassAbstractDef>().Should().HaveCount(0);
    }
}
