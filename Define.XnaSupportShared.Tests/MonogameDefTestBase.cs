using TestSharedLib;

namespace Define.Monogame.Tests;

public abstract class MonogameDefTestBase : DefTestBase
{
    protected override void PreLoad(DefDatabase db)
    {
        base.PreLoad(db);
        
        db.Loader.AddMonogameDataParsers();
    }
}
