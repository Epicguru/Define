namespace Define;

/// <summary>
/// An interface that, when placed on a type that is parsed for a <see cref="IDef"/>,
/// will have its <see cref="PostLoad"/> and <see cref="LatePostLoad"/> methods called
/// after all defs are done loading.
/// </summary>
public interface IPostLoad
{
    /// <summary>
    /// Called once after all defs have been loaded.
    /// Called <b>before </b> <see cref="LatePostLoad"/> and <see cref="IConfigErrors.ConfigErrors"/>.
    /// </summary>
    void PostLoad();

    /// <summary>
    /// Called once after all defs have been loaded and had <see cref="PostLoad"/> called on them.
    /// The default interface implementation does nothing.
    /// </summary>
    void LatePostLoad() { }
}