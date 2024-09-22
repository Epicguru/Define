using System.ComponentModel;

namespace Define;

/// <summary>
/// Classes or structs that implement this interface will have <see cref="ConfigErrors"/> called on them after <see cref="IPostLoad.PostLoad"/> and <see cref="IPostLoad.LatePostLoad"/>.
/// This method should be used to emit error messages when values are misconfigured in the def.
/// </summary>
public interface IConfigErrors
{
    /// <summary>
    /// Called once after <see cref="IPostLoad.PostLoad"/> and <see cref="IPostLoad.LatePostLoad"/> have been called.
    /// Should be used to check for errors in the def.
    /// </summary>
    void ConfigErrors(ConfigErrorReporter config);
    
    /// <summary>
    /// Should be source-generated.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void ConfigErrorsGenerated(ConfigErrorReporter config) { }
}