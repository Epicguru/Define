using System.Reflection;
using Xunit.Abstractions;

namespace Define.Tests;

public sealed class ConfigTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Theory]
    [InlineData(MemberTypes.Field, true)]
    [InlineData(MemberTypes.Field | MemberTypes.Property, true)]
    [InlineData(MemberTypes.Property, false)]
    [InlineData(MemberTypes.All, true)]
    [InlineData(MemberTypes.Custom, false)]
    public void TestFieldDiscovery(MemberTypes type, bool shouldFindField)
    {
        Config.DefaultMemberTypes = type;

        var def = LoadSingleDef<MemberTypeDef>("FieldFindDef", expectErrors: !shouldFindField);

        if (shouldFindField)
        {
            def.Field.Should().Be("Some Data");
        }
        else
        {
            def.Field.Should().BeNull();
            // There should be an error message in the log.
            ErrorMessages.Should().ContainMatch("Failed to find member called 'Field'*");
        }
    }
    
    [Theory]
    [InlineData(MemberTypes.Property, true)]
    [InlineData(MemberTypes.Field | MemberTypes.Property, true)]
    [InlineData(MemberTypes.Field, false)]
    [InlineData(MemberTypes.All, true)]
    [InlineData(MemberTypes.Custom, false)]
    public void TestPropertyDiscovery(MemberTypes type, bool shouldFindProperty)
    {
        Config.DefaultMemberTypes = type;

        var def = LoadSingleDef<MemberTypeDef>("PropFindDef", expectErrors: !shouldFindProperty);
        if (shouldFindProperty)
        {
            def.Property.Should().Be("Some Data");
        }
        else
        {
            def.Property.Should().BeNull();
            // There should be an error message in the log.
            ErrorMessages.Should().ContainMatch("Failed to find member called 'Property'*");
        }
    }

    [Fact]
    public void TestPropertyWithNoGetter()
    {
        Config.DefaultMemberTypes |= MemberTypes.Property;
        
        var def = LoadSingleDef<MemberTypeDef>("PropNoGetter");
        def.DidWritePropertyNoGetter.Should().BeTrue();
    }

    [Fact]
    public void TestPropertyWithNoSetter()
    {
        // This is invalid and should always fail.
        Config.DefaultMemberTypes |= MemberTypes.Property;
        
        LoadSingleDef<MemberTypeDef>("PropNoSetter", expectErrors: true);
        ErrorMessages.Should().ContainMatch("Failed to find member called 'PropertyNoSetter'*");
    }

    [Fact]
    public void TestStaticMemberWriting()
    {
        Config.DefaultMemberTypes |= MemberTypes.Property | MemberTypes.Field;
        Config.DefaultMemberBindingFlags |= BindingFlags.Static;

        LoadSingleDef<MemberTypeDef>("StaticMembers");

        MemberTypeDef.StaticField.Should().Be("StaticFieldData");
        MemberTypeDef.StaticProperty.Should().Be("StaticPropData");
    }

    [Fact]
    public void IgnoreShouldBeIgnored()
    {
        var def = LoadSingleDef<MemberTypeDef>("WriteIgnored", expectErrors: true);
        def.Ignored.Should().BeNull();
        ErrorMessages.Should().ContainMatch("Failed to find member called 'Ignored'*");
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncludeShouldBeIncluded(bool exclude)
    {
        // Regardless of whether private fields are included, the XmlInclude
        // attribute should cause it to be included.
        if (exclude)
            Config.DefaultMemberBindingFlags &= ~BindingFlags.NonPublic;
        else
            Config.DefaultMemberBindingFlags |= BindingFlags.NonPublic;
        
        var def = LoadSingleDef<MemberTypeDef>("WriteIncluded");
        def.GetIncluded().Should().Be("Some Data");
    }
}
