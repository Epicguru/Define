using Microsoft.Xna.Framework;

namespace Define.Monogame.Tests.DefClasses;

public class RectangleDef : MGDefBase
{
    public Rectangle Rectangle;
    public Rectangle? RectangleNullable;
    
    public override void EnsureExpected()
    {
        Rectangle.Should().Be(new Rectangle(10, 12, -15, 20));
        RectangleNullable.Should().Be(new Rectangle(11, 12, 15, 21));
    }
}