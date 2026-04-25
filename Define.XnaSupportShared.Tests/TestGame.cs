using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Define.Monogame.Tests;

public sealed class TestGame : Game
{
    public ContentManager ContentManager { get; private set; } = null!;

    public Action<TestGame>? ToExecute { get; set; }
    
    public TestGame()
    {
        _ = new GraphicsDeviceManager(this);
    }

    public TestGame(Action<TestGame>? toExecute) : this()
    {
        ToExecute = toExecute;
    }
    
    protected override void LoadContent()
    {
        base.LoadContent();
        ContentManager = Content;

        if (ToExecute is null)
            throw new InvalidOperationException("Missing run action");
        
        ToExecute?.Invoke(this);
        
        Exit();
    }
}
