using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Define.Monogame.Tests;

public sealed class RequiresGpuAttribute() : SkipAttribute("This test requires a GPU (graphics adapter) to run.")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        try
        {
            var adapter = GraphicsAdapter.DefaultAdapter;
            adapter.Should().NotBeNull();
            return Task.FromResult(false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create GraphicsAdapter.DefaultAdapter, skipping test ({context.TestName}). Exception:");
            Console.WriteLine(e);
            return Task.FromResult(true);
        }
    }
}