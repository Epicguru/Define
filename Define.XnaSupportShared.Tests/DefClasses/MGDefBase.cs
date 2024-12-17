namespace Define.Monogame.Tests.DefClasses;

public abstract class MGDefBase : IDef
{
    public string ID { get; set; } = null!;
    
    public void OnRegister(DefDatabase database) { }

    public void OnUnRegister(DefDatabase database) { }

    public abstract void EnsureExpected();
}