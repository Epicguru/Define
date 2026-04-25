using Define.FastCache;
using Define.Monogame.Tests.DefClasses;
using Microsoft.Xna.Framework.Graphics;

namespace Define.Monogame.Tests;

public class ParserTesters : MonogameDefTestBase
{
    [Test]
    public void TestVectorParsers()
    {
        var single = LoadSingleDef<VectorDef>("VectorDef");
        single.EnsureExpected();
    }
    
    [Test]
    public void TestRectangleParser()
    {
        var single = LoadSingleDef<RectangleDef>("RectangleDef");
        single.EnsureExpected();
    }
    
    [Test]
    public void TestColorParser()
    {
        var single = LoadSingleDef<ColorDef>("ColorDef");
        single.EnsureExpected();
    }

    [Test]
    public void TestFastCache()
    {
        DefDatabase.Loader.AddMonogameDataParsers();
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
        var fastCache = DefDatabase.ToFastCache();

        byte[] cacheData = fastCache.Serialize();
        cacheData.Should().HaveCountGreaterThan(0).And.Contain(b => b != 0);
        Console.WriteLine($"Serialized all MG defs into {cacheData.Length} bytes.");
        
        // Deserialize.
        var loadedCache = new DefFastCache(cacheData, DefDatabase.Config);
        
        // Load into new database.
        var db2 = new DefDatabase(Config);
        loadedCache.LoadIntoDatabase(db2);

        // Make sure it is all the same...
        foreach (var def in db2.GetAll<MGDefBase>())
        {
            def.EnsureExpected();
        }
        db2.GetAll().Should().BeEquivalentTo(DefDatabase.GetAll());
    }

    [Test]
    [GameTest]
    public void TestGameRunBaseline(TestGame game)
    {
        Console.WriteLine("Game ran successfully.");
    }

    [Test]
    [GameTest]
    public void TestGameLoadContentManual(TestGame game)
    {
        using var tex = game.ContentManager.Load<Texture2D>("Content/MyImage");
        tex.Should().NotBeNull();
        tex.Width.Should().Be(128);
        tex.Height.Should().Be(128);
    }

    [Test]
    [GameTest]    
    public void TestParseTexture(TestGame game)
    {
        game.Should().NotBeNull();
        game.ContentManager.Should().NotBeNull();
        DefDatabase.Should().NotBeNull();
        DefDatabase.Loader.Should().NotBeNull();
        
        DefDatabase.Loader.AddMonogameContentParsers(game.ContentManager);
        DefDatabase.AddDefDocument(File.ReadAllText("./Defs/ContentDef.xml"), "ContentDef.xml");
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();
            
        var def = DefDatabase.Get<ContentDef>("ContentDef");
        def.Should().NotBeNull();
        def!.Texture.Should().NotBeNull();
        def.Texture!.Width.Should().Be(128);
        def.Texture.Height.Should().Be(128);
    }
}