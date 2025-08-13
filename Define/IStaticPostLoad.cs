namespace Define;

/// <summary>
/// Classes meet the following requirements will have the <see cref="StaticPostLoad"/> static method called
/// after all defs are loaded and after the instance <see cref="IPostLoad.PostLoad"/> methods are called.
/// </summary>
public interface IStaticPostLoad
{
    static abstract void StaticPostLoad();
}