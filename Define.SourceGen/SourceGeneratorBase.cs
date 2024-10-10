using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Define.SourceGen;

public abstract class SourceGeneratorBase<T> : ISourceGenerator where T : ISyntaxContextReceiver, new()
{
    protected T SyntaxReceiver { get; private set; } = default!;

    protected static string MakeBindingFlags(ISymbol symbol)
    {
        string[] flags =
        [
            symbol.DeclaredAccessibility.HasFlag(Accessibility.Public) ? nameof(BindingFlags.Public) : nameof(BindingFlags.NonPublic),
            symbol.IsStatic? nameof(BindingFlags.Static) : nameof(BindingFlags.Instance)
        ];

        return string.Join(" | ", flags.Select(f => $"BindingFlags.{f}"));
    }

    public virtual void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => SyntaxReceiver = new T());
    }

    public abstract void Execute(GeneratorExecutionContext context);
}