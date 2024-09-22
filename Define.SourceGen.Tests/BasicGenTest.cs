using Define.SourceGen.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace Define.SourceGen.Tests;

[Example]
public class BasicGenTest(ITestOutputHelper output)
{
    
    [Fact]
    public void Test1()
    {
        output.WriteLine("Test running!");
    }
}