using Microsoft.Xna.Framework.Graphics;

namespace Define.Monogame.Tests.DefClasses;

public class ContentDef : IDef
{
    public string ID { get; set; } = null!;

    public Texture2D? Texture;
    
    public void OnRegister(DefDatabase database) { }
    public void OnUnRegister(DefDatabase database) { }
}