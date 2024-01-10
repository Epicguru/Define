using Define.Xml;

namespace Define;

/// <summary>
/// A class that is used to emit warning and error messages
/// from the def loading process.
/// Since this library is intended to be used in game, with a focus on modding in mind,
/// it is often desirable to attempt to continue with the loading process
/// when an error is encountered rather than throwing an exception.
/// However, you may subscribe to the <see cref="OnWarning"/> and <see cref="OnError"/> events and handle them as you see fit,
/// including throwing exceptions if you would prefer that behaviour.
/// </summary>
public static class DefDebugger
{
    /// <summary>
    /// An event raised whenever there is a warning in the parsing process.
    /// Warnings typically indicate a mis-configuration but will not necessarily mean
    /// that loading has failed in any way.
    /// </summary>
    public static event Action<string>? OnWarning;
    /// <summary>
    /// An event raised whenever there is an error in the parsing process.
    /// Errors may or may not be recoverable, as they can indicate anything from a incorrectly formatted float value,
    /// to a missing def in a def inheritance chain.
    /// </summary>
    public static event ParseErrorDelegate? OnError;

    /// <summary>
    /// A delegate for error callbacks, see <see cref="DefDebugger.OnError"/>.
    /// </summary>
    public delegate void ParseErrorDelegate(string message, Exception? e, in XmlParseContext? ctx);

    /// <summary>
    /// Raises the <see cref="OnWarning"/> event with the provided message.
    /// </summary>
    public static void Warn(string message)
    {
        OnWarning?.Invoke(message);
    }

    /// <summary>
    /// Raises the <see cref="OnError"/> event with the provided message and optional exception and parse context.
    /// </summary>
    public static void Error(string message, Exception? e = null, in XmlParseContext? ctx = null)
    {
        OnError?.Invoke(message, e, ctx);
    }
}