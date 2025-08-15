using Define.Callbacks;
using Xunit.Abstractions;

namespace Define.Tests;

public sealed class StaticCallbackTest(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestStaticPostLoad()
    {
        StaticCallbackDef.TimesCalled.Should().Be(0);
        
        LoadDefFile("StaticCallbackDefs");
        
        DefDatabase.GetAll<StaticCallbackDef>().Should().HaveCount(2);
        
        var baseDef = DefDatabase.Get<StaticCallbackDef>("StaticCallbackDef");
        baseDef.Should().NotBeNull();
        baseDef!.BaseValue.Should().Be(123);
        
        var childDef = DefDatabase.Get<StaticCallbackChildDef>("StaticCallbackChildDef");
        childDef.Should().NotBeNull();
        childDef!.BaseValue.Should().Be(456);
        
        // StaticPostLoad should only be called once, even if there are multiple defs including subclasses.
        StaticCallbackDef.TimesCalled.Should().Be(1);
    }

    public class StaticCallbackDef : IDef, IStaticPostLoad
    {
        public static int TimesCalled;

        public static void StaticPostLoad(DefDatabase database)
        {
            // Note: this is currently always called on a single thread (per database), but just in case
            // this changes in the future, we use Interlocked to increment.
            Interlocked.Increment(ref TimesCalled);
        }
        
        public string ID { get; set; } = null!;

        public int BaseValue;
    }

    public sealed class StaticCallbackChildDef : StaticCallbackDef
    {
        public int ChildValue;
    }
}