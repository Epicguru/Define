using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Define.Monogame.Tests;

public sealed class TestGame : Game
{
    public ContentManager ContentManager { get; private set; } = null!;

    private readonly Action<TestGame> toExecute;
    
    public TestGame(Action<TestGame> toExecute)
    {
        _ = new GraphicsDeviceManager(this);
        this.toExecute = toExecute;
    }
    
    protected override void LoadContent()
    {
        base.LoadContent();
        ContentManager = Content;

        toExecute(this);
        
        Exit();
    }
}
