using System;
using Microsoft.CodeAnalysis;

namespace Define.SourceGen;

internal static class Diagnostics
{
    public static readonly string Version = typeof(Diagnostics).Assembly.GetName().Version.ToString();

    private const string CATEGORY = "Define";

    public static readonly DiagnosticDescriptor AssertionExpressionNull = new DiagnosticDescriptor(
        id: "DEFS0001",
        title: "Assertion expression must not be null or blank",
        messageFormat: "Assertion expression must not be a null or blank string. The assertion will be ignored.",
        category: CATEGORY,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}