using Microsoft.Xna.Framework.Graphics;
using TestSharedLib;
using Xunit.Abstractions;

namespace Define.Monogame.Tests;

public abstract class MonogameDefTestBase(ITestOutputHelper output) : DefTestBase(output)
{
    protected static void CheckAdapterCreation()
    {
        try
        {
            var adapter = GraphicsAdapter.DefaultAdapter;
            adapter.Should().NotBeNull();
        }
        catch (Exception e)
        {
            throw new NoSuitableGraphicsDeviceException($"Default graphics adapter threw an exception when being created, assuming bad graphics device. See inner exception: {e}", e);
        }
    }

    protected override void PreLoad(DefDatabase db)
    {
        base.PreLoad(db);
        
        db.Loader.AddMonogameDataParsers();
    }
}
