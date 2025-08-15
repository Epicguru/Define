using System.Linq;
using Microsoft.CodeAnalysis;
using Scriban;
using SourceGenerator.Helper.CopyCode;

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
        {{~ for mem in members ~}}
        
        // {{ mem.name }}
        {{ mem.config_method_contents }}
        {{~ end ~}}
    }
} 
""";

    public override void Initialize(GeneratorInitializationContext context)
    {
        base.Initialize(context);
        context.RegisterForPostInitialization(ctx =>
        {
            ctx.AddSource("AssertAttribute.g.cs", Copy.DefineSourceGenAttributesAssertAttribute);
            ctx.AddSource("RequiredAttribute.g.cs", Copy.DefineSourceGenAttributesRequiredAttribute);
            ctx.AddSource("MinAttribute.g.cs", Copy.DefineSourceGenAttributesMinAttribute);
            ctx.AddSource("MaxAttribute.g.cs", Copy.DefineSourceGenAttributesMaxAttribute);
        });
    }

    public override void Execute(GeneratorExecutionContext context)
    {
        foreach (var diag in SyntaxReceiver.DiagnosticsList)
        {
            context.ReportDiagnostic(diag);
        }
        
        var template = Template.Parse(TEMPLATE);
        
        foreach (var def in SyntaxReceiver.DefData)
        {
            string className = def.ClassSymbol.Name;
            string classNamespace = def.ClassSymbol.ContainingNamespace.ToDisplayString();

            foreach (var member in def.MemberGenData)
            {
                member.ConfigMethodContents = member.GetConfigMethodContents(def);
            }
            
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
                },
                Members = def.MemberGenData.ToList()
            });
            
            context.AddSource($"{className}.g.cs", source);
        }
    }
}