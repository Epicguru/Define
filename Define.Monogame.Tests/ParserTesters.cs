using Define.FastCache;
using Define.Monogame.Tests.DefClasses;
using Microsoft.Xna.Framework.Graphics;
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
        DefDatabase.Loader!.AddMonogameDataParsers();
        DefDatabase.AddDefFolder("./Defs", fileFilter: f => !f.EndsWith("ContentDef.xml"));
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

    [Fact]
    public void TestGameRunBaseline()
    {
        using var game = new TestGame(_ =>
        {
            
        });
        game.Run();
    }
    
    [Fact]
    public void TestGameLoadContentManual()
    {
        using var game = new TestGame(g =>
        {
            using var tex = g.ContentManager.Load<Texture2D>("Content/MyImage");
            tex.Should().NotBeNull();
            tex.Width.Should().Be(128);
            tex.Height.Should().Be(128);
        });
        game.Run();
    }
    
    [Fact]
    public void TestParseTexture()
    {
        using var game = new TestGame(g =>
        {
            DefDatabase.StartLoading(Config);
            DefDatabase.Loader!.AddMonogameContentParsers(g.ContentManager);
            DefDatabase.AddDefDocument(File.ReadAllText("./Defs/ContentDef.xml"), "ContentDef.xml");
            DefDatabase.FinishLoading();

            ErrorMessages.Should().BeEmpty();
            WarningMessages.Should().BeEmpty();
            
            var def = DefDatabase.Get<ContentDef>("ContentDef");
            def.Should().NotBeNull();
            def!.Texture.Should().NotBeNull();
            def.Texture!.Width.Should().Be(128);
            def.Texture.Height.Should().Be(128);
        });
        game.Run();
    }
}