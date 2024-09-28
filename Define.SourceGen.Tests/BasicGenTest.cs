using FluentAssertions;
using TestSharedLib;
using Xunit;
using Xunit.Abstractions;

namespace Define.SourceGen.Tests;

//[Example]
public class BasicGenTest(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void Test1()
    {
        var def = LoadSingleDef<ExampleDef>("ExampleDef", expectWarnings: true);
        WarningMessages.Should().Contain("[ExampleDef] A warning.");
        
        //def.ConfigErrorsGenerated(new ConfigErrorReporter());
    }
}

public partial class ExampleDef : IDef, IConfigErrors
{
    public string ID { get; set; } = null!;

    [Required]
    [Assert("!= 'Invalid'")]
    public string Required = null!; // Populate!

    [Assert("is > 0 and < 10")]
    public int RangedInt = 5;
    
    public void ConfigErrors(ConfigErrorReporter config)
    {
        config.Warn("A warning.");
    }
}

