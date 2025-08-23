using TUnit.Core.Interfaces;

namespace Define.Monogame.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class GameTest : Attribute, IDataSourceAttribute, ITestExecutor
{
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        Task<object?[]?> task = Task.FromResult<object?[]?>(new object[1]);
        yield return () => task;
    }
    
    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        using var game = new TestGame(_ =>
        {
            action();
        });
        
        context.TestDetails.TestMethodArguments[0] = game;
            
        game.Run();
        return ValueTask.CompletedTask;
    }
}