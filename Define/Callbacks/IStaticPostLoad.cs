namespace Define.Callbacks;

/// <summary>
/// Classes meet the following requirements will have the <see cref="StaticPostLoad"/> static method called
/// after all defs are loaded and after the instance <see cref="IPostLoad.PostLoad"/> methods are called.
/// </summary>
public interface IStaticPostLoad
{
    /// <summary>
    /// Called once after all defs have been loaded and after the instance <see cref="IPostLoad.PostLoad"/> methods are called.
    /// This is only called if at least one instance of this type has been loaded from XML.
    /// </summary>
    /// <param name="database">The def database that is performing this callback.</param>
    static abstract void StaticPostLoad(DefDatabase database);
}