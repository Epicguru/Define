using Define.SourceGen.Attributes;
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
    
    [Example]
    public partial class ExampleDef : IDef, IConfigErrors
    {
        public string ID { get; set; } = null!;

        [Example]
        public string Required; // Populate!
    
        public void ConfigErrors(ConfigErrorReporter config)
        {
            config.Warn("A warning.");
        }
    }
}

