using Microsoft.Xna.Framework.Graphics;

namespace Define.Monogame.Tests;

public sealed class RequiresGpuAttribute() : SkipAttribute("This test requires a GPU (graphics adapter) to run.")
{
    private static bool? isGpuAvailable;
    private static Exception? gpuCheckException;
    private static readonly object gpuCheckLock = new object();
    
    public static bool ShouldSkip() => ShouldSkip(out _);
    
    public static bool ShouldSkip(out Exception? gpuException)
    {
        lock (gpuCheckLock)
        {
            // Return cached result if available.
            if (isGpuAvailable.HasValue)
            {
                gpuException = gpuCheckException;
                return !isGpuAvailable.Value;
            }
            
            // Check and cache result.
            try
            {
                var adapter = GraphicsAdapter.DefaultAdapter;
                adapter.Should().NotBeNull();
                isGpuAvailable = true;
                gpuException = gpuCheckException = null;
                return false;
            }
            catch (Exception e)
            {
                isGpuAvailable = false;
                gpuCheckException = e;
                gpuCheckException = gpuException = e;
                return true;
            }
        }
    }
    
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        if (!ShouldSkip(out var ex))
            return Task.FromResult(false);
        
        Console.WriteLine($"Skipping test '{context.TestName}' because no GPU is available.\nException:\n{ex}");
        return Task.FromResult(true);
    }
}