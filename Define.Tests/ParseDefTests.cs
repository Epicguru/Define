using Xunit.Abstractions;

namespace Define.Tests;

public sealed class ParseDefTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Theory]
    [MemberData(nameof(CheckInterfaceCallbacks_Args))]
    public void CheckInterfaceCallbacks(bool postLoad, bool latePostLoad, bool errors)
    {
        Config.DoPostLoad = postLoad;
        Config.DoLatePostLoad = latePostLoad;
        Config.DoConfigErrors = errors;

        TestDef def = LoadSingleDef<TestDef>("SimpleTestDef");

        def.ID.Should().Be("MyTestDef");

        def.PostLoadCalled.Should().Be(postLoad);
        def.LatePostLoadCalled.Should().Be(latePostLoad);
        def.ConfigErrorsCalled.Should().Be(errors);
        def.PostXmlConstructCalled.Should().BeTrue();

        // On the struct field, nothing should have been called because it is not specified in the XML.
        def.Inner.PostLoadCalled.Should().BeFalse();
        def.Inner.LatePostLoadCalled.Should().BeFalse();
        def.Inner.ConfigErrorsCalled.Should().BeFalse();
        def.Inner.PostXmlConstructCalled.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(CheckInterfaceCallbacks_Args))]
    public void CheckInterfaceCallbacksWithInner(bool postLoad, bool latePostLoad, bool errors)
    {
        Config.DoPostLoad = postLoad;
        Config.DoLatePostLoad = latePostLoad;
        Config.DoConfigErrors = errors;

        TestDef def = LoadSingleDef<TestDef>("SimpleTestDefWithInner");

        def.ID.Should().Be("MyTestDef");

        def.PostLoadCalled.Should().Be(postLoad);
        def.LatePostLoadCalled.Should().Be(latePostLoad);
        def.ConfigErrorsCalled.Should().Be(errors);
        def.PostXmlConstructCalled.Should().BeTrue();

        def.Inner.PostLoadCalled.Should().Be(postLoad);
        def.Inner.LatePostLoadCalled.Should().Be(latePostLoad);
        def.Inner.ConfigErrorsCalled.Should().Be(errors);
        def.Inner.PostXmlConstructCalled.Should().BeTrue();
    }

    public static IEnumerable<object[]> CheckInterfaceCallbacks_Args()
    {
        return new ArgMatrix
        {
            { [true, false] },
            { [true, false] },
            { [true, false] }
        }.MakeArgs();
    }
}
