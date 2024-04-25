using FluentAssertions;
using System.Diagnostics;
using System.Reflection;
using TestSharedLib;
using Xunit.Abstractions;

namespace Define.FastCache.Tests;

public class FastCacheTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestSerialize()
    {
        // Allow static fields too.
        Config.DefaultMemberBindingFlags |= BindingFlags.Static;
        
        DefDatabase.AddDefFolder("./Content");
        DefDatabase.FinishLoading();
        
        CheckDatabaseIsGood(DefDatabase);

        var cache = DefDatabase.ToFastCache();
        cache.Defs.Should().BeEquivalentTo(DefDatabase.GetAll());
        cache.StaticClassData.Should().ContainSingle(p => p.Key == typeof(SimpleDef));
        
        // Serialize.
        byte[] serialised = cache.Serialize();
        serialised.Length.Should().BeGreaterThan(0);
        serialised.Should().Contain(b => b != 0);
        
        // Deserialize.
        var db2 = new DefDatabase(Config);
        var cache2 = new DefFastCache(serialised, Config);
        cache2.Should().BeEquivalentTo(cache);
        
        // Check new load was successful.
        cache2.LoadIntoDatabase(db2);
        CheckDatabaseIsGood(db2);
        
        // Compare the db's again.
        DefDatabase.GetAll().Should().BeEquivalentTo(db2.GetAll());
        // This checks for reference equality:
        DefDatabase.GetAll().Should().NotIntersectWith(db2.GetAll());
    }

    [Fact]
    public void FastCacheShouldBeFasterThanXml()
    {
        // Attempt to mitigate external processes and other threads interfering with the test:
        SetupProcessorAndThreadPriority();

        // Allow static fields too.
        Config.DefaultMemberBindingFlags |= BindingFlags.Static;

        var timer = Stopwatch.StartNew();
        DefDatabase.AddDefFolder("./Content");
        DefDatabase.FinishLoading();
        timer.Stop();
        var baseline = timer.Elapsed;
        
        CheckDatabaseIsGood(DefDatabase);

        var toCache = DefDatabase.ToFastCache();
        var savedCache = toCache.Serialize();

        var newDb = new DefDatabase(Config);
        
        timer = Stopwatch.StartNew();
        var loadedCache = new DefFastCache(savedCache, Config);
        loadedCache.LoadIntoDatabase(newDb);
        timer.Stop();
        
        CheckDatabaseIsGood(newDb);
        
        Output.WriteLine($"XML {baseline.TotalMilliseconds:F3} ms vs Ceras {timer.Elapsed.TotalMilliseconds:F3} ms");
        baseline.Should().BeGreaterThan(timer.Elapsed);
    }

    private void SetupProcessorAndThreadPriority()
    {
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
        }
        catch (Exception e)
        {
            Output.WriteLine($"Failed to set process or thread priority...\n{e}");
        }
    }

    private void CheckDatabaseIsGood(DefDatabase db)
    {
        // Should be no issues.
        ErrorMessages.Should().BeEmpty();
        WarningMessages.Should().BeEmpty();
        
        db.Count.Should().Be(3002);

        // Check def contents...
        var def = db.Get<SimpleDef>("ExampleDef");
        var def2 = db.Get<SimpleDef>("ExampleDef2");
        
        def.Should().NotBeNull();
        def!.Data.Should().Be("ExampleDef data here");
        def.Ref.Should().Be(def2);
        def.SelfRef.Should().Be(def);
        
        def2.Should().NotBeNull();
        def2!.Data.Should().Be("ExampleDef2 data here");
        def2.Ref.Should().Be(def);
        def2.SelfRef.Should().Be(def2);
        
        // Should have populated static data.
        SimpleDef.StaticField.Should().Be("Some static data here!");
        // These should not have been modified:
        SimpleDef.StaticProperty.Should().Be("asd123");
    }
}