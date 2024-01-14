using Microsoft.Xna.Framework;

namespace Define.Monogame.Tests.DefClasses;

public class VectorDef : MGDefBase
{
    public Vector2 Vector2;
    public Vector3 Vector3;
    public Vector4 Vector4;

    public override void EnsureExpected()
    {
        Vector2.Should().Be(new Vector2(0, 1));
        Vector3.Should().Be(new Vector3(2, 3, 4));
        Vector4.Should().Be(new Vector4(5.5f, 6.7f, 7.8f, -123.23f));
    }
}