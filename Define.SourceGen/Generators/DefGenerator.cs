using Microsoft.CodeAnalysis;
using Scriban;

namespace Define.SourceGen.Generators;

[Generator]
public class DefGenerator : SourceGeneratorBase<DefGeneratorCollector>
{
    public const string DEF_INTERFACE_NAME = "Define.IDef";

    private const string TEMPLATE =
"""
using Define;

namespace {{ namespace }};
  
public partial class {{ class.name }}
{
    public {{ method.modifier }} void ConfigErrorsGenerated(ConfigErrorReporter config)
    {
        
    }
}
""";
    
    public override void Execute(GeneratorExecutionContext context)
    {
        var template = Template.Parse(TEMPLATE);
        
        foreach (var pair in SyntaxReceiver.ToGenerate)
        {
            var (syntax, symbol) = pair;
            string className = symbol.ContainingType.Name;
            string classNamespace = symbol.ContainingNamespace.ToDisplayString();

            var source = template.Render(new
            {
                Namespace = classNamespace,
                Class = new
                {
                    Name = className
                },
                Method = new
                {
                    Modifier = "virtual"
                }
            });
            
            context.AddSource($"{className}.g.cs", source);
        }
        
        //context.AddSource("Debug.g.cs", $"// There are {SyntaxReceiver.ToGenerate.Count} fields to generate.");
    }
}