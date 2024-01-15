using TestSharedLib;
using Xunit.Abstractions;

namespace Define.Monogame.Tests;

public abstract class MonogameDefTestBase(ITestOutputHelper output) : DefTestBase(output)
{
    protected override void PreLoad(DefDatabase db)
    {
        base.PreLoad(db);
        
        db.Loader!.AddMonogameDataParsers();
    }
}
