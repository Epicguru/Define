namespace Define.Callbacks;

/// <summary>
/// When a class that implements <see cref="IDef"/> also implements this interface,
/// it will have its <see cref="OnRegister(DefDatabase)"/> and <see cref="OnUnRegister(DefDatabase)"/>
/// methods called when it is registered or unregistered from a <see cref="DefDatabase"/>.
/// Unlike interfaces such as <see cref="IPostLoad"/>, this interface is only valid on the root <see cref="IDef"/> class, it does nothing on inner data.
/// </summary>
public interface IOnDatabaseRegister
{
    /// <summary>
    /// This is called immediately after this def is registered to a database.
    /// </summary>
    void OnRegister(DefDatabase database);

    /// <summary>
    /// This is called immediately after this def is unregistered from a database.
    /// </summary>
    void OnUnRegister(DefDatabase database);
}