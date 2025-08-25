using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using TUnit.Core.Interfaces;

namespace Define.Monogame.Tests;

/// <summary>
/// An attribute that can be applied to test methods that need to run inside a MonoGame Game instance.
/// The test method MUST have a single parameter of type <see cref="TestGame"/>, even if it does not use it.
/// Applying this attribute will do the following:
/// <list type="bullet">
/// <item>Ensure that this test does not run in parallel with other [GameTest] games.</item>
/// <item>Ensure that the test is skipped if no display adapter (GPU) is available.</item>
/// <item>Ensure that the MonoGame 'UI thread' is set to the current thread before running the test.</item>
/// <item>Run the test inside a <see cref="TestGame"/> instance, during the LoadContent method.</item>
/// </list>
/// Note: it is not safe to run asynchronous code inside the test method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GameTest : Attribute, IDataSourceAttribute, ITestExecutor, IParallelConstraint, ITestRegisteredEventReceiver, ITestDiscoveryEventReceiver, IScopedAttribute<GameTest>
{
    private const string PARALLEL_CONSTRAINT_KEY = "MONOGAME_GAME_TEST";
    
    public int Order { get; init; } = int.MaxValue / 2;
    
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // This is data row generator is necessary so that the test framework doesn't complain about missing parameters.
        // In practice the parameter is assigned a value in ExecuteTest.
        
        var args = dataGeneratorMetadata.TestInformation?.Parameters;
        if (args is not { Length: 1 } || !typeof(Game).IsAssignableFrom(args[0].Type))
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
        
        // Set text executor to this. Note that I'm creating a new instance every time here, 
        // which I'm not sure is necessary or not but that is the way that the standard TestExecutor<T> does it.
        context.SetTestExecutor(new GameTest());
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        // Set parallel constraint to not run in parallel with other GameTests.
        // It is not safe to run multiple game instances at once, they assume they are the only one running and will access
        // static data in a way that will conflict.
        context.SetParallelConstraint(new NotInParallelConstraint([PARALLEL_CONSTRAINT_KEY])
        {
            Order = Order
        });
        
        // Format the argument display: the first and only argument, which should be of type TestGame,
        // will be null by default since it is created at test execution time.
        // So instead just display the type name.
        // This is completely an aesthetic change that makes the test discovery screen look better and more intuitive rather
        // than just displaying "null" for the game parameter.
        context.AddArgumentDisplayFormatter(new ArgDisplayFormatter());
        
        return default;
    }

    private sealed class ArgDisplayFormatter : ArgumentDisplayFormatter
    {
        // No point in checking type, the object will always be null here.
        // It is assumed that the object is a null TestGame instance.
        public override bool CanHandle(object? value) => true; 

        public override string FormatValue(object? value) => "TestGame";
    }
}