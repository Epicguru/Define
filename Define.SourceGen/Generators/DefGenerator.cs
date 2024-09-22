using Microsoft.CodeAnalysis;

namespace Define.SourceGen.Generators;

[Generator]
public sealed class DefGenerator : SourceGeneratorBase<DefGeneratorCollector>
{
    public override void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("Example", "// This is an example file.\n" +
                                     "class ExampleClass {}");
    }
}