using Xunit.Abstractions;

namespace Define.Tests;

public class ExceptionHandlingTests(ITestOutputHelper output) : DefTestBase(output)
{
    [Fact]
    public void ExceptionsInCallbacksAreCaughtAndReported()
    {
        var def = LoadSingleDef<ThrowExceptionsDef>("ExceptionDef", expectErrors: true);
        
        // Despite the exceptions, the data should be there:
        def.SomeData.Should().Be("Some Data");
        
        // There should be 3 exceptions thrown and logged.
        ErrorMessages.Should().HaveCount(3);
    }
    
    [Fact]
    public void ExceptionInConstructorIsHandled()
    {
        ThrowExceptionsDef.ThrowInConstructor = true;
        var def = TryLoadSingleDef<ThrowExceptionsDef>("ExceptionDef", expectErrors: true);
        def.Should().BeNull();
        
        // There will probably be multiple errors logged.
        // because the def will be attempted to be instantiated several times.
        // For example, once at the start for pre-population of defs,
        // then again when the def node is reached.
        // Note: they may be errors or parse errors depending on the context, 
        // so check for both.
        int sum = ErrorMessages.Count;
        sum.Should().BeGreaterThan(0);
    }

    public override void Dispose()
    {
        base.Dispose();
        ThrowExceptionsDef.ThrowInConstructor = false;
    }
}