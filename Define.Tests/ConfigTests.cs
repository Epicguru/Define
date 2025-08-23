using System.Reflection;
using TUnit.Core;
using Xunit.Abstractions;

namespace Define.Tests;

public sealed class ConfigTests : DefTestBase
{
    [Test]
    [Arguments(MemberTypes.Field, true)]
    [Arguments(MemberTypes.Field | MemberTypes.Property, true)]
    [Arguments(MemberTypes.Property, false)]
    [Arguments(MemberTypes.All, true)]
    [Arguments(MemberTypes.Custom, false)]
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
    
    [Test]
    [Arguments(MemberTypes.Property, true)]
    [Arguments(MemberTypes.Field | MemberTypes.Property, true)]
    [Arguments(MemberTypes.Field, false)]
    [Arguments(MemberTypes.All, true)]
    [Arguments(MemberTypes.Custom, false)]
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

    [Test]
    public void TestPropertyWithNoGetter()
    {
        Config.DefaultMemberTypes |= MemberTypes.Property;
        
        var def = LoadSingleDef<MemberTypeDef>("PropNoGetter");
        def.DidWritePropertyNoGetter.Should().BeTrue();
    }

    [Test]
    public void TestPropertyWithNoSetter()
    {
        // This is invalid and should always fail.
        Config.DefaultMemberTypes |= MemberTypes.Property;
        
        LoadSingleDef<MemberTypeDef>("PropNoSetter", expectErrors: true);
        ErrorMessages.Should().ContainMatch("Failed to find member called 'PropertyNoSetter'*");
    }

    [Test]
    public void TestStaticMemberWriting()
    {
        Config.DefaultMemberTypes |= MemberTypes.Property | MemberTypes.Field;
        Config.DefaultMemberBindingFlags |= BindingFlags.Static;

        LoadSingleDef<MemberTypeDef>("StaticMembers");

        MemberTypeDef.StaticField.Should().Be("StaticFieldData");
        MemberTypeDef.StaticProperty.Should().Be("StaticPropData");
    }

    [Test]
    public void IgnoreShouldBeIgnored()
    {
        var def = LoadSingleDef<MemberTypeDef>("WriteIgnored", expectErrors: true);
        def.Ignored.Should().BeNull();
        ErrorMessages.Should().ContainMatch("Failed to find member called 'Ignored'*");
    }
    
    [Test]
    [Arguments(true)]
    [Arguments(false)]
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

    [Test]
    [Arguments(true,  true,  false)]
    [Arguments(true,  false, true)]
    [Arguments(false, true,  true)]
    [Arguments(false, false, true)]
    public void TestCaseSensitivity(bool caseSensitive, bool lowercase, bool shouldFind)
    {
        string toLoad = lowercase ? "CaseSensitivity_Lowercase" : "CaseSensitivity_Uppercase";
        Config.MemberNamesAreCaseSensitive = caseSensitive;

        var def = LoadSingleDef<TestDef>(toLoad, expectErrors: !shouldFind);
        if (!shouldFind)
        {
            ErrorMessages.Should().ContainMatch("Failed to find member called *");
        }
        else
        {
            def.SimpleString.Should().Be("Correct write.");
        }
    }
}
