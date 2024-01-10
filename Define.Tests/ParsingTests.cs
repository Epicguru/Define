using Xunit.Abstractions;

namespace Define.Tests;

public class ParsingTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void DelegateParsing()
    {
        var defs = LoadSingleDef<ParserDef>("Parsing/Delegates");
        
        defs.Action.Should().NotBeNull();
        
        defs.SimpleFunc.Should().NotBeNull();
        defs.SimpleFunc!("asd", true).Should().Be(5);

        defs.SimpleFunc2.Should().NotBeNull();
        defs.SimpleFunc2!("asd", true, 12f).Should().Be(12);

        defs.MethodDoMath.Should().NotBeNull();
        float f = 123f;
        defs.MethodDoMath!(12f, ref f).Should().BeApproximately(123f * 12f, 0.0001f);

        defs.MethodWithOutArg.Should().NotBeNull();
        defs.MethodWithOutArg!(12f, out string str);
        str.Should().Be("Hello");
    }

    [Fact]
    public void TypeParsing()
    {
        var defs = LoadSingleDef<ParserDef>("Parsing/Types");
        defs.Types.Should().NotBeNull();
        defs.Types.Should().HaveCount(4);

        foreach (var type in defs.Types)
        {
            type.Should().NotBeNull();
        }
    }
}
