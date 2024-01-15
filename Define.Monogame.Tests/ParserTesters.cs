using Define.FastCache;
using Define.Monogame.Tests.DefClasses;
using Xunit.Abstractions;

namespace Define.Monogame.Tests;

public class ParserTesters(ITestOutputHelper output) : MonogameDefTestBase(output)
{
    [Fact]
    public void TestVectorParsers()
    {
        var single = LoadSingleDef<VectorDef>("VectorDef");
        single.EnsureExpected();
    }
    
    [Fact]
    public void TestRectangleParser()
    {
        var single = LoadSingleDef<RectangleDef>("RectangleDef");
        single.EnsureExpected();
    }
    
    [Fact]
    public void TestColorParser()
    {
        var single = LoadSingleDef<ColorDef>("ColorDef");
        single.EnsureExpected();
    }

    [Fact]
    public void TestFastCache()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.Loader!.AddMonogameParsers();
        DefDatabase.AddDefFolder("./Defs");
        DefDatabase.FinishLoading();

        const int EXPECTED = 3;
        DefDatabase.GetAll().Should().HaveCount(EXPECTED);
        var all = DefDatabase.GetAll<MGDefBase>();
        all.Should().HaveCount(EXPECTED);

        foreach (var def in all)
        {
            def.EnsureExpected();
        }
        
        // Make fast cache.
        var fastCache = DefDatabase.CreateFastCache();

        byte[] cacheData = fastCache.Serialize();
        cacheData.Should().HaveCountGreaterThan(0).And.Contain(b => b != 0);
        Output.WriteLine($"Serialized all MG defs into {cacheData.Length} bytes.");
        
        // Deserialize.
        var loadedCache = DefFastCache.Load(cacheData, DefDatabase.Config!);
        
        // Load into new database.
        var db2 = new DefDatabase();
        loadedCache.LoadIntoDatabase(db2);

        // Make sure it is all the same...
        foreach (var def in db2.GetAll<MGDefBase>())
        {
            def.EnsureExpected();
        }
        db2.GetAll().Should().BeEquivalentTo(DefDatabase.GetAll());
    }
}