using Microsoft.Xna.Framework;

namespace Define.Monogame.Tests.DefClasses;

public class ColorDef : MGDefBase
{
    public List<Color> Colors = [];
    
    public override void EnsureExpected()
    {
        List<Color> expected =
        [
            new Color(0.1f, 0.2f, 0.3f),
            new Color(0.1f, 0.2f, 0.3f, 0.45f),
            new Color(79, 219, 116),
            new Color(79, 219, 116, 250),
            Color.Green,
            Color.Aquamarine,
            Color.Beige
        ];

        Colors.Should().BeEquivalentTo(expected);
    }
}