using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Define;

/// <summary>
/// An object used to report errors after the XML load process.
/// See <see cref="IConfigErrors"/>.
/// </summary>
[PublicAPI]
public class ConfigErrorReporter
{
    /// <summary>
    /// The current <see cref="IDef"/> that is reporting errors.
    /// </summary>
    public IDef? CurrentDef { get; set; }

    /// <summary>
    /// Logs a config error for this def.
    /// It is logged to the <see cref="DefDebugger"/> as an error.
    /// </summary>
    /// <param name="message">The message to print. Should not be null.</param>
    /// <param name="e">The optional exception if one occurred. May be null.</param>
    public void Error(string message, Exception? e = null)
    {
        DefDebugger.Error($"[{CurrentDef?.ID ?? "?"}] {message}", e);
    }

    /// <summary>
    /// Logs a config warning for this def.
    /// It is logged to the <see cref="DefDebugger"/> as a warning.
    /// </summary>
    /// <param name="message">The message to print. Should not be null.</param>
    public void Warn(string message)
    {
        DefDebugger.Warn($"[{CurrentDef?.ID ?? "?"}] {message}");
    }

    /// <summary>
    /// Checks that an expression is true. If the expression is not true, an error is logged using <see cref="Error(string, Exception)"/>.
    /// Unlike <see cref="System.Diagnostics.Debug.Assert(bool)"/>, this method is not conditionally compiled, so it will still run in release mode.
    /// The return value is simply <paramref name="condition"/>, allowing this to be placed inline in a <c>if</c> statement.
    /// </summary>
    /// <param name="condition">The condition to evaluate. It is expected to be true.</param>
    /// <param name="message">The message to print. If you do not specify this argument, it will be automatically populated with the expression of <paramref name="condition"/>.</param>
    /// <returns>The value of <paramref name="condition"/>.</returns>
    public bool Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
    {
        if (!condition)
            Error($"Assert failed: {message}");

        return condition;
    }
}