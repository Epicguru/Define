using System.Diagnostics;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace Define.Monogame.Tests;

public sealed class GameTest : NotInParallelAttribute, IDataSourceAttribute, ITestExecutor, IParallelConstraint, ITestRegisteredEventReceiver
{
    public GameTest() : base("DEFINE_MONOGAME_GAME_TEST") { }
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var args = dataGeneratorMetadata.TestInformation?.Parameters;
        if (args is not { Length: 1 } || !typeof(Microsoft.Xna.Framework.Game).IsAssignableFrom(args[0].Type))
        {
            throw new Exception("GameTest attribute can only be applied to tests with a single Game parameter.");
        }

        Task<object?[]?> task = Task.FromResult<object?[]?>(new object[1]);
        yield return () => task;
    }

    private static void ForceUIThreadToCurrent()
    {
        // In Monogame (and probably XNA) many operations must be done on the main 'UI' thread.
        // The UI thread is determined via a static constructor on the Threading class.
        // This causes problems when running tests, since each test may be run on a different thread
        // even when the tests are run in sequence (not in parallel).
        // Solve this by forcibly changing the 'UI thread' to the current thread before running the test.

        try
        {
            Type? threadingClass = Type.GetType("Microsoft.Xna.Framework.Threading, MonoGame.Framework");
            if (threadingClass == null)
            {
                Console.WriteLine("Failed to find Microsoft.Xna.Framework.Threading class, UI thread cannot be set.");
                return;
            }

            FieldInfo? threadIdField = threadingClass.GetField("_mainThreadId", BindingFlags.Static | BindingFlags.NonPublic);
            if (threadIdField == null)
            {
                Console.WriteLine("Failed to find _mainThreadId field, UI thread cannot be set.");
                return;
            }
            
            Debug.Assert(threadIdField.FieldType == typeof(int), "Expected _mainThreadId to be of type int.");
            threadIdField.SetValue(null, Environment.CurrentManagedThreadId);
            
            Console.WriteLine($"Successfully set UI thread to thread ID {Environment.CurrentManagedThreadId}.");
        }
        catch
        {
            Console.WriteLine("Failed to set UI thread, tests may fail if they require the UI thread (should not be an issue in KNI).");
        }
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        ForceUIThreadToCurrent();

        using var game = new TestGame(_ =>
        {
            action().AsTask().Wait();
        });
        
        // Assign Game argument.
        context.TestDetails.TestMethodArguments[0] = game;
        
        game.Run();
        return ValueTask.CompletedTask;
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        // Skip this test if no GPU is available.
        // This is the exact same logic as in RequiresGpuAttribute.
        if (RequiresGpuAttribute.ShouldSkip())
        {
            context.TestContext.SkipReason = "This test requires a GPU (graphics adapter) to run.";
        }
        
        return ValueTask.CompletedTask;
    }
}