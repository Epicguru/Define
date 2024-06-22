using Xunit.Abstractions;

namespace Define.Tests;

public sealed class AliasTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Theory]
    [InlineData("AliasDef1", true, "This is name1.")]
    [InlineData("AliasDef2", true, "This is name2.")]
    [InlineData("AliasDef3", false, "This is name3.")]
    [InlineData("AliasDef4", false, "This is name4.")]
    public void CheckAlias(string defName, bool singleAttr, string expected)
    {
        LoadDefFile("AliasDefs");
        DefDatabase.Count.Should().Be(4);

        var def = DefDatabase.Get<TestDef>(defName);
        def.Should().NotBeNull();

        if (singleAttr)
        {
            def!.MultiAliasSingleAttribute.Should().Be(expected);
        }
        else
        {
            def!.MultiAliasMultiAttribute.Should().Be(expected);
        }
    }

    [Fact]
    public void MultipleAssignToAliasShouldGiveWarning()
    {
        LoadDefFile("AliasMultipleAssign", expectWarnings: true);
        DefDatabase.Count.Should().Be(1);

        var def = DefDatabase.Get<TestDef>("AliasDef1");
        def.Should().NotBeNull();
        // Multiple assignment should use the last one.
        def!.MultiAliasSingleAttribute.Should().Be("This is name2.");

        WarningMessages.Count.Should().Be(1);
        string msg = WarningMessages[0];
        msg.Should().Contain("Duplicate assignment to member 'MultiAliasSingleAttribute'");
    }

    [Fact]
    public void MultipleAssignShouldGiveWarning()
    {
        LoadDefFile("SimpleMultipleAssign", expectWarnings: true);
        DefDatabase.Count.Should().Be(1);

        var def = DefDatabase.Get<TestDef>("Def1");
        def.Should().NotBeNull();
        // Multiple assignment should use the last one.
        def!.SimpleString.Should().Be("C");

        WarningMessages.Count.Should().Be(2);
        string msg = WarningMessages[0];
        msg.Should().Contain("Duplicate assignment to member 'SimpleString'");
    }
}
