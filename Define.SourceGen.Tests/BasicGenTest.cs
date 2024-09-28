using Define.SourceGen.Attributes;
using FluentAssertions;
using TestSharedLib;
using Xunit;
using Xunit.Abstractions;

namespace Define.SourceGen.Tests;

public class BasicGenTest(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestExampleDef()
    {
        var def = LoadSingleDef<ExampleDef>("ExampleDef", expectWarnings: true);

        WarningMessages.Should().HaveCount(2);
        WarningMessages.Should().Contain("[ExampleDef] A warning.");
        WarningMessages.Should().Contain("[ExampleDef] Assert failed: SomeFloat is > 0");
    }
}

public partial class ExampleDef : IDef, IConfigErrors
{
    public string ID { get; set; } = null!;

    [Required]
    [Assert("!= 'Invalid'")]
    public string Required = null!;

    [Assert("is > 0 and < 10")]
    public int RangedInt = 5;

    [Assert("is > 0", isError: false)]
    public float SomeFloat = -1;

    [Min(5.23f)]
    public float HasMin = 5;
    
    public void ConfigErrors(ConfigErrorReporter config)
    {
        config.Warn("A warning.");
    }
}

