using System;
using Microsoft.CodeAnalysis;

namespace Define.SourceGen;

internal static class Diagnostics
{
    public static readonly string Version = typeof(Diagnostics).Assembly.GetName().Version.ToString();

    private const string CATEGORY = "Define";

    // public static readonly DiagnosticDescriptor ParsersNeedNoArgConstructor = new DiagnosticDescriptor(
    //     id: "MGS0001",
    //     title: "XML parsers must have a public parameterless constructor",
    //     messageFormat: "XML parser '{0}' must have a public parameterless constructor",
    //     category: CATEGORY,
    //     DiagnosticSeverity.Error,
    //     isEnabledByDefault: true
    // );
}