using System.Xml;
using Xunit.Abstractions;

namespace Define.Tests;

public class SanityChecks(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public async Task CanLoadXmlDoc()
    {
        const string PATH = "./Defs/DummyXMLFile.xml";
        File.Exists(PATH).Should().BeTrue();

        var doc = new XmlDocument();
        doc.LoadXml(await File.ReadAllTextAsync(PATH));
        doc.InnerText.Should().NotBeEmpty();
    }
}