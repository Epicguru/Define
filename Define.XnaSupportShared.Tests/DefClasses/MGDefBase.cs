namespace Define.Monogame.Tests.DefClasses;

public abstract class MGDefBase : IDef
{
    public string ID { get; set; } = null!;

    public abstract void EnsureExpected();
}