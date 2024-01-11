using TestSharedLib;
using Xunit.Abstractions;

namespace Define.Zip.Tests;

public class ZipFileTests(ITestOutputHelper output) : DefTestBase(output)
{
    private void EnsureLoadedCorrectly()
    {
        DefDatabase.Count.Should().Be(2);
        
        var a = DefDatabase.Get<SimpleDef>("ExampleDef")!;
        a.Data.Should().Be("ExampleDef data here");
        
        var b = DefDatabase.Get<SimpleDef>("ExampleDef2")!;
        b.Data.Should().Be("ExampleDef2 data here");
    }

    [Fact]
    public void TestReadUnEncryptedDefs()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.AddDefsFromZip("./Content/Defs.zip", null!).Should().BeTrue();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();

        EnsureLoadedCorrectly();
    }
    
    [Fact]
    public async Task TestReadUnEncryptedDefsAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefsFromZipAsync("./Content/Defs.zip", null!)).Should().BeTrue();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();

        EnsureLoadedCorrectly();
    }
    
    [Fact]
    public void TestReadUnEncryptedDefsWithPassword()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.AddDefsFromZip("./Content/Defs.zip", "Wrong password").Should().BeTrue();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();
        
        EnsureLoadedCorrectly();
    }
    
    [Fact]
    public async Task TestReadUnEncryptedDefsWithPasswordAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefsFromZipAsync("./Content/Defs.zip", "Wrong password")).Should().BeTrue();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();
        
        EnsureLoadedCorrectly();
    }
    
    [Fact]
    public void TestReadEncryptedDefs_FailNoPassword()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.AddDefsFromZip("./Content/DefsEncrypted.zip").Should().BeFalse();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().NotBeEmpty();
        WarningMessages.Should().BeEmpty();
    }
    
    [Fact]
    public async Task TestReadEncryptedDefs_FailNoPasswordAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefsFromZipAsync("./Content/DefsEncrypted.zip")).Should().BeFalse();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().NotBeEmpty();
        WarningMessages.Should().BeEmpty();
    }
    
    [Fact]
    public void TestReadEncryptedDefs_FailWrongPassword()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.AddDefsFromZip("./Content/DefsEncrypted.zip", "asd123").Should().BeFalse();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().NotBeEmpty();
        WarningMessages.Should().BeEmpty();
    }
    
    [Fact]
    public async Task TestReadEncryptedDefs_FailWrongPasswordAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefsFromZipAsync("./Content/DefsEncrypted.zip", "asd123")).Should().BeFalse();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().NotBeEmpty();
        WarningMessages.Should().BeEmpty();
    }
    
    [Fact]
    public void TestReadEncryptedDefs()
    {
        DefDatabase.StartLoading(Config);
        DefDatabase.AddDefsFromZip("./Content/DefsEncrypted.zip", "ExamplePassword").Should().BeTrue();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();
        
        EnsureLoadedCorrectly();
    }
    
    [Fact]
    public async Task TestReadEncryptedDefsAsync()
    {
        DefDatabase.StartLoading(Config);
        (await DefDatabase.AddDefsFromZipAsync("./Content/DefsEncrypted.zip", "ExamplePassword")).Should().BeTrue();
        DefDatabase.FinishLoading();

        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();
        
        EnsureLoadedCorrectly();
    }

    [Fact]
    public void CompareEncryptedAndNonEncrypted()
    {
        var db1 = new DefDatabase();
        var db2 = new DefDatabase();
        db1.StartLoading(Config);
        db2.StartLoading(Config);

        db1.AddDefsFromZip("./Content/Defs.zip");
        db2.AddDefsFromZip("./Content/DefsEncrypted.zip", "ExamplePassword");
        
        db1.FinishLoading();
        db2.FinishLoading();

        db1.Count.Should().Be(2);
        
        // Should have the same defs in the same order.
        db1.GetAll().Should().BeEquivalentTo(db2.GetAll());
    }
}