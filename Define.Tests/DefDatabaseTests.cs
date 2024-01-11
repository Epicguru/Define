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
        var sub = DefDatabase.GetAll<AltSubclassAbstractDef>()[0];
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
    
    [Fact]
    public void TestLoadMultipleDefs()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.AddDefDocument(File.ReadAllText("./Defs/SimpleSubDefs.xml"), "SimpleSubDefs.xml").Should().BeTrue();
        DefDatabase.AddDefDocument(File.ReadAllText("./Defs/NullWithContents.xml"), "NullWithContents.xml").Should().BeTrue();
        DefDatabase.FinishLoading();
        
        // There should not be any errors or warnings.
        WarningMessages.Should().BeEmpty();
        ErrorMessages.Should().BeEmpty();
        
        // Should be 3 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(3);
        DefDatabase.GetAll<IDef>().Should().HaveCount(3);
        DefDatabase.GetAll<TestDef>().Should().HaveCount(3);
        
        // But only one of the specific subclass type:
        DefDatabase.GetAll<AltSubclassDef>().Should().HaveCount(1);
        DefDatabase.GetAll<AltSubclassAbstractDef>().Should().HaveCount(1);
    }

    [Fact]
    public void TestLoadFolder()
    {
        DefDatabase.StartLoading(Config);
        (DefDatabase.AddDefFolder("./Defs/Parsing")).Should().BeTrue();
        DefDatabase.FinishLoading();
        
        // There should not be any errors or warnings.
        WarningMessages.Should().BeEmpty();
        ErrorMessages.Should().BeEmpty();
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
    }
    
    [Fact]
    public async Task TestLoadFolderAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefFolderAsync("./Defs/Parsing")).Should().BeTrue();
        DefDatabase.FinishLoading();
        
        // There should not be any errors or warnings.
        WarningMessages.Should().BeEmpty();
        ErrorMessages.Should().BeEmpty();
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void TestLoadFromStream()
    {
        DefDatabase.StartLoading(Config);
        
        using var fs = new FileStream("./Defs/SimpleSubDefs.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
        DefDatabase.AddDefDocument(fs, "SimpleSubDefs.xml").Should().BeTrue();
        
        DefDatabase.FinishLoading();
        
        // There should not be any errors or warnings.
        WarningMessages.Should().BeEmpty();
        ErrorMessages.Should().BeEmpty();
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void TestLoadFromZipFile()
    {
        DefDatabase.StartLoading(Config);
        (DefDatabase.AddDefsFromZip("./Defs/Parsing.zip")).Should().BeTrue();
        DefDatabase.FinishLoading();
        
        // There should not be any errors or warnings.
        WarningMessages.Should().BeEmpty();
        ErrorMessages.Should().BeEmpty();
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
    }
    
    [Fact]
    public async Task TestLoadFromZipFileAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefsFromZipAsync("./Defs/Parsing.zip")).Should().BeTrue();
        DefDatabase.FinishLoading();
        
        // There should not be any errors or warnings.
        WarningMessages.Should().BeEmpty();
        ErrorMessages.Should().BeEmpty();
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
    }
    
    [Fact]
    public async Task TestLoadFromStreamAsync()
    {
        await using var fs = new FileStream("./Defs/SimpleSubDefs.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
        DefDatabase.StartLoading(Config);
        await DefDatabase.AddDefDocumentAsync(fs, "SimpleSubDefs.xml");
        DefDatabase.FinishLoading();
        
        // Should be 2 defs loaded:
        DefDatabase.GetAll().Should().HaveCount(2);
    }
}
