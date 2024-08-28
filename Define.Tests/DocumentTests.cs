using System.Xml;
using Xunit.Abstractions;

namespace Define.Tests;

public class DocumentTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void TestXPathGeneration()
    {
        var document = new XmlDocument
        {
            PreserveWhitespace = true
        };
        
        document.LoadXml(File.ReadAllText("./Defs/XPathDef.xml"));

        Stack<XmlElement> nodes = new Stack<XmlElement>();
        foreach (XmlNode child in document.ChildNodes)
        {
            if (child is XmlElement e)
                nodes.Push(e);
        }
        
        while (nodes.TryPop(out var node))
        {
            string xPath = node.GetFullXPath();
            xPath.Should().NotBeNullOrEmpty();

            string txt = node.OuterXml.Replace("\n", "");
            if (txt.Length > 30)
                txt = txt[..30] + "...";
            Output.WriteLine($"'{txt}' -> '{xPath}'");

            var found = document.SelectSingleNode(xPath);
            found.Should().NotBeNull();
            (found == node).Should().BeTrue();

            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node)
                {
                    if (child is XmlElement e)
                        nodes.Push(e);
                }
            }
        }
    }
}
