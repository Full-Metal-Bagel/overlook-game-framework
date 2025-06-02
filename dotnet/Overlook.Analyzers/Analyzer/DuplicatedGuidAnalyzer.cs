using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Overlook.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicatedGuidAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DuplicateTypeGuid, DiagnosticDescriptors.DuplicateMethodGuid);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        {
            var map = new ConcurrentDictionary<Guid, INamedTypeSymbol>();
            context.RegisterSymbolAction(ctx => AnalyzeGuidAttribute(ctx, map, (INamedTypeSymbol)ctx.Symbol, "Overlook.TypeGuidAttribute", DiagnosticDescriptors.DuplicateTypeGuid), SymbolKind.NamedType);
        }
        {
            var map = new ConcurrentDictionary<Guid, INamedTypeSymbol>();
            context.RegisterSymbolAction(ctx => AnalyzeGuidAttribute(ctx, map, (INamedTypeSymbol)ctx.Symbol, "System.GuidAttribute", DiagnosticDescriptors.DuplicateTypeGuid), SymbolKind.NamedType);
        }
        {
            var map = new ConcurrentDictionary<Guid, IMethodSymbol>();
            context.RegisterSymbolAction(ctx => AnalyzeGuidAttribute(ctx, map, (IMethodSymbol)ctx.Symbol, "Overlook.MethodGuidAttribute", DiagnosticDescriptors.DuplicateMethodGuid), SymbolKind.Method);
        }
    }

    private static void AnalyzeGuidAttribute<T>(
        SymbolAnalysisContext context,
        ConcurrentDictionary<Guid, T> guidMap,
        T symbol,
        string attributeName,
        DiagnosticDescriptor descriptor) where T : ISymbol
    {
        var guidAttribute = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == attributeName);

        if (guidAttribute == null)
            return;

        if (guidAttribute.ConstructorArguments.Length == 0 ||
            guidAttribute.ConstructorArguments[0].Value is not string guidString)
            return;

        if (!Guid.TryParse(guidString, out var guid))
            return;

        if (!guidMap.TryAdd(guid, symbol))
        {
            var diagnostic = Diagnostic.Create(
                descriptor,
                symbol.Locations[0],
                symbol.Name,
                guidMap[guid].Name,
                guid);

            context.ReportDiagnostic(diagnostic);
        }
    }
}

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor DuplicateTypeGuid = new(
        id: "GUID001",
        title: "Duplicate TypeGuid detected",
        messageFormat: "Type '{0}' has the same TypeGuid as '{1}': {2}",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each type with a TypeGuidAttribute should have a unique GUID.");

    public static readonly DiagnosticDescriptor DuplicateMethodGuid = new(
        id: "GUID002",
        title: "Duplicate MethodGuid detected",
        messageFormat: "Method '{0}' has the same MethodGuid as '{1}': {2}",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each method with a MethodGuidAttribute should have a unique GUID.");
}
