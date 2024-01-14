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
}